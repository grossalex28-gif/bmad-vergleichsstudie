#!/usr/bin/env bash
# Ergaenzung zu hole-openapi.sh: fuer drei Implementierungen reicht die
# generierte OpenAPI-Beschreibung allein nicht aus, um die Adapter zu bauen
# (Abschnitt 6 der Akzeptanztest-Architektur verlangt eigentlich
# "ausschliesslich anhand der OpenAPI-Beschreibung", das stoesst hier aber
# an eine Grenze):
#
#   - a_bmad_1, a_solo_3: die Sitzplatz-Status-/Typ-Enums (SeatStatus bzw.
#     SitzplatzTyp/SitzplatzStatus) sind in der OpenAPI-Beschreibung nur als
#     "type: integer" dokumentiert, ohne die zugehoerigen Bezeichner (kein
#     "enum"/"x-enumNames"). Aus der Zahl allein ist nicht ablesbar, welcher
#     Wert "Belegt" bzw. "Gang" bedeutet.
#   - b_bmad_3: die generierte OpenAPI-Beschreibung dokumentiert dort
#     ueberhaupt nur die Request-Bodies (CreateOrderRequestDto,
#     OrderLineRequestDto, RatingRequestDto), keine einzige Response-Form.
#
# Dieses Skript holt fuer diese drei Implementierungen je einmal eine echte
# Beispielantwort, damit sich Enum-Werte bzw. Feldnamen direkt am JSON
# ablesen lassen. Aendert an den Datenbanken nichts dauerhaft Relevantes:
# das sind reine GET-Aufrufe (ausser Skriptende-Hinweis fuer b_bmad_3, das
# noch einen manuellen Folgeschritt braucht).
#
# Voraussetzung: MSSQL_SA_PASSWORD ist gesetzt.
# Aufruf: ./hole-beispiele.sh

set -uo pipefail

if [ -z "${MSSQL_SA_PASSWORD:-}" ]; then
  echo "MSSQL_SA_PASSWORD ist nicht gesetzt. Vorher: export MSSQL_SA_PASSWORD='<passwort>'" >&2
  exit 1
fi
if ! command -v python3 >/dev/null 2>&1; then
  echo "python3 wird fuer die JSON-Auswertung benoetigt, ist aber nicht gefunden." >&2
  exit 1
fi

BACKEND_PORT=5001
BASE_URL="http://localhost:${BACKEND_PORT}"
AUSGABE_ORDNER="a1-testsuite/openapi-samples"
mkdir -p "$AUSGABE_ORDNER"

start_backend() {
  local run_dir="$1" db_name="$2"
  local backend_csproj
  backend_csproj=$(find "$run_dir/code" -maxdepth 4 -iname "*.csproj" 2>/dev/null | grep -vi test | head -n1 || true)
  if [ -z "$backend_csproj" ]; then
    echo "  Kein Backend-Projekt unter ${run_dir}/code gefunden." >&2
    return 1
  fi
  echo "  Starte: $backend_csproj"
  ConnectionStrings__Default="Server=localhost,1433;Database=${db_name};User Id=sa;Password=${MSSQL_SA_PASSWORD};TrustServerCertificate=True" \
  ASPNETCORE_URLS="$BASE_URL" \
  ASPNETCORE_ENVIRONMENT="Development" \
  dotnet run --project "$backend_csproj" --no-launch-profile &
  BACKEND_PID=$!

  local tries=0
  while ! curl -s -o /dev/null "${BASE_URL}/"; do
    if ! kill -0 "$BACKEND_PID" 2>/dev/null; then
      echo "  Backend-Prozess vorzeitig beendet." >&2
      return 1
    fi
    sleep 1
    tries=$((tries + 1))
    if [ "$tries" -gt 60 ]; then
      echo "  Backend antwortet nach 60s nicht." >&2
      kill "$BACKEND_PID" 2>/dev/null || true
      return 1
    fi
  done
  echo "  Backend antwortet."
}

stop_backend() {
  kill "$BACKEND_PID" 2>/dev/null || true
  wait "$BACKEND_PID" 2>/dev/null || true
}

# Sucht rekursiv im geparsten JSON nach dem ersten Dict, das irgendwo einen
# String enthaelt, der $2 als Teilstring hat, und gibt dessen "id"/"Id" aus.
find_id_by_text() {
  python3 -c "
import json, sys
gesucht = sys.argv[2]
def suche(o):
    if isinstance(o, dict):
        werte = [v for v in o.values() if isinstance(v, str)]
        if any(gesucht in v for v in werte):
            for key in ('id', 'Id', 'ID'):
                if key in o:
                    return o[key]
        for v in o.values():
            r = suche(v)
            if r is not None:
                return r
    elif isinstance(o, list):
        for e in o:
            r = suche(e)
            if r is not None:
                return r
    return None
d = json.load(open(sys.argv[1]))
r = suche(d)
if r is None:
    sys.exit(1)
print(r)
" "$1" "$2"
}

# Sucht rekursiv das erste Dict mit einem "id"-Feld (fuer b_bmad_3, wo die
# Feldnamen komplett unbekannt sind).
find_first_id() {
  python3 -c "
import json, sys
def suche(o):
    if isinstance(o, dict):
        for key in ('id', 'Id', 'ID'):
            if key in o:
                return o[key]
        for v in o.values():
            r = suche(v)
            if r is not None:
                return r
    elif isinstance(o, list):
        for e in o:
            r = suche(e)
            if r is not None:
                return r
    return None
d = json.load(open(sys.argv[1]))
r = suche(d)
if r is None:
    sys.exit(1)
print(r)
" "$1"
}

echo "== [a_bmad_1] Sitzplatz-Enum ermitteln =="
if start_backend "runs/A/bmad/01" "a_bmad_1"; then
  curl -s "${BASE_URL}/api/events" -o "${AUSGABE_ORDNER}/a_bmad_1_events.json"
  EVENT_ID=$(find_id_by_text "${AUSGABE_ORDNER}/a_bmad_1_events.json" "Kammerkonzert Frühling") || true
  if [ -n "${EVENT_ID:-}" ]; then
    curl -s "${BASE_URL}/api/events/${EVENT_ID}/sitzplan" -o "${AUSGABE_ORDNER}/a_bmad_1_sitzplan.json"
    echo "  gespeichert: ${AUSGABE_ORDNER}/a_bmad_1_sitzplan.json"
  else
    echo "  Konnte die Veranstaltung 'Kammerkonzert Frühling' nicht in der Antwort finden." >&2
  fi
  stop_backend
fi

echo "== [a_solo_3] Sitzplatz-Enum ermitteln =="
if start_backend "runs/A/solo/03" "a_solo_3"; then
  curl -s "${BASE_URL}/api/veranstaltungen" -o "${AUSGABE_ORDNER}/a_solo_3_veranstaltungen.json"
  EVENT_ID=$(find_id_by_text "${AUSGABE_ORDNER}/a_solo_3_veranstaltungen.json" "Kammerkonzert Frühling") || true
  if [ -n "${EVENT_ID:-}" ]; then
    curl -s "${BASE_URL}/api/veranstaltungen/${EVENT_ID}/sitzplan" -o "${AUSGABE_ORDNER}/a_solo_3_sitzplan.json"
    echo "  gespeichert: ${AUSGABE_ORDNER}/a_solo_3_sitzplan.json"
  else
    echo "  Konnte die Veranstaltung 'Kammerkonzert Frühling' nicht in der Antwort finden." >&2
  fi
  stop_backend
fi

echo "== [b_bmad_3] Antwortformen ermitteln (Response-Schemas fehlen komplett in der OpenAPI-Datei) =="
if start_backend "runs/B/bmad/03" "b_bmad_3"; then
  curl -s "${BASE_URL}/api/categories" -o "${AUSGABE_ORDNER}/b_bmad_3_categories.json"
  echo "  gespeichert: ${AUSGABE_ORDNER}/b_bmad_3_categories.json"
  curl -s "${BASE_URL}/api/products?page=1" -o "${AUSGABE_ORDNER}/b_bmad_3_products.json"
  echo "  gespeichert: ${AUSGABE_ORDNER}/b_bmad_3_products.json"
  PRODUCT_ID=$(find_first_id "${AUSGABE_ORDNER}/b_bmad_3_products.json") || true
  if [ -n "${PRODUCT_ID:-}" ]; then
    curl -s "${BASE_URL}/api/products/${PRODUCT_ID}" -o "${AUSGABE_ORDNER}/b_bmad_3_product_detail.json"
    echo "  gespeichert: ${AUSGABE_ORDNER}/b_bmad_3_product_detail.json (Produkt-ID: ${PRODUCT_ID})"
  else
    echo "  Konnte keine Produkt-ID aus der Produktliste lesen." >&2
  fi
  stop_backend
fi

echo ""
echo "Fertig. Ergebnisse in ${AUSGABE_ORDNER}/:"
ls -la "$AUSGABE_ORDNER" 2>/dev/null || true
echo ""
echo "Fuer b_bmad_3 fehlt danach noch ein manueller Folgeschritt (Beispiel"
echo "fuer POST /api/orders und dessen Response-Form) -- folgt, sobald die"
echo "Produkt-Beispielantwort ausgewertet ist (Lieferanten-Feldname darin"
echo "noch unbekannt)."
