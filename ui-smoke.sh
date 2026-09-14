#!/usr/bin/env bash
# ui-smoke.sh
#
# Startnachweis der Oberflaechen fuer die zwoelf Messlaeufe.
# Startet je Lauf das Backend gegen die vorhandene Datenbank, startet das
# Frontend und laesst ui-smoke.mjs die Startseite pruefen.
#
# Es wird NICHTS an den Laufverzeichnissen veraendert. Die Datenbanken werden
# nur gelesen, nicht zurueckgesetzt, weil die Pruefung die Startseite laedt
# und keinen Zustand schreibt.
#
# Voraussetzungen:
#   - Container "sqlserver-bachelor" laeuft
#   - MSSQL_SA_PASSWORD ist gesetzt
#   - node, npm und dotnet sind verfuegbar
#   - playwright ist installiert (siehe --setup)
#
# Aufruf:
#   ./ui-smoke.sh --setup            einmalig, installiert Playwright lokal
#   ./ui-smoke.sh                    alle zwoelf Laeufe
#   ./ui-smoke.sh A bmad 1           ein einzelner Lauf
#
# Ergebnisse:  smoke-ergebnisse/<lauf>.json, <lauf>.png, uebersicht.csv

set -uo pipefail

SQLSERVER_CONTAINER="sqlserver-bachelor"
DB_PORT=1433
FRONTEND_PORT=4200
ERGEBNIS_DIR="smoke-ergebnisse"
WARTEZEIT_BACKEND=90     # Sekunden
WARTEZEIT_FRONTEND=180   # Sekunden, erster ng-serve-Build dauert lange

SKRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
cd "$SKRIPT_DIR"

# ---------------------------------------------------------------- Setup ----
if [ "${1:-}" = "--setup" ]; then
  echo "== Playwright installieren =="
  npm init -y >/dev/null 2>&1 || true
  npm install --no-save playwright@latest || exit 1
  npx playwright install chromium || exit 1
  npx playwright install-deps chromium || echo "Hinweis: install-deps benoetigt ggf. sudo, bei Fehlern manuell nachziehen."
  echo "Fertig."
  exit 0
fi

# --------------------------------------------------- Vorbedingungen ----
fehler=0
for werkzeug in docker node npm dotnet npx; do
  if ! command -v "$werkzeug" >/dev/null 2>&1; then
    echo "FEHLT: $werkzeug ist nicht im PATH." >&2; fehler=1
  fi
done

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "FEHLT: MSSQL_SA_PASSWORD ist nicht gesetzt. Vorher: export MSSQL_SA_PASSWORD='<passwort>'" >&2
  fehler=1
fi

if command -v docker >/dev/null 2>&1; then
  if docker ps --format '{{.Names}}' | grep -qx "$SQLSERVER_CONTAINER"; then
    echo "OK: Container '$SQLSERVER_CONTAINER' laeuft."
  else
    if docker ps -a --format '{{.Names}}' | grep -qx "$SQLSERVER_CONTAINER"; then
      echo "Container '$SQLSERVER_CONTAINER' ist gestoppt, starte ihn."
      docker start "$SQLSERVER_CONTAINER" >/dev/null || { echo "Start fehlgeschlagen." >&2; fehler=1; }
      sleep 15
    else
      echo "FEHLT: Container '$SQLSERVER_CONTAINER' existiert nicht." >&2
      fehler=1
    fi
  fi
fi

if [ ! -f "node_modules/playwright/package.json" ] && [ ! -d "node_modules/playwright" ]; then
  echo "FEHLT: playwright ist nicht installiert. Einmalig './ui-smoke.sh --setup' ausfuehren." >&2
  fehler=1
fi

if [ ! -d "runs" ]; then
  echo "FEHLT: Verzeichnis 'runs' nicht gefunden. Skript im Repo-Wurzelverzeichnis ablegen." >&2
  fehler=1
fi

[ "$fehler" -ne 0 ] && { echo "Abbruch wegen fehlender Voraussetzungen." >&2; exit 1; }

sqlcmd() {
  docker exec "$SQLSERVER_CONTAINER" /opt/mssql-tools18/bin/sqlcmd \
    -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b "$@"
}

mkdir -p "$ERGEBNIS_DIR"

# ------------------------------------------------------------- Helfer ----
BACKEND_PID=""
FRONTEND_PID=""

aufraeumen() {
  [ -n "$FRONTEND_PID" ] && kill "$FRONTEND_PID" 2>/dev/null
  [ -n "$BACKEND_PID" ] && kill "$BACKEND_PID" 2>/dev/null
  sleep 2
  [ -n "$FRONTEND_PID" ] && kill -9 "$FRONTEND_PID" 2>/dev/null
  [ -n "$BACKEND_PID" ] && kill -9 "$BACKEND_PID" 2>/dev/null
  # ng serve startet Kindprozesse, die den Port halten koennen
  pkill -f "ng serve --port ${FRONTEND_PORT}" 2>/dev/null
  FRONTEND_PID=""; BACKEND_PID=""
  sleep 2
}
trap 'aufraeumen; exit 130' INT TERM

# Sucht den Port, den das Frontend fuer das Backend erwartet.
# Reihenfolge: proxy.conf*.json, dann environment*.ts, sonst Standardwert.
backend_port_ermitteln() {
  local fe="$1" port=""
  port=$(grep -rhoE '"target"[^"]*"https?://[^"]*:([0-9]{4,5})' "$fe" --include='proxy.conf*.json' 2>/dev/null \
         | grep -oE ':[0-9]{4,5}' | tr -d ':' | head -1)
  if [ -z "$port" ]; then
    port=$(grep -rhoE 'https?://localhost:[0-9]{4,5}' "$fe/src" 2>/dev/null \
           | grep -oE ':[0-9]{4,5}' | tr -d ':' | grep -v "^${FRONTEND_PORT}$" | head -1)
  fi
  [ -z "$port" ] && port=5001
  echo "$port"
}

warte_auf_port() {
  local url="$1" grenze="$2" pid="$3" i=0
  while [ "$i" -lt "$grenze" ]; do
    if curl -s -o /dev/null -m 3 "$url" 2>/dev/null; then return 0; fi
    if [ -n "$pid" ] && ! kill -0 "$pid" 2>/dev/null; then return 2; fi
    sleep 1; i=$((i+1))
  done
  return 1
}

# ----------------------------------------------------------- ein Lauf ----
pruefe_lauf() {
  local P="$1" ANSATZ="$2" WDH="$3"
  local p_lower db lauf run_dir csproj fe be_port
  p_lower=$(echo "$P" | tr '[:upper:]' '[:lower:]')
  db="${p_lower}_${ANSATZ}_${WDH}"
  lauf="$db"

  run_dir="runs/${P}/${ANSATZ}/$(printf '%02d' "$WDH")"
  [ -d "$run_dir" ] || run_dir="runs/${P}/${ANSATZ}/${WDH}"
  if [ ! -d "$run_dir" ]; then
    echo "$lauf: Laufverzeichnis nicht gefunden, uebersprungen." >&2; return 1
  fi

  echo ""
  echo "===================== $lauf ====================="

  if ! sqlcmd -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$db') IS NULL THEN 0 ELSE 1 END;" \
       | tr -d '[:space:]' | grep -q '^1$'; then
    echo "$lauf: Datenbank '$db' fehlt, uebersprungen." >&2; return 1
  fi

  csproj=$(find "$run_dir/code" -maxdepth 4 -iname "*.csproj" 2>/dev/null | grep -vi test | head -n1)
  if [ -z "$csproj" ]; then echo "$lauf: kein Backend-Projekt gefunden." >&2; return 1; fi

  fe=$(find "$run_dir/code" -maxdepth 3 -name "angular.json" 2>/dev/null | head -n1)
  if [ -z "$fe" ]; then echo "$lauf: kein Frontend-Projekt gefunden." >&2; return 1; fi
  fe=$(dirname "$fe")

  be_port=$(backend_port_ermitteln "$fe")
  echo "Backend: $csproj (Port $be_port)"
  echo "Frontend: $fe"

  echo "-- Backend starten --"
  ConnectionStrings__Default="Server=localhost,${DB_PORT};Database=${db};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True" \
  ASPNETCORE_URLS="http://localhost:${be_port}" \
  ASPNETCORE_ENVIRONMENT="Development" \
  dotnet run --project "$csproj" --no-launch-profile > "${ERGEBNIS_DIR}/${lauf}.backend.log" 2>&1 &
  BACKEND_PID=$!

  warte_auf_port "http://localhost:${be_port}/" "$WARTEZEIT_BACKEND" "$BACKEND_PID"
  case $? in
    2) echo "$lauf: Backend-Prozess vorzeitig beendet, siehe ${ERGEBNIS_DIR}/${lauf}.backend.log" >&2
       echo "{\"lauf\":\"$lauf\",\"urteil\":\"Backend startet nicht\"}" > "${ERGEBNIS_DIR}/${lauf}.json"
       aufraeumen; return 1 ;;
    1) echo "$lauf: Backend antwortet nicht innerhalb ${WARTEZEIT_BACKEND}s." >&2
       echo "{\"lauf\":\"$lauf\",\"urteil\":\"Backend antwortet nicht\"}" > "${ERGEBNIS_DIR}/${lauf}.json"
       aufraeumen; return 1 ;;
  esac
  echo "Backend antwortet."

  if [ ! -d "$fe/node_modules" ]; then
    echo "-- npm install (node_modules fehlt) --"
    (cd "$fe" && npm install > "${SKRIPT_DIR}/${ERGEBNIS_DIR}/${lauf}.npm.log" 2>&1) \
      || { echo "$lauf: npm install fehlgeschlagen." >&2; aufraeumen; return 1; }
  fi

  echo "-- Frontend starten --"
  (cd "$fe" && npx ng serve --port "$FRONTEND_PORT" > "${SKRIPT_DIR}/${ERGEBNIS_DIR}/${lauf}.frontend.log" 2>&1) &
  FRONTEND_PID=$!

  warte_auf_port "http://localhost:${FRONTEND_PORT}/" "$WARTEZEIT_FRONTEND" ""
  if [ $? -ne 0 ]; then
    echo "$lauf: Frontend antwortet nicht innerhalb ${WARTEZEIT_FRONTEND}s, siehe ${ERGEBNIS_DIR}/${lauf}.frontend.log" >&2
    echo "{\"lauf\":\"$lauf\",\"urteil\":\"Frontend startet nicht\"}" > "${ERGEBNIS_DIR}/${lauf}.json"
    aufraeumen; return 1
  fi
  echo "Frontend antwortet."

  echo "-- Startnachweis --"
  node ui-smoke.mjs --lauf "$lauf" --url "http://localhost:${FRONTEND_PORT}/" --out "$ERGEBNIS_DIR"

  aufraeumen
  return 0
}

# ------------------------------------------------------------- Ablauf ----
if [ $# -eq 3 ]; then
  pruefe_lauf "$1" "$2" "$3"
else
  for P in A B; do
    for ANSATZ in bmad solo; do
      for WDH in 1 2 3; do
        pruefe_lauf "$P" "$ANSATZ" "$WDH"
      done
    done
  done
fi

echo ""
echo "== Uebersicht =="
{
  echo "Lauf;Urteil;Datenabrufe;fehlerhafte Datenabrufe;Konsolenfehler"
  for f in "${ERGEBNIS_DIR}"/*.json; do
    [ -e "$f" ] || continue
    node -e '
      const d = require("fs").readFileSync(process.argv[1], "utf-8");
      const o = JSON.parse(d);
      console.log([o.lauf, o.urteil, o.datenAufrufe ?? o.apiAufrufe ?? "", (o.fehlerhafteAufrufe||o.apiFehlerhaft||[]).length, o.anzahlKonsolenfehler ?? ""].join(";"));
    ' "$f"
  done
} | tee "${ERGEBNIS_DIR}/uebersicht.csv"

echo ""
echo "Ergebnisse in ${ERGEBNIS_DIR}/ (JSON, PNG, uebersicht.csv)."
