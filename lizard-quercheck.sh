#!/usr/bin/env bash
#
# lizard-quercheck.sh
#
# Unabhaengiger Quercheck zu SonarQubes A3 (zyklomatische Komplexitaet) mit
# dem Werkzeug "lizard" (sprachagnostisch, eigene Implementierung, siehe
# https://github.com/terryyin/lizard). Spiegelt bewusst exakt dieselbe
# Seed-Ausschluss-Logik wie cloc-neuzaehlung.sh (Hardlink-Arbeitskopie,
# Seed-Dateien vorab entfernen), damit lizard auf demselben bereinigten
# Codebestand misst wie die cloc-Neuzaehlung.
#
# CSV-Spaltenlayout von "lizard --csv" (empirisch bestaetigt, 03.09.2026):
#   NLOC,CCN,token,PARAM,length,location,file,function,long_name,start,end
# -> Spalte 2 (Index 1) ist CCN.
#
# Ablage:
#   runs/<Projekt>/<Ansatz>/<NN>/metrics/lizard.json  (+ lizard.csv, roh)
#   lizard-ergebnisse/<name>_vollstaendig.json          (+ .csv, roh)
#
# Aufruf:
#   ./lizard-quercheck.sh check          # nur lizard-Verfuegbarkeit pruefen
#   ./lizard-quercheck.sh dry A bmad 01  # ein Lauf, nur anzeigen, nichts speichern
#   ./lizard-quercheck.sh runs           # alle zwoelf KI-Laeufe
#   ./lizard-quercheck.sh baseline       # OSS-Baselines (vollstaendig)
#   ./lizard-quercheck.sh alle           # runs + baseline

set -euo pipefail

REPO_ROOT="${REPO_ROOT:-$HOME/dev/Bachelor/bmad-vergleichsstudie}"
OSS_A_DIR="${OSS_A_DIR:-$HOME/dev/Bachelor/baseline/angularbooking}"
OSS_B_DIR="${OSS_B_DIR:-$HOME/dev/Bachelor/baseline/Godsend}"

# Dieselben Verzeichnis-Ausschluesse wie in cloc-neuzaehlung.sh, nur im
# lizard-Glob-Format (-x wird mehrfach angegeben).
EXCLUDE_ARGS=(
  -x "*/node_modules/*" -x "*/bin/*" -x "*/obj/*" -x "*/dist/*"
  -x "*/wwwroot/*" -x "*/.claude/*" -x "*/.agents/*" -x "*/_bmad/*"
  -x "*/_bmad-output/*" -x "*/Migrations/*" -x "*/.git/*"
)

ERGEBNIS_DIR="$REPO_ROOT/lizard-ergebnisse"
mkdir -p "$ERGEBNIS_DIR"

trap 'rm -rf "$REPO_ROOT"/.lizard-tmp-* 2>/dev/null || true' EXIT

pruefe_lizard() {
  if ! command -v lizard >/dev/null 2>&1; then
    echo "FEHLER: 'lizard' nicht im PATH. Installation: pipx install lizard" >&2
    exit 1
  fi
  echo "lizard gefunden: $(command -v lizard) ($(lizard --version 2>&1 | head -1))"
}

# Wertet eine bereits erzeugte CSV-Datei aus (Spalte 2 = CCN) und schreibt
# das JSON-Ergebnis. $1 = CSV-Pfad, $2 = Ziel-JSON-Pfad.
auswerten() {
  local csv_pfad="$1" ziel_json="$2"
  # WICHTIG (Bugfix 03.09.2026): "python3 - ... < csv <<PYEOF" schlaegt fehl,
  # weil bash bei mehreren Stdin-Umlenkungen im selben Kommando nur die
  # LETZTE (hier: das Heredoc) tatsaechlich anwendet. Der Heredoc-Inhalt
  # (das Python-Skript selbst) wird dadurch als Stdin von "python3 -"
  # gelesen und dabei vollstaendig konsumiert -- die eigentliche CSV-Datei
  # kommt nie an, csv.reader(sys.stdin) liefert 0 Zeilen. Deshalb hier das
  # Python-Skript in eine echte temporaere Datei schreiben und die CSV erst
  # beim SEPARATEN Ausfuehren des Skripts als Stdin anhaengen.
  local py_tmp
  py_tmp=$(mktemp --suffix=.py)
  cat > "$py_tmp" <<'PYEOF'
import csv, json, sys, statistics
ziel = sys.argv[1]
rows = list(csv.reader(sys.stdin))
ccns = [int(r[1]) for r in rows if len(r) > 1 and r[1].strip() != ""]
n = len(ccns)
if n == 0:
    ergebnis = {"funktionsanzahl": 0, "median_ccn": None, "mittelwert_ccn": None,
                "anteil_ueber_10": None, "anzahl_ueber_10": 0}
else:
    ueber_10 = sum(1 for c in ccns if c > 10)
    ergebnis = {
        "funktionsanzahl": n,
        "median_ccn": statistics.median(ccns),
        "mittelwert_ccn": round(sum(ccns) / n, 3),
        "anteil_ueber_10": round(ueber_10 / n, 4),
        "anzahl_ueber_10": ueber_10,
    }
with open(ziel, "w") as f:
    json.dump(ergebnis, f, indent=2, ensure_ascii=False)
print(json.dumps(ergebnis, indent=2, ensure_ascii=False))
PYEOF
  python3 "$py_tmp" "$ziel_json" < "$csv_pfad"
  rm -f "$py_tmp"
}

seed_liste_projekt() {
  local projekt="$1"
  local seed_dir="$REPO_ROOT/seed/$projekt"
  if [ ! -d "$seed_dir" ] || [ ! -e "$seed_dir/.git" ]; then
    echo "FEHLER: Seed-Submodul fehlt/nicht initialisiert: $seed_dir" >&2
    exit 1
  fi
  local liste
  liste=$(mktemp)
  git -C "$seed_dir" ls-tree -r --name-only HEAD > "$liste"
  echo "$liste"
}

messe_lauf() {
  local projekt="$1" ansatz="$2" nn="$3" nur_anzeigen="${4:-nein}"
  local run_dir="$REPO_ROOT/runs/$projekt/$ansatz/$nn/code"
  local out_dir="$REPO_ROOT/runs/$projekt/$ansatz/$nn/metrics"

  if [ ! -d "$run_dir" ]; then
    echo "FEHLT: $run_dir" >&2
    return 1
  fi

  local seed_liste_rel
  seed_liste_rel=$(seed_liste_projekt "$projekt")

  local tmp_container tmp_run
  tmp_container=$(mktemp -d "$REPO_ROOT/.lizard-tmp-XXXXXX")
  tmp_run="$tmp_container/code"
  cp -al "$run_dir" "$tmp_run"

  local entfernt=0
  while IFS= read -r rel; do
    [ -z "$rel" ] && continue
    if [ -f "$tmp_run/$rel" ]; then
      rm -f "$tmp_run/$rel"
      entfernt=$((entfernt + 1))
    fi
  done < "$seed_liste_rel"
  rm -f "$seed_liste_rel"
  echo "  -> $entfernt Seed-Dateien ausgeschlossen"

  local csv_tmp
  csv_tmp=$(mktemp)
  lizard "$tmp_run" "${EXCLUDE_ARGS[@]}" --csv > "$csv_tmp" 2>/dev/null || true

  if [ "$nur_anzeigen" = "ja" ]; then
    auswerten "$csv_tmp" /dev/stdout >/dev/null 2>&1 || true
    python3 -c "
import csv, statistics
rows = list(csv.reader(open('$csv_tmp')))
ccns = [int(r[1]) for r in rows if len(r) > 1 and r[1].strip() != '']
print(f'Funktionen: {len(ccns)}, Median CCN: {statistics.median(ccns) if ccns else None}, Anteil > 10: {round(sum(1 for c in ccns if c > 10)/len(ccns), 4) if ccns else None}')
"
  else
    mkdir -p "$out_dir"
    cp "$csv_tmp" "$out_dir/lizard.csv"
    auswerten "$csv_tmp" "$out_dir/lizard.json"
    echo "  -> gespeichert: $out_dir/lizard.json (+ lizard.csv)"
  fi

  rm -f "$csv_tmp"
  rm -rf "$tmp_container"
}

messe_baseline() {
  local name="$1" pfad="$2"
  if [ ! -d "$pfad" ]; then
    echo "FEHLT: $pfad -- OSS_A_DIR/OSS_B_DIR anpassen." >&2
    return 1
  fi
  local csv_tmp
  csv_tmp=$(mktemp)
  lizard "$pfad" "${EXCLUDE_ARGS[@]}" --csv > "$csv_tmp" 2>/dev/null || true
  cp "$csv_tmp" "$ERGEBNIS_DIR/${name}_vollstaendig.csv"
  auswerten "$csv_tmp" "$ERGEBNIS_DIR/${name}_vollstaendig.json"
  echo "  -> gespeichert: $ERGEBNIS_DIR/${name}_vollstaendig.json (+ .csv)"
  rm -f "$csv_tmp"
}

MODUS="${1:-}"
case "$MODUS" in
  check)
    pruefe_lizard
    ;;
  dry)
    pruefe_lizard
    messe_lauf "${2:?z.B. A}" "${3:?z.B. bmad}" "${4:?z.B. 01}" "ja"
    ;;
  runs)
    pruefe_lizard
    for projekt in A B; do
      for ansatz in bmad solo; do
        for nn in 01 02 03; do
          echo "Messe $projekt-$ansatz-$nn ..."
          messe_lauf "$projekt" "$ansatz" "$nn"
        done
      done
    done
    echo "Fertig. Zwoelf Ergebnisse unter runs/<Projekt>/<Ansatz>/<NN>/metrics/lizard.json"
    ;;
  baseline)
    pruefe_lizard
    echo "Messe Baseline A (vollstaendig) ..."
    messe_baseline "projektA" "$OSS_A_DIR"
    echo "Messe Baseline B (vollstaendig) ..."
    messe_baseline "projektB" "$OSS_B_DIR"
    ;;
  alle)
    "$0" runs
    "$0" baseline
    ;;
  *)
    cat >&2 <<EOF
Aufruf:
  $0 check              # lizard-Verfuegbarkeit pruefen
  $0 dry <P> <A> <NN>   # ein Lauf, z.B. $0 dry A bmad 01
  $0 runs               # alle zwoelf KI-Laeufe
  $0 baseline           # OSS-Baselines (vollstaendig)
  $0 alle               # runs + baseline
EOF
    exit 1
    ;;
esac
