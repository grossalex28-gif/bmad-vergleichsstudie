#!/usr/bin/env bash
# Zweite Ergaenzung zu hole-beispiele.sh:
#   - a_solo_3: die Suche nach "Kammerkonzert Fruehling" schlug fehl, weil
#     diese Implementierung randomisierte Testveranstaltungstitel seedet
#     ("Testveranstaltung <hash>"). Nimmt stattdessen einfach die erste
#     Veranstaltung aus der Liste.
#   - b_bmad_3: POST /api/orders und GET /api/orders/{id} sind in der
#     OpenAPI-Beschreibung nur mit "200 OK" ohne Response-Schema
#     dokumentiert. Legt eine echte Testbestellung an (Produkt P11 /
#     Lieferant L4, siehe b_bmad_3_product_detail.json) und liest die
#     Antwortform direkt ab.
#
# Voraussetzung: MSSQL_SA_PASSWORD ist gesetzt.
# Aufruf: ./hole-beispiele-2.sh

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

echo "== [a_solo_3] Sitzplatz-Enum ermitteln (erste Veranstaltung statt Titelsuche) =="
if start_backend "runs/A/solo/03" "a_solo_3"; then
  EVENT_ID=$(python3 -c "import json; print(json.load(open('${AUSGABE_ORDNER}/a_solo_3_veranstaltungen.json'))[0]['id'])" 2>/dev/null) || true
  if [ -n "${EVENT_ID:-}" ]; then
    curl -s "${BASE_URL}/api/veranstaltungen/${EVENT_ID}/sitzplan" -o "${AUSGABE_ORDNER}/a_solo_3_sitzplan.json"
    echo "  gespeichert: ${AUSGABE_ORDNER}/a_solo_3_sitzplan.json (Veranstaltung: ${EVENT_ID})"
  else
    echo "  Konnte keine Veranstaltungs-ID aus a_solo_3_veranstaltungen.json lesen." >&2
  fi
  stop_backend
fi

echo "== [b_bmad_3] Bestell-Antwortform ermitteln =="
if start_backend "runs/B/bmad/03" "b_bmad_3"; then
  ORDER_BODY='{"name":"Testkunde","street":"Teststr. 1","postalCode":"12345","city":"Teststadt","country":"Deutschland","email":"test@example.com","items":[{"productId":"P11","supplierId":"L4","quantity":1}]}'
  curl -s -X POST "${BASE_URL}/api/orders" -H "Content-Type: application/json" -d "$ORDER_BODY" -o "${AUSGABE_ORDNER}/b_bmad_3_order_create_response.json"
  echo "  gespeichert: ${AUSGABE_ORDNER}/b_bmad_3_order_create_response.json"
  ORDER_ID=$(find_first_id "${AUSGABE_ORDNER}/b_bmad_3_order_create_response.json") || true
  if [ -n "${ORDER_ID:-}" ]; then
    curl -s "${BASE_URL}/api/orders/${ORDER_ID}" -o "${AUSGABE_ORDNER}/b_bmad_3_order_get_response.json"
    echo "  gespeichert: ${AUSGABE_ORDNER}/b_bmad_3_order_get_response.json (Bestell-ID: ${ORDER_ID})"
  else
    echo "  Konnte keine Bestell-ID aus der Anlage-Antwort lesen -- Antwortinhalt pruefen:" >&2
    cat "${AUSGABE_ORDNER}/b_bmad_3_order_create_response.json" >&2
  fi
  stop_backend
fi

echo ""
echo "Fertig. Ergebnisse in ${AUSGABE_ORDNER}/:"
ls -la "$AUSGABE_ORDNER" 2>/dev/null || true
