#!/usr/bin/env bash
# Fuehrt die A1-Akzeptanztestsuite fuer genau eine Implementierung aus.
#
# Arbeitet gegen den bereits bestehenden, dauerhaft laufenden Container
# "sqlserver-bachelor" und die vom Harness (database.py) bereits angelegten
# Datenbanken (Namensschema <projekt>_<bedingung>_<wiederholung>,
# z. B. a_bmad_1). Legt NICHTS neu an, wenn die Datenbank schon existiert,
# und loescht nichts automatisch.
#
# Voraussetzungen:
#   - Container "sqlserver-bachelor" laeuft
#   - MSSQL_SA_PASSWORD ist gesetzt
#   - backup-rohzustand.sh wurde vorher einmal fuer alle zwoelf Datenbanken
#     ausgefuehrt (Sicherheitskopie des unveraenderten Ausgangszustands)
#
# Aufruf: ./run-a1.sh <Projekt: A|B> <Ansatz: bmad|solo> <Wiederholung: 1|2|3>

set -euo pipefail

if [ $# -ne 3 ]; then
  echo "Aufruf: $0 <Projekt: A|B> <Ansatz: bmad|solo> <Wiederholung: 1|2|3>" >&2
  exit 1
fi
if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "MSSQL_SA_PASSWORD ist nicht gesetzt. Vorher: export MSSQL_SA_PASSWORD='<passwort>'" >&2
  exit 1
fi

PROJEKT_ORDNER="$1"   # A oder B, fuer den Pfad runs/<Projekt>/...
ANSATZ="$2"           # bmad oder solo
WDH="$3"              # 1, 2 oder 3

PROJEKT_LOWER=$(echo "$PROJEKT_ORDNER" | tr '[:upper:]' '[:lower:]')
DB_NAME="${PROJEKT_LOWER}_${ANSATZ}_${WDH}"

SQLSERVER_CONTAINER="sqlserver-bachelor"
DB_PORT=1433
BACKEND_PORT=5001
BASE_URL="http://localhost:${BACKEND_PORT}"

# Laufordner: unterstuetzt sowohl "01" als auch "1" als Ordnernamen,
# da der DB-Name laut database.py keine fuehrende Null hat, der Ordner
# aber moeglicherweise schon.
RUN_DIR="runs/${PROJEKT_ORDNER}/${ANSATZ}/$(printf '%02d' "$WDH")"
[ -d "$RUN_DIR" ] || RUN_DIR="runs/${PROJEKT_ORDNER}/${ANSATZ}/${WDH}"
if [ ! -d "$RUN_DIR" ]; then
  echo "Laufverzeichnis nicht gefunden (weder .../${WDH} noch .../$(printf '%02d' "$WDH"))." >&2
  exit 1
fi

BACKUP_HOST_PATH="${RUN_DIR}/artifacts/a1-baseline.bak"
BACKUP_CONTAINER_PATH="/var/opt/mssql/backup/${DB_NAME}_a1baseline.bak"

sqlcmd() {
  # -b: bei einem SQL-Fehler nichtnull beenden, sonst gibt sqlcmd auch nach
  # einem fehlgeschlagenen Statement 0 zurueck und "set -e" greift nicht.
  docker exec "$SQLSERVER_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C -b "$@"
}

echo "== [$DB_NAME] Container pruefen =="
if ! docker ps --format '{{.Names}}' | grep -qx "$SQLSERVER_CONTAINER"; then
  echo "Container '$SQLSERVER_CONTAINER' laeuft nicht. docker start $SQLSERVER_CONTAINER zuerst ausfuehren." >&2
  exit 1
fi

echo "== [$DB_NAME] Vorhandensein der Datenbank pruefen =="
DB_EXISTS=$(sqlcmd -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$DB_NAME') IS NULL THEN 0 ELSE 1 END;" | tr -d '[:space:]')

if [ "$DB_EXISTS" = "1" ]; then
  echo "Datenbank '$DB_NAME' existiert bereits (Stand aus der urspruenglichen Erhebung). Bleibt unveraendert."
else
  echo "Datenbank '$DB_NAME' existiert nicht (ungewoehnlich, falls der Lauf regulaer terminiert ist). Lege eine leere Datenbank an."
  sqlcmd -Q "CREATE DATABASE [$DB_NAME];"
fi

echo "== [$DB_NAME] Backend-Projekt suchen =="
BACKEND_CSPROJ=$(find "$RUN_DIR/code" -maxdepth 4 -iname "*.csproj" 2>/dev/null | grep -vi test | head -n1 || true)
if [ -z "$BACKEND_CSPROJ" ]; then
  echo "Kein Backend-Projekt unter ${RUN_DIR}/code gefunden. Pfad pruefen." >&2
  exit 1
fi
echo "Gefunden: $BACKEND_CSPROJ"

# Sicherheitsnetz: wird die manuelle Bestaetigungs-Pause per Strg+C
# abgebrochen, ruft bash stop_backend() nicht automatisch auf (nur die in
# diesem Skript vorgesehenen Codepfade tun das) -- der im Hintergrund
# gestartete Backend-Prozess blieb dadurch als Waise aktiv und beantwortete
# spaeter Anfragen mit einer inzwischen ungueltigen Datenbankverbindung
# (Vorfall bei b_bmad_2: DROP DATABASE + Neustart liefen gegen den
# verwaisten alten Prozess statt den neuen, taeuschte eine leere Datenbank
# vor, obwohl die Daten korrekt vorhanden waren). Deshalb bei INT/TERM/EXIT
# aufraeumen, falls ein Backend noch laeuft.
cleanup_on_exit() {
  if [ -n "${BACKEND_PID:-}" ]; then
    stop_backend
  fi
}
trap cleanup_on_exit INT TERM EXIT

start_backend() {
  ConnectionStrings__Default="Server=localhost,${DB_PORT};Database=${DB_NAME};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True" \
  ASPNETCORE_URLS="$BASE_URL" \
  ASPNETCORE_ENVIRONMENT="Development" \
  dotnet run --project "$BACKEND_CSPROJ" --no-launch-profile &
  BACKEND_PID=$!
  echo "Warte auf Backend (PID $BACKEND_PID)..."
  local tries=0
  # Nur pruefen, ob der Server ueberhaupt antwortet, nicht auf welchem
  # Pfad eine OpenAPI-Beschreibung liegt: Neuere .NET-Versionen liefern sie
  # standardmaessig unter /openapi/v1.json, aeltere Swashbuckle-Projekte
  # unter /swagger/v1/swagger.json, manche Implementierungen gar nicht.
  # curl ohne -f: jeder HTTP-Statuscode zaehlt als "erreichbar".
  until curl -s -o /dev/null "${BASE_URL}/" 2>&1; do
    if ! kill -0 "$BACKEND_PID" 2>/dev/null; then
      echo "Backend-Prozess ist bereits beendet, bevor er antwortete. dotnet-Ausgabe oben pruefen." >&2
      exit 1
    fi
    sleep 1
    tries=$((tries+1))
    if [ "$tries" -gt 60 ]; then
      echo "Backend antwortet nach 60s nicht. Abbruch." >&2
      kill "$BACKEND_PID" 2>/dev/null || true
      exit 1
    fi
  done
  echo "Backend antwortet."
}

stop_backend() {
  # "dotnet run" kann bei manchen SDK-Versionen einen Kindprozess (das
  # eigentliche kompilierte Backend) abspalten, der beim Beenden des
  # Wrapper-Prozesses nicht automatisch mitstirbt und Port ${BACKEND_PORT}
  # weiter offen haelt -- Vorfall beim Uebergang a_solo_3 -> b_bmad_1:
  # naechster Lauf scheiterte mit "Address already in use". Deshalb
  # zusaetzlich alle Kindprozesse des Wrapper-PIDs beenden, bevor der
  # Wrapper selbst beendet wird.
  pkill -P "$BACKEND_PID" 2>/dev/null || true
  kill "$BACKEND_PID" 2>/dev/null || true
  wait "$BACKEND_PID" 2>/dev/null || true
}

if [ ! -f "$BACKUP_HOST_PATH" ]; then
  echo "== [$DB_NAME] Noch keine A1-Baseline gesichert =="
  start_backend

  echo ""
  echo "Jetzt pruefen, ob der aktuelle Inhalt dem bestaetigten Anfangsdatenbestand entspricht."
  echo "OpenAPI-Beschreibung je nach .NET-Version/Paket ausprobieren: ${BASE_URL}/openapi/v1.json oder ${BASE_URL}/swagger/v1/swagger.json"
  echo "Weicht er ab (z. B. durch eigene Testaktivitaet der Implementierung waehrend des Baus):"
  echo "  Strg+C hier, dann von Hand:"
  echo "    docker exec ${SQLSERVER_CONTAINER} /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P \"\$MSSQL_SA_PASSWORD\" -C \\"
  echo "      -Q \"ALTER DATABASE [${DB_NAME}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [${DB_NAME}];\""
  echo "  und danach dieses Skript erneut starten, damit die Anwendung sich beim naechsten Start selbst neu seedet."
  echo "  (Der Rohzustand ist durch backup-rohzustand.sh bereits separat gesichert, das DROP hier ist unbedenklich.)"
  read -r -p "Entspricht der Inhalt dem Anfangsdatenbestand? Weiter mit dem Backup als Baseline (Enter druecken) "

  docker exec "$SQLSERVER_CONTAINER" mkdir -p /var/opt/mssql/backup
  sqlcmd -Q "BACKUP DATABASE [$DB_NAME] TO DISK = N'$BACKUP_CONTAINER_PATH' WITH INIT;"

  mkdir -p "${RUN_DIR}/artifacts"
  docker cp "${SQLSERVER_CONTAINER}:${BACKUP_CONTAINER_PATH}" "$BACKUP_HOST_PATH"
  echo "Baseline gesichert: $BACKUP_HOST_PATH"

  stop_backend
fi

echo "== [$DB_NAME] Restore auf die gesicherte Baseline =="
docker cp "$BACKUP_HOST_PATH" "${SQLSERVER_CONTAINER}:${BACKUP_CONTAINER_PATH}"
# docker cp legt die Datei im Container mit Rechten an, unter denen der
# mssql-Serverprozess (User "mssql", nicht root) sie oft nicht oeffnen kann
# (Fehler 3201 / OS error 5, Access is denied). Deshalb hier freigeben.
# "docker exec" laeuft standardmaessig als Container-User "mssql", nicht
# root, und der kann eine per "docker cp" (meist root-eigen) hereinkopierte
# Datei nicht chmod'en. Deshalb hier explizit als root ausfuehren.
docker exec -u root "$SQLSERVER_CONTAINER" chmod 666 "$BACKUP_CONTAINER_PATH"
sqlcmd -Q "
ALTER DATABASE [$DB_NAME] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
RESTORE DATABASE [$DB_NAME] FROM DISK = N'$BACKUP_CONTAINER_PATH' WITH REPLACE;
ALTER DATABASE [$DB_NAME] SET MULTI_USER;
"

echo "== [$DB_NAME] Backend starten, Testsuite ausfuehren =="
start_backend

mkdir -p "${RUN_DIR}/metrics"
TEST_EXIT=0
dotnet test a1-testsuite/xunit-project \
  --filter "FullyQualifiedName~${DB_NAME}_Tests" \
  --logger "trx;LogFileName=$(pwd)/${RUN_DIR}/metrics/a1.trx" \
  || TEST_EXIT=$?

stop_backend

echo ""
echo "== [$DB_NAME] Fertig =="
echo "Ergebnis: ${RUN_DIR}/metrics/a1.trx"
exit "$TEST_EXIT"
