#!/usr/bin/env bash
#
# cloc-neuzaehlung.sh
#
# Zaehlt Codezeilen je Lauf mit cloc 1.98 nach der projektweiten Ausschlussliste
# aus claude/Bachelorarbeit_Gesamtstand.md Abschnitt 11 und
# claude/Erhebung_abgeschlossen.md Abschnitt 5.
#
# Ablage passend zum bestehenden Schema:
#   runs/<Projekt>/<Ansatz>/<NN>/metrics/cloc.json   (die zwoelf KI-Laeufe)
#   cloc-ergebnisse/<name>.json                       (OSS-Baselines, "vollstaendig")
#
# SEED-AUSSCHLUSS (Version 4, korrigiert):
#   runs/<P>/<A>/<NN>/code hat KEIN eigenes Git-Repo -- das gesamte
#   bmad-vergleichsstudie-Repo ist ein einziges Mono-Repo, die Lauf-Ordner
#   sind darin nur normale Unterverzeichnisse (bestaetigt per
#   `git -C runs/A/bmad/01/code rev-parse --show-toplevel`).
#   Die Seed-Repositories sind stattdessen als Git-Submodule unter seed/A
#   und seed/B eingebunden, jeweils auf einem fest gepinnten Commit
#   (siehe `git submodule status` im Repo-Root). Da der Seed in allen
#   sechs Laeufen eines Projekts byteidentisch ist, wird die relative
#   Seed-Dateiliste nur EINMAL PRO PROJEKT aus seed/A bzw. seed/B erzeugt
#   (nicht pro Lauf).
#   cloc's eigene "--exclude-list-file"-Option matcht Pfade unzuverlaessig
#   (im Testlauf vom 01.09. bit-identisches Ergebnis trotz korrekt
#   gefundener Seed-Dateien) und wird daher NICHT verwendet. Stattdessen
#   wird je Lauf eine Hardlink-Arbeitskopie angelegt (cp -al, kein
#   Byte-Kopieraufwand), daraus werden genau die Seed-Pfade entfernt, und
#   cloc zaehlt danach ganz normal auf der bereinigten Kopie.
#
# Aufruf:
#   ./cloc-neuzaehlung.sh check          # nur cloc-Version pruefen/installieren
#   ./cloc-neuzaehlung.sh dry A bmad 01  # EINEN Lauf testen, Ergebnis anzeigen, nichts speichern
#   ./cloc-neuzaehlung.sh runs           # alle zwoelf KI-Laeufe zaehlen und speichern
#   ./cloc-neuzaehlung.sh baseline       # OSS-Baselines ("vollstaendig") zaehlen
#   ./cloc-neuzaehlung.sh alle           # runs + baseline
#
set -euo pipefail

# ---------------------------------------------------------------------------
# Konfiguration -- bitte vor dem ersten Lauf pruefen/anpassen
# ---------------------------------------------------------------------------

REPO_ROOT="${REPO_ROOT:-$HOME/dev/Bachelor/bmad-vergleichsstudie}"
CLOC_VERSION_ERWARTET="1.98"
CLOC_PL="/tmp/cloc-${CLOC_VERSION_ERWARTET}.pl"

# TODO: Pfade zu deinen lokalen Checkouts der beiden OSS-Referenzprojekte
# eintragen (siehe claude/Referenzprojekte_Auswahl.md bzw. deine eigene
# Ablage). Falls noch kein Checkout existiert:
#   git clone https://github.com/adamlarner/angularbooking.git <Pfad>
#   git -C <Pfad> checkout <commit-hash aus deinen Notizen, siehe
#     claude/Harness_Probelauf_Befunde.md Abschnitt 4a: "51f5283..." fuer A,
#     "dc97055..." fuer B -- dort nur gekuerzt notiert, vollen Hash aus der
#     eigenen HarnessTesting/catalogs/{A,B}/quelle.json nachschlagen>
OSS_A_DIR="${OSS_A_DIR:-$HOME/dev/Bachelor/baseline/angularbooking}"
OSS_B_DIR="${OSS_B_DIR:-$HOME/dev/Bachelor/baseline/Godsend}"

# Verzeichnis-Ausschluesse (gelten fuer KI-Laeufe UND Baselines)
EXCLUDE_DIRS="node_modules,bin,obj,dist,wwwroot,.claude,.agents,_bmad,_bmad-output,Migrations,.git"

# Datei-Ausschluesse per Namensmuster (Lock-Dateien)
EXCLUDE_FILE_PATTERN='(^|/)(package-lock\.json|yarn\.lock|npm-shrinkwrap\.json)$'

ERGEBNIS_DIR="$REPO_ROOT/cloc-ergebnisse"
mkdir -p "$ERGEBNIS_DIR"

# Aufraeumen der temporaeren Hardlink-Arbeitskopien (.cloc-tmp-*), falls das
# Skript irgendwo mittendrin abbricht.
trap 'rm -rf "$REPO_ROOT"/.cloc-tmp-* 2>/dev/null || true' EXIT

# ---------------------------------------------------------------------------
# Hilfsfunktionen
# ---------------------------------------------------------------------------

pruefe_cloc() {
  if command -v cloc >/dev/null 2>&1 && [ "$(cloc --version)" = "$CLOC_VERSION_ERWARTET" ]; then
    echo "cloc $CLOC_VERSION_ERWARTET bereits vorhanden (Systeminstallation)."
    CLOC_CMD="cloc"
    return
  fi
  if [ -f "$CLOC_PL" ]; then
    echo "Verwende gepinnte Version unter $CLOC_PL"
    CLOC_CMD="perl $CLOC_PL"
    return
  fi
  echo "Lade cloc $CLOC_VERSION_ERWARTET herunter (gepinnt, unabhaengig von der Distro-Version) ..."
  curl -fsSL -o "$CLOC_PL" \
    "https://github.com/AlDanial/cloc/releases/download/v${CLOC_VERSION_ERWARTET}/cloc-${CLOC_VERSION_ERWARTET}.pl"
  CLOC_CMD="perl $CLOC_PL"
  echo "OK: $($CLOC_CMD --version)"
}

# Ermittelt einmal pro Projekt die relative Dateiliste des Seed-Submoduls
# (seed/A bzw. seed/B) am gepinnten Commit und cached sie in einer
# temporaeren Datei.
#
# WICHTIG: bewusst OHNE Command-Substitution ($(...)) aufgerufen, sondern
# ueber die globale Variable SEED_LISTE_REL, die die Funktion setzt. Ein
# $(...) wuerde die Funktion in einer Subshell laufen lassen -- darin wuerde
# ein Fehlschlag unter "set -e" bei einer Zuweisung wie "x=$(funktion)"
# NICHT den Skriptabbruch ausloesen, sondern x wuerde einfach leer bleiben
# und der Fehler wuerde still verschwinden (das war exakt die Ursache fuer
# die vorherigen "0 Seed-Dateien" ganz ohne jede Fehlermeldung).
declare -A SEED_LISTE_REL_CACHE
SEED_LISTE_REL=""

seed_liste_projekt() {
  local projekt="$1"
  if [ -n "${SEED_LISTE_REL_CACHE[$projekt]:-}" ]; then
    SEED_LISTE_REL="${SEED_LISTE_REL_CACHE[$projekt]}"
    return
  fi
  local seed_dir="$REPO_ROOT/seed/$projekt"
  if [ ! -d "$seed_dir" ]; then
    echo "FEHLER: Seed-Submodul-Verzeichnis fehlt: $seed_dir" >&2
    exit 1
  fi
  if [ ! -e "$seed_dir/.git" ]; then
    echo "FEHLER: $seed_dir ist kein initialisiertes Submodul (.git fehlt dort)." >&2
    echo "        Im Repo-Root ausfuehren: git submodule update --init --recursive" >&2
    exit 1
  fi
  local liste err
  liste=$(mktemp)
  err=$(mktemp)
  # ls-tree ohne Commit-Argument = HEAD des Submoduls, das laut
  # `git submodule status` bereits exakt auf dem gepinnten Commit steht.
  if ! git -C "$seed_dir" ls-tree -r --name-only HEAD > "$liste" 2>"$err"; then
    echo "FEHLER: 'git -C $seed_dir ls-tree -r --name-only HEAD' ist fehlgeschlagen:" >&2
    cat "$err" >&2
    rm -f "$liste" "$err"
    exit 1
  fi
  rm -f "$err"
  local n
  n=$(wc -l < "$liste")
  echo "  -> Seed-Referenz $projekt: $n Dateien aus $seed_dir (HEAD)" >&2
  if [ "$n" -eq 0 ]; then
    echo "FEHLER: $seed_dir liefert 0 Dateien laut 'git ls-tree HEAD'." >&2
    echo "        Zur Kontrolle im Repo-Root pruefen:" >&2
    echo "          git -C seed/$projekt status" >&2
    echo "          git -C seed/$projekt log --oneline -1" >&2
    echo "          ls seed/$projekt" >&2
    exit 1
  fi
  SEED_LISTE_REL_CACHE[$projekt]="$liste"
  SEED_LISTE_REL="$liste"
}

zaehle_lauf() {
  local projekt="$1" ansatz="$2" nn="$3" nur_anzeigen="${4:-nein}"
  local run_dir="$REPO_ROOT/runs/$projekt/$ansatz/$nn/code"
  local out_dir="$REPO_ROOT/runs/$projekt/$ansatz/$nn/metrics"

  if [ ! -d "$run_dir" ]; then
    echo "FEHLT: $run_dir" >&2
    return 1
  fi

  seed_liste_projekt "$projekt"
  local seed_liste_rel="$SEED_LISTE_REL"

  # WICHTIG (Version 4): cloc's --exclude-list-file hat sich im Testlauf
  # vom 01.09. als wirkungslos erwiesen -- trotz 34 korrekt gefundener
  # Seed-Pfade blieb die Zaehlung bit-identisch zur ungefilterten Zaehlung.
  # cloc matcht Pfade aus --exclude-list-file offenbar nicht zuverlaessig
  # gegen das, was beim Scannen eines absoluten Verzeichnisses tatsaechlich
  # verglichen wird (Normalisierung/Symlink-Aufloesung unklar). Statt das
  # cloc-interne Matching weiter zu debuggen: die Seed-Dateien werden jetzt
  # VOR dem Zaehlen physisch aus einer Arbeitskopie entfernt, und cloc
  # zaehlt danach ganz normal ohne --exclude-list-file. Die Arbeitskopie
  # entsteht per Hardlinks (cp -al) im selben Dateisystem wie REPO_ROOT
  # -- kein Kopieren von Bytes, keine zusaetzliche Wartezeit, und "rm" in
  # der Kopie loescht nur den Hardlink, nicht die Originaldatei.
  local tmp_container tmp_run
  tmp_container=$(mktemp -d "$REPO_ROOT/.cloc-tmp-XXXXXX")
  tmp_run="$tmp_container/code"
  cp -al "$run_dir" "$tmp_run"

  local gesucht=0 entfernt=0
  while IFS= read -r rel; do
    [ -z "$rel" ] && continue
    gesucht=$((gesucht + 1))
    if [ -f "$tmp_run/$rel" ]; then
      rm -f "$tmp_run/$rel"
      entfernt=$((entfernt + 1))
    fi
  done < "$seed_liste_rel"
  echo "  -> $entfernt von $gesucht Seed-Dateien im Lauf gefunden und ausgeschlossen"
  if [ "$entfernt" -lt "$gesucht" ]; then
    echo "     Hinweis: $((gesucht - entfernt)) Seed-Datei(en) existier(t/en) unter diesem Pfad nicht mehr im Lauf" \
         "(umbenannt/verschoben/geloescht durch die KI? ggf. manuell pruefen)." >&2
  fi

  local ziel
  if [ "$nur_anzeigen" = "ja" ]; then
    ziel="/dev/stdout"
  else
    mkdir -p "$out_dir"
    ziel="$out_dir/cloc.json"
  fi

  $CLOC_CMD "$tmp_run" \
    --exclude-dir="$EXCLUDE_DIRS" \
    --not-match-f="$EXCLUDE_FILE_PATTERN" \
    --json \
    --out="$ziel"

  rm -rf "$tmp_container"

  if [ "$nur_anzeigen" != "ja" ]; then
    echo "  -> gespeichert: $out_dir/cloc.json"
  fi
}

zaehle_baseline() {
  local name="$1" pfad="$2"
  if [ ! -d "$pfad" ]; then
    echo "FEHLT: $pfad -- OSS_A_DIR/OSS_B_DIR oben im Skript anpassen." >&2
    return 1
  fi
  $CLOC_CMD "$pfad" \
    --exclude-dir="$EXCLUDE_DIRS" \
    --not-match-f="$EXCLUDE_FILE_PATTERN" \
    --json \
    --out="$ERGEBNIS_DIR/${name}_vollstaendig.json"
  echo "  -> gespeichert: $ERGEBNIS_DIR/${name}_vollstaendig.json"
}

# ---------------------------------------------------------------------------
# Hauptprogramm
# ---------------------------------------------------------------------------

MODUS="${1:-}"

case "$MODUS" in
  check)
    pruefe_cloc
    ;;

  dry)
    pruefe_cloc
    projekt="${2:?Projekt angeben, z.B. A}"
    ansatz="${3:?Ansatz angeben, z.B. bmad}"
    nn="${4:?Laufnummer angeben, z.B. 01}"
    echo "Probelauf fuer $projekt-$ansatz-$nn (nichts wird gespeichert):"
    zaehle_lauf "$projekt" "$ansatz" "$nn" "ja"
    ;;

  runs)
    pruefe_cloc
    for projekt in A B; do
      for ansatz in bmad solo; do
        for nn in 01 02 03; do
          echo "Zaehle $projekt-$ansatz-$nn ..."
          zaehle_lauf "$projekt" "$ansatz" "$nn"
        done
      done
    done
    echo "Fertig. Zwoelf Ergebnisse unter runs/<Projekt>/<Ansatz>/<NN>/metrics/cloc.json"
    ;;

  baseline)
    pruefe_cloc
    echo "Zaehle Baseline A (vollstaendig) ..."
    zaehle_baseline "projektA" "$OSS_A_DIR"
    echo "Zaehle Baseline B (vollstaendig) ..."
    zaehle_baseline "projektB" "$OSS_B_DIR"
    echo "Hinweis: die 'modulscharfe' Baseline-Zaehlung braucht zusaetzlich die"
    echo "Feature-zu-Datei-Zuordnung (offener Punkt 2) und ist hier nicht enthalten."
    ;;

  alle)
    "$0" runs
    "$0" baseline
    ;;

  *)
    cat >&2 <<EOF
Aufruf:
  $0 check              # cloc-Version pruefen/installieren
  $0 dry <P> <A> <NN>   # einen Lauf testen, z.B. $0 dry A bmad 01
  $0 runs               # alle zwoelf KI-Laeufe zaehlen und speichern
  $0 baseline           # OSS-Baselines (vollstaendig) zaehlen
  $0 alle               # runs + baseline
EOF
    exit 1
    ;;
esac
