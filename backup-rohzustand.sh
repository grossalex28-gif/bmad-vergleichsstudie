#!/usr/bin/env bash
# Einmalig VOR jeder weiteren Arbeit am Datenbestand ausfuehren.
# Sichert den jetzigen, unveraenderten Rohzustand aller zwoelf vom Harness
# angelegten Datenbanken, unabhaengig davon, ob dieser Zustand spaeter als
# A1-Baseline taugt. Veraendert nichts, loescht nichts.
#
# Voraussetzung: Container "sqlserver-bachelor" laeuft, MSSQL_SA_PASSWORD
# ist gesetzt.

set -euo pipefail

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "MSSQL_SA_PASSWORD ist nicht gesetzt." >&2
  exit 1
fi

SQLSERVER_CONTAINER="sqlserver-bachelor"
OUT_DIR="./rohzustand_backups"
mkdir -p "$OUT_DIR"

sqlcmd() {
  docker exec "$SQLSERVER_CONTAINER" /opt/mssql-tools18/bin/sqlcmd -S localhost -U sa -P "$MSSQL_SA_PASSWORD" -C "$@"
}

docker exec "$SQLSERVER_CONTAINER" mkdir -p /var/opt/mssql/backup

DBS=(a_bmad_1 a_bmad_2 a_bmad_3 a_solo_1 a_solo_2 a_solo_3 \
     b_bmad_1 b_bmad_2 b_bmad_3 b_solo_1 b_solo_2 b_solo_3)

for db in "${DBS[@]}"; do
  echo "== $db =="
  EXISTS=$(sqlcmd -h -1 -Q "SET NOCOUNT ON; SELECT CASE WHEN DB_ID('$db') IS NULL THEN 0 ELSE 1 END;" | tr -d '[:space:]')
  if [ "$EXISTS" != "1" ]; then
    echo "  nicht vorhanden, uebersprungen."
    continue
  fi
  sqlcmd -Q "BACKUP DATABASE [$db] TO DISK = N'/var/opt/mssql/backup/${db}_rohzustand.bak' WITH INIT;"
  docker cp "${SQLSERVER_CONTAINER}:/var/opt/mssql/backup/${db}_rohzustand.bak" "${OUT_DIR}/${db}_rohzustand.bak"
  echo "  gesichert nach ${OUT_DIR}/${db}_rohzustand.bak"
done

echo ""
echo "Fertig. Rohzustand aller vorhandenen Datenbanken liegt in ${OUT_DIR}/."
