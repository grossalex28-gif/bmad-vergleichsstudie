#!/usr/bin/env bash
# Startet nacheinander alle zwoelf Implementierungen kurz, nur um deren
# generierte OpenAPI-Beschreibung zu sichern -- Grundlage fuer den Bau der
# zwoelf Adapter (Abschnitt 6 der Akzeptanztest-Architektur: "Grundlage der
# Uebersetzung ist die von ASP.NET Core erzeugte OpenAPI-Beschreibung, nicht
# der Quellcode"). Legt KEINE Datenbank an, prueft keine Inhalte, sichert
# keine Baseline -- reines Hilfsskript fuer diesen einen Zweck.
#
# Fuer b_solo_1/b_solo_2 (siehe Uebergabe, Abschnitt 2a) muss die Datenbank
# vorher existieren, sonst schlaegt der Start ggf. fehl. Falls dieses Skript
# dafuer keine OpenAPI-Datei liefert: vorher einmal
#   ./run-a1.sh B solo 1
#   ./run-a1.sh B solo 2
# ausfuehren (legt die fehlende Datenbank an und laesst die Anwendung sich
# selbst seeden), danach dieses Skript erneut aufrufen -- bereits vorhandene
# Ergebnisse werden uebersprungen.
#
# Voraussetzung: MSSQL_SA_PASSWORD ist gesetzt.
# Aufruf: ./hole-openapi.sh
# Ergebnis: a1-testsuite/openapi/<db_name>.json je Implementierung

set -uo pipefail  # bewusst kein -e: ein Fehlschlag bei einer Implementierung
                  # soll die anderen elf nicht verhindern

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "MSSQL_SA_PASSWORD ist nicht gesetzt. Vorher: export MSSQL_SA_PASSWORD='<passwort>'" >&2
  exit 1
fi

BACKEND_PORT=5001
BASE_URL="http://localhost:${BACKEND_PORT}"
AUSGABE_ORDNER="a1-testsuite/openapi"
mkdir -p "$AUSGABE_ORDNER"

LAEUFE=(
  "A/bmad/01:a_bmad_1"
  "A/bmad/02:a_bmad_2"
  "A/bmad/03:a_bmad_3"
  "A/solo/01:a_solo_1"
  "A/solo/02:a_solo_2"
  "A/solo/03:a_solo_3"
  "B/bmad/01:b_bmad_1"
  "B/bmad/02:b_bmad_2"
  "B/bmad/03:b_bmad_3"
  "B/solo/01:b_solo_1"
  "B/solo/02:b_solo_2"
  "B/solo/03:b_solo_3"
)

hole_eine() {
  local run_subdir="$1" db_name="$2"
  local run_dir="runs/${run_subdir}"
  local ziel="${AUSGABE_ORDNER}/${db_name}.json"

  echo "== [$db_name] =="

  if [ -f "$ziel" ]; then
    echo "  bereits vorhanden, ueberspringe."
    return 0
  fi

  local backend_csproj
  backend_csproj=$(find "$run_dir/code" -maxdepth 4 -iname "*.csproj" 2>/dev/null | grep -vi test | head -n1 || true)
  if [ -z "$backend_csproj" ]; then
    echo "  Kein Backend-Projekt unter ${run_dir}/code gefunden, ueberspringe." >&2
    return 1
  fi

  echo "  Starte: $backend_csproj"
  ConnectionStrings__Default="Server=localhost,1433;Database=${db_name};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True" \
  ASPNETCORE_URLS="$BASE_URL" \
  ASPNETCORE_ENVIRONMENT="Development" \
  dotnet run --project "$backend_csproj" --no-launch-profile &
  local pid=$!

  local tries=0
  while ! curl -s -o /dev/null "${BASE_URL}/"; do
    if ! kill -0 "$pid" 2>/dev/null; then
      echo "  Backend-Prozess vorzeitig beendet (evtl. fehlende Datenbank, siehe Skriptkopf), ueberspringe." >&2
      return 1
    fi
    sleep 1
    tries=$((tries + 1))
    if [ "$tries" -gt 60 ]; then
      echo "  Backend antwortet nach 60s nicht, breche ab." >&2
      kill "$pid" 2>/dev/null || true
      wait "$pid" 2>/dev/null || true
      return 1
    fi
  done

  local gefunden=""
  for pfad in "openapi/v1.json" "swagger/v1/swagger.json"; do
    if curl -s -f -o "${ziel}.tmp" "${BASE_URL}/${pfad}"; then
      mv "${ziel}.tmp" "$ziel"
      gefunden="$pfad"
      break
    fi
  done

  kill "$pid" 2>/dev/null || true
  wait "$pid" 2>/dev/null || true

  if [ -n "$gefunden" ]; then
    echo "  OpenAPI gesichert: $ziel (ueber /$gefunden)"
    return 0
  else
    echo "  Keine OpenAPI-Beschreibung gefunden (weder openapi/v1.json noch swagger/v1/swagger.json)." >&2
    rm -f "${ziel}.tmp" 2>/dev/null || true
    return 1
  fi
}

FEHLGESCHLAGEN=()
for eintrag in "${LAEUFE[@]}"; do
  run_subdir="${eintrag%%:*}"
  db_name="${eintrag##*:}"
  if ! hole_eine "$run_subdir" "$db_name"; then
    FEHLGESCHLAGEN+=("$db_name")
  fi
done

echo ""
echo "Fertig. Ergebnisse in ${AUSGABE_ORDNER}/:"
ls -la "$AUSGABE_ORDNER" 2>/dev/null || true

if [ "${#FEHLGESCHLAGEN[@]}" -gt 0 ]; then
  echo ""
  echo "Ohne OpenAPI-Beschreibung geblieben: ${FEHLGESCHLAGEN[*]}"
  exit 1
fi
