#!/usr/bin/env python3
"""Verkettung mehrerer Messlaeufe fuer den unbeaufsichtigten Betrieb.

Der Harness kennt keine Warteschlange, und sein Rueckgabewert taugt nicht als
Ersatz: `cmd_run` kehrt nach jeder Terminierung regulaer zurueck, unabhaengig
davon, ob der Lauf fertig wurde oder an einem Blocker haengt. Dieses Skript
entscheidet deshalb ausschliesslich anhand von `state.json`.

Grundsaetze:
  * Strikt sequenziell. Parallele Laeufe teilen sich das Rate-Limit und
    verfaelschen C1c in beiden.
  * Rate-Limit-Pausen wartet der Harness selbst ab. Hier wird nichts getan.
  * Bei `blocker` und `spend_limit` haelt die Kette an, weil sonst eine
    unaufgeloeste Zelle liegen bleibt und zugleich die naechste verbraucht wird.
  * Ab dem ersten `run` ist eine Zelle unwiederbringlich verbraucht. Deshalb
    `--dry-run` und `--max-runs`.

Aufruf:
    export MSSQL_SA_PASSWORD='...'
    python3 laufkette.py --config config.messlauf.json --max-runs 2
    python3 laufkette.py --config config.messlauf.json --dry-run
"""

from __future__ import annotations

import argparse
import datetime as dt
import json
import os
import shutil
import subprocess
import sys
import time
from pathlib import Path

# Reihenfolge nach claude/Zeitregime_Laufsteuerung.md Abschnitt 4:
# abwechselnd, Projekt A vollstaendig vor Projekt B.
STANDARD_FOLGE = [
    "A-BMAD-1", "A-SOLO-1", "A-BMAD-2", "A-SOLO-2", "A-BMAD-3", "A-SOLO-3",
    "B-BMAD-1", "B-SOLO-1", "B-BMAD-2", "B-SOLO-2", "B-BMAD-3", "B-SOLO-3",
]

# Terminierungsursachen, nach denen die Kette weiterlaufen darf. Alle vier sind
# vorab gesetzte Deckel oder der regulaere Abschluss, also gueltige Befunde.
WEITER = {"regular", "T1", "T2", "token_budget", "cost_budget", "max_steps"}
# Alles Uebrige haelt an, insbesondere `blocker` und `spend_limit`.

MINDESTPLATZ_GB = 5.0


def jetzt() -> str:
    return dt.datetime.now().strftime("%Y-%m-%d %H:%M:%S")


def lauf_verzeichnis(runs_root: Path, run_id: str) -> Path:
    projekt, bedingung, wdh = run_id.split("-")
    return runs_root / projekt / bedingung.lower() / f"{int(wdh):02d}"


def zustand(rd: Path) -> dict | None:
    p = rd / "state.json"
    if not p.exists():
        return None
    try:
        return json.loads(p.read_text(encoding="utf-8"))
    except Exception:
        return None


class Protokoll:
    def __init__(self, pfad: Path):
        self.pfad = pfad
        pfad.parent.mkdir(parents=True, exist_ok=True)

    def __call__(self, text: str) -> None:
        zeile = f"[{jetzt()}] {text}"
        print(zeile, flush=True)
        with self.pfad.open("a", encoding="utf-8") as fh:
            fh.write(zeile + "\n")


def harness(cfg: Path, befehl: str, run_id: str | None, log) -> int:
    argumente = [sys.executable, "harness.py", befehl, "--config", str(cfg)]
    if run_id:
        argumente += ["--run", run_id]
    log(f"    $ {' '.join(argumente[1:])}")
    return subprocess.run(argumente).returncode


def main() -> int:
    ap = argparse.ArgumentParser(description=__doc__,
                                 formatter_class=argparse.RawDescriptionHelpFormatter)
    ap.add_argument("--config", required=True, type=Path)
    ap.add_argument("--runs", nargs="*", default=None,
                    help="Lauf-IDs statt der Standardfolge")
    ap.add_argument("--max-runs", type=int, default=2,
                    help="Hoechstzahl in dieser Nacht zu startender Laeufe (Vorgabe 2)")
    ap.add_argument("--pause-min", type=float, default=0.0,
                    help="Ruhezeit zwischen zwei Laeufen in Minuten")
    ap.add_argument("--dry-run", action="store_true",
                    help="nur anzeigen, was geschehen wuerde")
    args = ap.parse_args()

    if not args.config.exists():
        print(f"Konfiguration {args.config} nicht gefunden.")
        return 2
    cfg_raw = json.loads(args.config.read_text(encoding="utf-8"))
    runs_root = Path(os.path.expanduser(cfg_raw["runs_root"])).resolve()

    stempel = dt.datetime.now().strftime("%Y%m%d-%H%M")
    log = Protokoll(runs_root / f"kette-{stempel}.log")

    sperre = runs_root / "KETTE_LAEUFT"
    if sperre.exists() and not args.dry_run:
        log(f"Abbruch: {sperre} existiert. Laeuft bereits eine Kette? "
            f"Datei entfernen, wenn nicht.")
        return 2

    folge = args.runs if args.runs else STANDARD_FOLGE

    # --- Vorpruefungen -----------------------------------------------------
    if not os.environ.get("MSSQL_SA_PASSWORD"):
        log("Abbruch: MSSQL_SA_PASSWORD ist nicht gesetzt.")
        return 2

    plan: list[tuple[str, str]] = []
    for run_id in folge:
        rd = lauf_verzeichnis(runs_root, run_id)
        st = zustand(rd)
        if st is None:
            plan.append((run_id, "init+run" if not rd.exists() or not any(rd.iterdir())
                         else "HINDERNIS: Verzeichnis nicht leer, kein state.json"))
        elif st.get("finished"):
            plan.append((run_id, f"uebersprungen (beendet: {st.get('termination_reason')})"))
        else:
            plan.append((run_id, f"fortsetzen ab Schritt {st.get('step_index')}"))

    log(f"Kette geplant, hoechstens {args.max_runs} Laeufe in diesem Durchgang.")
    for run_id, was in plan:
        log(f"  {run_id:12s} {was}")

    if args.dry_run:
        log("Testlauf, es wurde nichts angefasst.")
        return 0

    if harness(args.config, "dbcheck", None, log) != 0:
        log("Abbruch: dbcheck fehlgeschlagen. Laeuft der Container?")
        return 2

    sperre.write_text(jetzt(), encoding="utf-8")
    gestartet = 0
    try:
        for run_id, was in plan:
            if gestartet >= args.max_runs:
                log(f"Hoechstzahl von {args.max_runs} Laeufen erreicht. Ende.")
                break
            if was.startswith("uebersprungen"):
                continue
            if was.startswith("HINDERNIS"):
                log(f"{run_id}: {was}. Kette haelt an.")
                break

            frei = shutil.disk_usage(runs_root).free / 1024**3
            if frei < MINDESTPLATZ_GB:
                log(f"Abbruch: nur noch {frei:.1f} GB frei, mindestens "
                    f"{MINDESTPLATZ_GB} GB noetig.")
                break

            if gestartet and args.pause_min:
                log(f"Ruhezeit {args.pause_min:.0f} min vor {run_id}.")
                time.sleep(args.pause_min * 60)

            rd = lauf_verzeichnis(runs_root, run_id)
            log(f"=== {run_id} ===  ({frei:.1f} GB frei)")

            if zustand(rd) is None:
                if harness(args.config, "init", run_id, log) != 0:
                    log(f"{run_id}: init fehlgeschlagen. Kette haelt an.")
                    break

            harness(args.config, "run", run_id, log)
            gestartet += 1

            st = zustand(rd) or {}
            grund = st.get("termination_reason")
            fertig = st.get("finished")
            log(f"{run_id}: finished={fertig} · Ursache={grund} · "
                f"Schritte={st.get('step_index')} · "
                f"T1={(st.get('active_seconds') or 0)/3600:.2f} h · "
                f"Warten={(st.get('wait_seconds') or 0)/3600:.2f} h · "
                f"Tokens={st.get('tokens', {}).get('input', 0) + st.get('tokens', {}).get('output', 0):,} "
                f"· {st.get('cost_usd', 0):.2f} USD (Schaetzung)")

            if not fertig:
                log(f"{run_id}: nicht beendet, vermutlich von Hand gestoppt. "
                    f"Kette haelt an.")
                break
            if grund not in WEITER:
                log(f"{run_id}: Ursache '{grund}' erfordert einen Blick von dir. "
                    f"Kette haelt an.")
                break
    finally:
        if sperre.exists():
            sperre.unlink()

    log(f"Durchgang beendet, {gestartet} Lauf/Laeufe gestartet. "
        f"Protokoll: {log.pfad}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
