#!/usr/bin/env bash
#
# dependency-scan.sh
#
# A8 (Security), Abhaengigkeits-Befunde: "dotnet list package --vulnerable"
# und "npm audit" je Metrikkatalog.md Abschnitt 2 ("A8 trennt Eigen- und
# Abhaengigkeitsbefunde"). Ergaenzt sonar_metriken_abziehen.py, das nur
# Eigenbefunde liefert.
#
# UNGETESTET. Aufruf:
#   ./dependency-scan.sh lauf A bmad 01
#   ./dependency-scan.sh runs
#   ./dependency-scan.sh baseline A modulscharf   # modulscharf/vollstaendig macht hier
#                                                   # keinen Unterschied (Abhaengigkeiten
#                                                   # sind global je Projekt, nicht je Feature) --
#                                                   # wird nur einmal je Projekt erhoben
#   ./dependency-scan.sh baselines
set -euo pipefail

REPO_ROOT="${REPO_ROOT:-$HOME/dev/Bachelor/bmad-vergleichsstudie}"
OSS_A_DIR="${OSS_A_DIR:-$HOME/dev/Bachelor/baseline/angularbooking}"
OSS_B_DIR="${OSS_B_DIR:-$HOME/dev/Bachelor/baseline/Godsend}"
ERGEBNIS_DIR="$REPO_ROOT/sonar-ergebnisse/abhaengigkeiten"
mkdir -p "$ERGEBNIS_DIR"

erhebe() {
  local key="$1" backend_dir="$2" frontend_dir="$3"
  echo "== $key =="
  if [ -d "$backend_dir" ]; then
    ( cd "$backend_dir" && dotnet list package --vulnerable --include-transitive ) \
      > "$ERGEBNIS_DIR/${key}-dotnet.txt" 2>&1 || true
    echo "  -> $ERGEBNIS_DIR/${key}-dotnet.txt"
  fi
  if [ -d "$frontend_dir" ]; then
    ( cd "$frontend_dir" && npm audit --json ) > "$ERGEBNIS_DIR/${key}-npm.json" 2>&1 || true
    echo "  -> $ERGEBNIS_DIR/${key}-npm.json"
  fi
}

MODUS="${1:-}"
case "$MODUS" in
  lauf)
    projekt="${2:?}"; ansatz="${3:?}"; nn="${4:?}"
    projekt_klein=$(echo "$projekt" | tr 'A-Z' 'a-z')
    code="$REPO_ROOT/runs/$projekt/$ansatz/$nn/code"
    erhebe "${projekt}-${ansatz}-${nn}" \
      "$code/seed-${projekt_klein}-backend.Api" \
      "$code/seed-${projekt_klein}-frontend"
    ;;
  runs)
    for projekt in A B; do
      for ansatz in bmad solo; do
        for nn in 01 02 03; do
          "$0" lauf "$projekt" "$ansatz" "$nn"
        done
      done
    done
    ;;
  baseline)
    projekt="${2:?A|B}"
    if [ "$projekt" = "A" ]; then
      erhebe "baseline-A" "$OSS_A_DIR/AngularBooking" "$OSS_A_DIR/AngularBooking/ClientApp"
    else
      erhebe "baseline-B" "$OSS_B_DIR/Godsend" "$OSS_B_DIR/Client"
    fi
    ;;
  baselines)
    "$0" baseline A
    "$0" baseline B
    ;;
  *)
    echo "Aufruf: $0 {lauf <P> <A> <NN> | runs | baseline <P> | baselines}" >&2
    exit 1
    ;;
esac
