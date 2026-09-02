#!/usr/bin/env python3
"""
sonar_metriken_abziehen.py

Zieht A3, A5, A6, A7, A8 (Eigenbefunde) je SonarQube-Projekt-Key ueber die
Web-API ab und speichert das Ergebnis unter sonar-ergebnisse/<key>.json,
analog zur bestehenden Ablage in cloc-ergebnisse/.

UNGETESTET -- vorbereitet ohne laufende SonarQube-Instanz. Metrik-Schluessel
(ncloc, complexity, code_smells, sqale_debt_ratio, duplicated_lines_density,
vulnerabilities, functions) sind seit Jahren stabile SonarQube-Kernmetriken;
vor dem produktiven Einsatz trotzdem einmal gegen die eigene Instanz mit
--nur-anzeigen fuer EIN Projekt gegenpruefen (Web-API-Antwort kann sich in
Zukunft in Details unterscheiden).

Aufruf:
  export SONAR_TOKEN=...
  python3 sonar_metriken_abziehen.py --host http://localhost:9000 \
      --projekt bmad-vgl-a-bmad-01-backend
  python3 sonar_metriken_abziehen.py --host http://localhost:9000 --alle-aus-datei sonar-projekte.txt

sonar-projekte.txt: eine Projekt-Key pro Zeile (z.B. von sonar-scan.sh
gemeldete Keys sammeln, oder aus `api/projects/search` ableiten).
"""
import argparse
import json
import os
import sys
import urllib.request
import urllib.parse
import base64

METRIK_SCHLUESSEL = [
    "ncloc",                       # Grundlage fuer je-kLOC-Dichten (A6/A7/A8)
    "functions",                   # Grundlage fuer A3-Quercheck mit lizard
    "complexity",                  # A3, Summe zyklomatische Komplexitaet
    "cognitive_complexity",        # nur Anhang (A4)
    "code_smells",                 # A7
    "sqale_index",                 # A5, Aufwand in Minuten
    "sqale_debt_ratio",            # A5, technische Schuldenquote in %
    "duplicated_lines_density",    # A6, in %
    "duplicated_blocks",
    "vulnerabilities",             # A8, Eigenbefunde (Abhaengigkeits-CVEs separat, siehe dependency-scan.sh)
    "security_rating",
]


def api_get(host: str, token: str, pfad: str, query: dict):
    url = f"{host.rstrip('/')}{pfad}?{urllib.parse.urlencode(query)}"
    req = urllib.request.Request(url)
    auth = base64.b64encode(f"{token}:".encode()).decode()
    req.add_header("Authorization", f"Basic {auth}")
    with urllib.request.urlopen(req, timeout=30) as resp:
        return json.loads(resp.read().decode())


def hole_measures(host: str, token: str, projekt_key: str) -> dict:
    daten = api_get(
        host, token, "/api/measures/component",
        {"component": projekt_key, "metricKeys": ",".join(METRIK_SCHLUESSEL)},
    )
    measures = {m["metric"]: m.get("value") for m in daten.get("component", {}).get("measures", [])}
    return measures


def hole_vulnerabilities_nach_schweregrad(host: str, token: str, projekt_key: str) -> dict:
    daten = api_get(
        host, token, "/api/issues/search",
        {"componentKeys": projekt_key, "types": "VULNERABILITY", "facets": "severities", "ps": 1},
    )
    ergebnis = {}
    for facet in daten.get("facets", []):
        if facet.get("property") == "severities":
            for wert in facet.get("values", []):
                ergebnis[wert["val"]] = wert["count"]
    return ergebnis


def verarbeite(host: str, token: str, projekt_key: str, out_dir: str, nur_anzeigen: bool):
    measures = hole_measures(host, token, projekt_key)
    schweregrade = hole_vulnerabilities_nach_schweregrad(host, token, projekt_key)
    ergebnis = {
        "projekt_key": projekt_key,
        "sonarqube_host": host,
        "measures": measures,
        "vulnerabilities_nach_schweregrad": schweregrade,
    }
    if nur_anzeigen:
        print(json.dumps(ergebnis, indent=2, ensure_ascii=False))
        return
    os.makedirs(out_dir, exist_ok=True)
    ziel = os.path.join(out_dir, f"{projekt_key}.json")
    with open(ziel, "w", encoding="utf-8") as f:
        json.dump(ergebnis, f, indent=2, ensure_ascii=False)
    print(f"-> gespeichert: {ziel}")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--host", default=os.environ.get("SONAR_HOST_URL", "http://localhost:9000"))
    ap.add_argument("--out-dir", default=os.path.join(
        os.environ.get("REPO_ROOT", os.path.expanduser("~/dev/Bachelor/bmad-vergleichsstudie")),
        "sonar-ergebnisse",
    ))
    ap.add_argument("--projekt", help="ein einzelner Projekt-Key")
    ap.add_argument("--alle-aus-datei", help="Datei mit einem Projekt-Key je Zeile")
    ap.add_argument("--nur-anzeigen", action="store_true", help="nicht speichern, nur auf stdout ausgeben")
    args = ap.parse_args()

    token = os.environ.get("SONAR_TOKEN")
    if not token:
        sys.exit("FEHLER: SONAR_TOKEN nicht gesetzt.")

    keys = []
    if args.projekt:
        keys.append(args.projekt)
    if args.alle_aus_datei:
        with open(args.alle_aus_datei, encoding="utf-8") as f:
            keys.extend(z.strip() for z in f if z.strip() and not z.startswith("#"))
    if not keys:
        sys.exit("FEHLER: --projekt oder --alle-aus-datei angeben.")

    for key in keys:
        try:
            verarbeite(args.host, token, key, args.out_dir, args.nur_anzeigen)
        except Exception as e:
            print(f"FEHLER bei {key}: {e}", file=sys.stderr)


if __name__ == "__main__":
    main()
