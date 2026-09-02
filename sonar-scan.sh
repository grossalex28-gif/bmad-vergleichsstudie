#!/usr/bin/env bash
#
# sonar-scan.sh
#
# Fuehrt SonarQube-Analysen (A3/A5/A6/A7/A8, siehe Metrikkatalog.md) fuer die
# zwoelf KI-Laeufe und die vier Baseline-Varianten (je Projekt modulscharf +
# vollstaendig) durch. Getrennte Analyse je Schicht (Backend/Frontend), siehe
# Metrikkatalog.md Abschnitt 2 "Getrennte Auswertung nach Schicht".
#
# UNGETESTET -- vorbereitet in einer Sandbox ohne Docker/dotnet/Netzwerk.
# Vor dem produktiven Einsatz mit "dry" an genau einem Lauf/einer Baseline
# gegenpruefen, siehe docs/SonarQube_Setup.md Abschnitt 5.
#
# Nutzt AUSDRUECKLICH NICHT die Seed-Ausschluss-Logik aus cloc-neuzaehlung.sh.
# Korrektur (01.09.2026, nach erstem echten dry-run): cloc-neuzaehlung.sh entfernt
# Seed-Dateien rein pfadbasiert aus einer Hardlink-Arbeitskopie, um NUR neu
# geschriebene Codezeilen zu zaehlen -- das ist fuer ein Zeilenzaehl-Werkzeug
# unproblematisch, das nichts kompiliert. Fuer den Backend-Scan hier ist es
# das nicht: SonarScanner fuer .NET braucht einen echten "dotnet build", und
# die Projektdatei (<Projekt>.csproj) selbst steht in der Seed-Dateiliste,
# obwohl sie zum Bauen unverzichtbar ist -- ihre Entfernung liess den ersten
# echten dry-run mit "MSB1009: Project file does not exist" scheitern.
# A3/A5/A6/A7/A8 messen ausserdem den GESAMTEN gelieferten Code (siehe
# Metrikkatalog.md/Bachelorarbeit_Gesamtstand.md Abschnitt 8.1), die
# modulscharfe Abgrenzung laeuft ueber sonar.inclusions/exclusions aus
# sonar/scope/*.properties (nur fuer die OSS-Baselines) -- ein zusaetzlicher
# Seed-Diff-Filter war dafuer nie erforderlich. Backend/Frontend werden daher
# direkt gegen runs/<P>/<A>/<NN>/code analysiert, ohne Zwischenkopie.
#
# Aufruf:
#   ./sonar-scan.sh check                     # Werkzeuge + Servererreichbarkeit pruefen
#   ./sonar-scan.sh dry-run A bmad 01 backend  # ein Layer eines Laufs, nichts hochladen (-Dsonar.qualitygate.wait=false, lokales Dry-Run ueber sonar-scanner nicht nativ moeglich -> siehe Hinweis unten)
#   ./sonar-scan.sh lauf A bmad 01             # ein Lauf, beide Layer
#   ./sonar-scan.sh runs                       # alle zwoelf Laeufe, beide Layer (24 Analysen)
#   ./sonar-scan.sh baseline A modulscharf     # eine Baseline-Variante, beide Layer
#   ./sonar-scan.sh baselines                  # alle acht Baseline-Analysen
#   ./sonar-scan.sh alle                       # runs + baselines (32 Analysen)
#
# Hinweis "dry run": SonarQube-Scanner kennen keinen echten Dry-Run, der die
# Ergebnisse lokal anzeigt statt sie zum Server zu senden (anders als cloc).
# "dry-run" hier fuehrt die Analyse reell aus, aber gegen einen eigenen
# Projekt-Key mit Suffix "-probe", damit ein Fehlversuch keinen der 32
# eigentlichen Projekt-Keys verschmutzt. Danach in der SonarQube-UI pruefen
# und den Probe-Key bei Bedarf loeschen.
set -euo pipefail

REPO_ROOT="${REPO_ROOT:-$HOME/dev/Bachelor/bmad-vergleichsstudie}"
OSS_A_DIR="${OSS_A_DIR:-$HOME/dev/Bachelor/baseline/angularbooking}"
OSS_B_DIR="${OSS_B_DIR:-$HOME/dev/Bachelor/baseline/Godsend}"
SONAR_HOST_URL="${SONAR_HOST_URL:-http://localhost:9000}"
: "${SONAR_TOKEN:?Bitte SONAR_TOKEN exportieren (User-Token aus SonarQube, siehe docs/SonarQube_Setup.md Abschnitt 3)}"

PROJECT_PREFIX="bmad-vgl"
SCOPE_DIR="$REPO_ROOT/sonar/scope"
ERGEBNIS_DIR="$REPO_ROOT/sonar-ergebnisse"
mkdir -p "$ERGEBNIS_DIR"

# gleiche Verzeichnis-/Datei-Ausschluesse wie cloc-neuzaehlung.sh, gelten
# zusaetzlich zu den scope/*.properties-Dateien (die Feature-Zuordnung regelt
# NUR die modulscharfe Einschraenkung, nicht die generelle Ausschlussliste)
ALLGEMEINE_EXCLUDES="**/node_modules/**,**/bin/**,**/obj/**,**/dist/**,**/wwwroot/**,**/.claude/**,**/.agents/**,**/_bmad/**,**/_bmad-output/**,**/Migrations/**,**/*.spec.ts,**/e2e/**"

trap 'rm -rf "$REPO_ROOT"/.sonar-tmp-* 2>/dev/null || true' EXIT

# ---------------------------------------------------------------------------
# Werkzeugpruefung
# ---------------------------------------------------------------------------

pruefe_werkzeuge() {
  local fehlt=0
  command -v dotnet >/dev/null 2>&1 || { echo "FEHLT: dotnet SDK"; fehlt=1; }
  command -v sonar-scanner >/dev/null 2>&1 || { echo "FEHLT: sonar-scanner (SonarScanner CLI, fuer das Frontend)"; fehlt=1; }
  command -v npm >/dev/null 2>&1 || { echo "FEHLT: npm"; fehlt=1; }
  if command -v dotnet >/dev/null 2>&1; then
    dotnet tool list -g 2>/dev/null | grep -qi dotnet-sonarscanner || { echo "FEHLT: dotnet tool 'dotnet-sonarscanner' (dotnet tool install --global dotnet-sonarscanner --version 11.2.1)"; fehlt=1; }
  fi
  if command -v curl >/dev/null 2>&1; then
    if ! curl -fsS "$SONAR_HOST_URL/api/system/status" >/dev/null 2>&1; then
      echo "FEHLT/NICHT ERREICHBAR: SonarQube unter $SONAR_HOST_URL (docker compose -f docker-compose.sonarqube.yml --env-file .env.sonarqube up -d)"
      fehlt=1
    fi
  fi
  if [ "$fehlt" -ne 0 ]; then
    echo "Mindestens ein Werkzeug fehlt oder Server nicht erreichbar, siehe oben." >&2
    exit 1
  fi
  echo "Alle Werkzeuge vorhanden, Server unter $SONAR_HOST_URL erreichbar."
}

# ---------------------------------------------------------------------------
# Lauf-Basisverzeichnis (keine Seed-Zwischenkopie, siehe Korrekturhinweis oben)
# ---------------------------------------------------------------------------

lauf_verzeichnis() {
  local projekt="$1" ansatz="$2" nn="$3"
  local run_dir="$REPO_ROOT/runs/$projekt/$ansatz/$nn/code"
  [ -d "$run_dir" ] || { echo "FEHLT: $run_dir" >&2; exit 1; }
  LAUF_DIR="$run_dir"
}

# ---------------------------------------------------------------------------
# properties-Datei -> gemergte sonar.inclusions/exclusions
# ---------------------------------------------------------------------------
# Setzt SCOPE_INCLUSIONS (leer, wenn die Datei keine sonar.inclusions definiert)
# und SCOPE_EXCLUSIONS (immer gesetzt: ALLGEMEINE_EXCLUDES + scope-Datei-Excludes).
# Wichtig: eine scope-Datei ERGAENZT die allgemeine Ausschlussliste, statt sie zu
# ersetzen -- ein wiederholtes -D/-d:sonar.exclusions gilt bei beiden Scannern als
# "letzter Wert gewinnt", nicht als Vereinigung. Ohne dieses Mergen wuerde z.B.
# node_modules/dist/wwwroot in "vollstaendig"-Baseline-Scans wieder mitgezaehlt,
# weil die scope/*.properties-Dateien diese Pfade nicht erneut ausschliessen.
# Korrektur (02.09.2026): vorher wurden scope-Properties unconditional als
# "-Dkey=value" gebaut, was fuer dotnet-sonarscanner (erwartet "/d:key=value")
# nicht funktioniert und beim ersten echten baselines-Lauf mit
# "Unrecognized command line argument" fehlschlug.

lade_scope_args() {
  local datei="$1"
  SCOPE_INCLUSIONS=""
  SCOPE_EXCLUSIONS="$ALLGEMEINE_EXCLUDES"
  [ -f "$datei" ] || return 0
  while IFS='=' read -r key val; do
    [[ "$key" =~ ^#.*$ || -z "$key" ]] && continue
    case "$key" in
      sonar.inclusions) SCOPE_INCLUSIONS="$val" ;;
      sonar.exclusions) SCOPE_EXCLUSIONS="${ALLGEMEINE_EXCLUDES},${val}" ;;
    esac
  done < "$datei"
}

# ---------------------------------------------------------------------------
# Backend-Analyse (C#, via dotnet-sonarscanner begin/build/end)
# ---------------------------------------------------------------------------

scan_backend() {
  local project_key="$1" base_dir="$2" csproj="$3" scope_props="$4"
  echo "== Backend-Scan: $project_key (Basis: $base_dir) =="
  lade_scope_args "$scope_props"
  local incl_arg=()
  [ -n "$SCOPE_INCLUSIONS" ] && incl_arg=(/d:sonar.inclusions="$SCOPE_INCLUSIONS")
  ( cd "$base_dir" && \
    dotnet-sonarscanner begin \
      /k:"$project_key" \
      /d:sonar.host.url="$SONAR_HOST_URL" \
      /d:sonar.token="$SONAR_TOKEN" \
      /d:sonar.exclusions="$SCOPE_EXCLUSIONS" \
      "${incl_arg[@]}" && \
    dotnet build "$csproj" -c Release && \
    dotnet-sonarscanner end /d:sonar.token="$SONAR_TOKEN" )
}

# ---------------------------------------------------------------------------
# Frontend-Analyse (TypeScript, via sonar-scanner CLI)
# ---------------------------------------------------------------------------

scan_frontend() {
  local project_key="$1" base_dir="$2" scope_props="$3"
  echo "== Frontend-Scan: $project_key (Basis: $base_dir) =="
  lade_scope_args "$scope_props"
  local incl_arg=()
  [ -n "$SCOPE_INCLUSIONS" ] && incl_arg=(-Dsonar.inclusions="$SCOPE_INCLUSIONS")
  if [ -f "$base_dir/package.json" ] && [ ! -d "$base_dir/node_modules" ]; then
    ( cd "$base_dir" && npm ci --no-audit --no-fund ) || \
    ( cd "$base_dir" && npm install --no-audit --no-fund ) || true
  fi
  ( cd "$base_dir" && \
    sonar-scanner \
      -Dsonar.projectKey="$project_key" \
      -Dsonar.host.url="$SONAR_HOST_URL" \
      -Dsonar.token="$SONAR_TOKEN" \
      -Dsonar.sources=. \
      -Dsonar.exclusions="$SCOPE_EXCLUSIONS" \
      "${incl_arg[@]}" )
}

# ---------------------------------------------------------------------------
# Ein KI-Lauf (beide Layer)
# ---------------------------------------------------------------------------

scan_lauf() {
  local projekt="$1" ansatz="$2" nn="$3" suffix="${4:-}"
  local projekt_klein; projekt_klein=$(echo "$projekt" | tr 'A-Z' 'a-z')
  lauf_verzeichnis "$projekt" "$ansatz" "$nn"
  local key_base="${PROJECT_PREFIX}-${projekt_klein}-${ansatz}-${nn}${suffix}"
  scan_backend  "${key_base}-backend"  "$LAUF_DIR/seed-${projekt_klein}-backend.Api" \
                "$LAUF_DIR/seed-${projekt_klein}-backend.Api/seed-${projekt_klein}-backend.Api.csproj" /dev/null
  scan_frontend "${key_base}-frontend" "$LAUF_DIR/seed-${projekt_klein}-frontend" /dev/null
}

# ---------------------------------------------------------------------------
# Eine Baseline-Variante (beide Layer)
# ---------------------------------------------------------------------------

scan_baseline() {
  local projekt="$1" variante="$2"   # variante = modulscharf | vollstaendig
  local oss_dir backend_base backend_csproj frontend_base
  if [ "$projekt" = "A" ]; then
    oss_dir="$OSS_A_DIR"
    backend_base="$oss_dir/AngularBooking"
    backend_csproj="$backend_base/AngularBooking.csproj"
    frontend_base="$oss_dir/AngularBooking/ClientApp"
  else
    oss_dir="$OSS_B_DIR"
    backend_base="$oss_dir"
    backend_csproj="$oss_dir/Godsend/Godsend.csproj"
    frontend_base="$oss_dir/Client"
  fi
  local projekt_klein; projekt_klein=$(echo "$projekt" | tr 'A-Z' 'a-z')
  local key_base="${PROJECT_PREFIX}-baseline-${projekt_klein}-${variante}"
  scan_backend  "${key_base}-backend"  "$backend_base"  "$backend_csproj" \
                "$SCOPE_DIR/${projekt}-backend-${variante}.properties"
  scan_frontend "${key_base}-frontend" "$frontend_base" \
                "$SCOPE_DIR/${projekt}-frontend-${variante}.properties"
}

# ---------------------------------------------------------------------------
# Hauptprogramm
# ---------------------------------------------------------------------------

MODUS="${1:-}"
case "$MODUS" in
  check)
    pruefe_werkzeuge
    ;;
  dry-run)
    pruefe_werkzeuge
    projekt="${2:?z.B. A}"; ansatz="${3:?z.B. bmad}"; nn="${4:?z.B. 01}"; layer="${5:?backend|frontend}"
    projekt_klein=$(echo "$projekt" | tr 'A-Z' 'a-z')
    lauf_verzeichnis "$projekt" "$ansatz" "$nn"
    if [ "$layer" = "backend" ]; then
      scan_backend "${PROJECT_PREFIX}-probe-backend" \
        "$LAUF_DIR/seed-${projekt_klein}-backend.Api" \
        "$LAUF_DIR/seed-${projekt_klein}-backend.Api/seed-${projekt_klein}-backend.Api.csproj" /dev/null
    else
      scan_frontend "${PROJECT_PREFIX}-probe-frontend" "$LAUF_DIR/seed-${projekt_klein}-frontend" /dev/null
    fi
    echo "Probe abgeschlossen, Projekt '${PROJECT_PREFIX}-probe-*' in SonarQube pruefen."
    ;;
  lauf)
    pruefe_werkzeuge
    scan_lauf "${2:?Projekt}" "${3:?Ansatz}" "${4:?NN}"
    ;;
  runs)
    pruefe_werkzeuge
    for projekt in A B; do
      for ansatz in bmad solo; do
        for nn in 01 02 03; do
          echo "### Lauf $projekt-$ansatz-$nn ###"
          scan_lauf "$projekt" "$ansatz" "$nn"
        done
      done
    done
    ;;
  baseline)
    pruefe_werkzeuge
    scan_baseline "${2:?Projekt A|B}" "${3:?modulscharf|vollstaendig}"
    ;;
  baselines)
    pruefe_werkzeuge
    for projekt in A B; do
      for variante in modulscharf vollstaendig; do
        echo "### Baseline $projekt-$variante ###"
        scan_baseline "$projekt" "$variante"
      done
    done
    ;;
  alle)
    "$0" runs
    "$0" baselines
    ;;
  *)
    sed -n '2,25p' "$0" >&2
    exit 1
    ;;
esac
