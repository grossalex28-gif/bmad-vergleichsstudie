#!/usr/bin/env python3
"""
Selbsttest des Harness.

Prueft die Steuerungslogik gegen eine Attrappe der Claude-Code-CLI:
kein Netzwerkzugriff, kein Tokenverbrauch, Laufzeit unter einer Minute.

    python3 selftest.py

Geprueft werden zehn Szenarien:
  1  BMAD-Bedingung laeuft regulaer durch, Artefakte und Git-Tags entstehen
  2  H2  abweichender Eingabe-Hash bricht den Lauf ab
  3  Stopp waehrend eines Schritts, danach Fortsetzung an derselben Stelle
  4  Rate-Limit wird erkannt, Wartezeit getrennt gezaehlt, Lauf faehrt fort
  5  H7  Blocker terminiert den Lauf mit dem richtigen Grund
  6  T1  Aktivzeitdeckel terminiert den Lauf
  7  T3  haengender Schritt wird abgebrochen
  8  Solo-Bedingung laeuft ohne BMAD-Definitionen und endet bei "FERTIG: JA"
  9  Aktive Claude-Code-Hooks verhindern den Lauf
 10  Tokenbudget ohne Cache-Reads, Wiedereroeffnung als protokollierter Eingriff
 11  Netzwerkpruefung: externer Abruf wird erkannt und protokolliert
"""

from __future__ import annotations

import json
import os
import shutil
import subprocess
import sys
import tempfile
import threading
import time
from pathlib import Path

HERE = Path(__file__).resolve().parent
HARNESS = HERE / "harness.py"
FAKE = HERE / "tools" / "fake_claude.py"

PASSED: list[str] = []
FAILED: list[tuple[str, str]] = []


# ---------------------------------------------------------------------------


class Workspace:
    """Wegwerf-Arbeitsumgebung mit Seed, Eingabedatei und BMAD-Attrappe."""

    def __init__(self, name: str):
        self.root = Path(tempfile.mkdtemp(prefix=f"harness-selftest-{name}-"))
        (self.root / "seed").mkdir()
        (self.root / "seed" / "README.md").write_text("Seed\n", encoding="utf-8")
        (self.root / "seed" / "tests").mkdir()
        (self.root / "seed" / "tests" / ".gitkeep").write_text("", encoding="utf-8")

        (self.root / "inputs").mkdir()
        self.input_file = self.root / "inputs" / "probe.md"
        self.input_file.write_text(
            "# Projektauftrag\n\nEin sehr kleines Testprojekt.\n", encoding="utf-8")

        # BMAD-Attrappe: nur die Ordner, die der Harness kopiert.
        bs = self.root / "bmad"
        (bs / "_bmad" / "_config").mkdir(parents=True)
        (bs / "_bmad" / "_config" / "manifest.yaml").write_text(
            "installation:\n  version: 6.10.0\n", encoding="utf-8")
        (bs / ".claude" / "skills").mkdir(parents=True)
        (bs / ".claude" / "skills" / ".gitkeep").write_text("", encoding="utf-8")
        self.bmad_source = bs

        # Wrapper, damit claude_bin ein einzelnes ausfuehrbares Programm ist.
        self.fake_bin = self.root / "fake-claude"
        self.fake_bin.write_text(
            f'#!/bin/sh\nexec "{sys.executable}" "{FAKE}" "$@"\n', encoding="utf-8")
        self.fake_bin.chmod(0o755)

    def config(self, **limits) -> Path:
        cfg = {
            "runs_root": str(self.root / "runs"),
            "seed_repo": str(self.root / "seed"),
            "bmad_source": str(self.bmad_source),
            "input_files": {"P": str(self.input_file)},
            "model": "test-model",
            "claude_bin": str(self.fake_bin),
            "permission_mode": "bypassPermissions",
            "allowed_tools": [],
            "disallowed_tools": [],
            "require_no_hooks": limits.get("require_no_hooks", False),
            "limits": {
                "t1_active_hours": limits.get("t1", 4),
                "t2_calendar_hours": limits.get("t2", 24),
                "t3_step_minutes": limits.get("t3", 5),
                "token_budget": limits.get("token_budget"),
                "cost_budget_usd": limits.get("cost_budget_usd"),
                "max_steps": limits.get("max_steps", 40),
            },
            "progress": False,
            "rate_limit": {
                "wait_minutes": limits.get("wait_minutes", 0.02),
                "max_retries": limits.get("max_retries", 3),
            },
            "sequence": {
                "routing_mode": limits.get("routing_mode", "help"),
                "solo_max_iterations": limits.get("solo_max_iterations", 10),
                "fallback_chain": [{"skill": "bmad-prd"}, {"skill": "bmad-architecture"}],
            },
        }
        p = self.root / "config.json"
        p.write_text(json.dumps(cfg, indent=2), encoding="utf-8")
        return p

    def state(self, run_id: str) -> dict:
        for p in (self.root / "runs").rglob("state.json"):
            data = json.loads(p.read_text(encoding="utf-8"))
            if data["run_id"] == run_id:
                return data
        raise AssertionError(f"kein state.json fuer {run_id}")

    def run_dir(self, run_id: str) -> Path:
        for p in (self.root / "runs").rglob("state.json"):
            if json.loads(p.read_text(encoding="utf-8"))["run_id"] == run_id:
                return p.parent
        raise AssertionError(f"kein Laufverzeichnis fuer {run_id}")

    def cleanup(self) -> None:
        shutil.rmtree(self.root, ignore_errors=True)


# Hermetische, hook-freie Claude-Konfiguration fuer alle Szenarien ausser 9.
CLEAN_CFG = Path(tempfile.mkdtemp(prefix="harness-selftest-cleancfg-"))
(CLEAN_CFG / "settings.json").write_text('{"theme":"dark"}', encoding="utf-8")


def harness(cfg: Path, cmd: str, run_id: str, env: dict | None = None,
            timeout: int = 120) -> subprocess.CompletedProcess:
    e = dict(os.environ)
    e["CLAUDE_CONFIG_DIR"] = str(CLEAN_CFG)
    e.update(env or {})
    return subprocess.run(
        [sys.executable, str(HARNESS), cmd, "--config", str(cfg), "--run", run_id],
        capture_output=True, text=True, env=e, timeout=timeout)


def check(name: str, cond: bool, detail: str = "") -> None:
    if cond:
        PASSED.append(name)
        print(f"  \033[32mOK\033[0m   {name}")
    else:
        FAILED.append((name, detail))
        print(f"  \033[31mFEHL\033[0m {name}  {detail}")


# ---------------------------------------------------------------------------
# Szenarien
# ---------------------------------------------------------------------------


def t1_happy_path() -> None:
    print("\n[1] BMAD-Bedingung, regulaerer Durchlauf")
    ws = Workspace("happy")
    try:
        cfg = ws.config()
        r = harness(cfg, "init", "P-BMAD-1")
        check("init legt Laufverzeichnis an", r.returncode == 0, r.stderr[-300:])
        rd = ws.run_dir("P-BMAD-1")
        check("Seed kopiert", (rd / "code" / "README.md").exists())
        check("BMAD-Definitionen in der BMAD-Bedingung vorhanden",
              (rd / "code" / "_bmad").exists() and (rd / "code" / ".claude").exists())
        conf = json.loads((rd / "config.json").read_text(encoding="utf-8"))
        check("config.json enthaelt Input-Hash und Seed-Commit",
              len(conf.get("input_sha256", "")) == 64 and len(conf.get("seed_commit", "")) == 40)

        r = harness(cfg, "run", "P-BMAD-1")
        st = ws.state("P-BMAD-1")
        check("Lauf terminiert regulaer", st["termination_reason"] == "regular",
              str(st.get("termination_reason")) + r.stdout[-300:])
        check("mehrere Schritte ausgefuehrt", st["step_index"] >= 5, str(st["step_index"]))
        check("Tokens gezaehlt", st["tokens"]["output"] > 0, str(st["tokens"]))
        check("Kosten aus der CLI uebernommen", st["cost_usd"] > 0, str(st["cost_usd"]))
        check("Modellbezeichner je Schritt erfasst",
              all(s.get("model_seen") for s in st["completed_steps"]))
        check("Hook-Ereignis vor init stoert den Parser nicht",
              all(s.get("session_id") for s in st["completed_steps"]))
        check("Konfiguration der Hooks in config.json dokumentiert",
              "claude_settings" in conf)
        check("Annahmen aus JSON-Status uebernommen",
              any(s.get("assumptions") for s in st["completed_steps"]))
        check("offene Fragen erfasst",
              any(s.get("open_questions") for s in st["completed_steps"]))
        check("protokoll.csv geschrieben", (rd / "logs" / "protokoll.csv").exists())
        check("JSONL je Schritt vorhanden",
              len(list((rd / "logs").glob("step-*.jsonl"))) >= st["step_index"])
        tags = subprocess.run(["git", "tag"], cwd=rd / "code",
                              capture_output=True, text=True).stdout.split()
        check("Git-Tag je Schritt", len(tags) == st["step_index"],
              f"{len(tags)} Tags / {st['step_index']} Schritte")
        n_router = len(list((rd / "logs").glob("*router*")))
        check("Story-Zyklus laeuft ohne Routing-Aufruf je Schritt",
              n_router < st["step_index"], f"{n_router} Router / {st['step_index']} Schritte")
        check("Herkunft jedes Schritts protokolliert",
              all(s.get("source") in ("router", "cycle") for s in st["completed_steps"]))
        check("Paketmanager-Aufrufe als unbedenklich eingestuft",
              st["network"]["package"] > 0 and st["network"]["fetch"] == 0,
              str(st["network"]))
        check("Werkzeugzensus erfasst Haupt- und Subagent",
              st["tools"]["main"].get("Bash", 0) > 0 and st["tools"]["sub"].get("Read", 0) > 0,
              str(st["tools"]))
        check("Router hat die Kette abgearbeitet",
              [s["skill"] for s in st["completed_steps"]][1:3]
              == ["bmad-prd", "bmad-architecture"],
              str([s["skill"] for s in st["completed_steps"]]))
    finally:
        ws.cleanup()


def t2_hash_guard() -> None:
    print("\n[2] H2 – Hash-Abgleich der Eingabedatei")
    ws = Workspace("hash")
    try:
        cfg = ws.config()
        harness(cfg, "init", "P-BMAD-1")
        ws.input_file.write_text("# Veraendert\n", encoding="utf-8")
        r = harness(cfg, "run", "P-BMAD-1")
        check("Lauf bricht bei abweichendem Hash ab", r.returncode != 0, r.stdout[-200:])
        check("Abbruchmeldung nennt H2", "H2" in (r.stderr + r.stdout))
    finally:
        ws.cleanup()


def t3_stop_and_resume() -> None:
    print("\n[3] Stoppen waehrend eines Schritts und Fortsetzen")
    ws = Workspace("stop")
    try:
        cfg = ws.config()
        harness(cfg, "init", "P-BMAD-1")
        rd = ws.run_dir("P-BMAD-1")

        def set_stop():
            (rd / "STOP").write_text("test", encoding="utf-8")

        t = threading.Timer(3.5, set_stop)
        t.start()
        t0 = time.time()
        r = harness(cfg, "run", "P-BMAD-1", env={"FAKE_STEP_SECONDS": "2"})
        t.cancel()
        st = ws.state("P-BMAD-1")
        steps_before = st["step_index"]
        check("Lauf pausiert statt zu terminieren", not st["finished"],
              str(st.get("termination_reason")))
        check("mindestens ein Schritt wurde abgeschlossen", steps_before >= 1,
              str(steps_before))
        check("laufender Schritt wurde zu Ende gefuehrt",
              time.time() - t0 >= 4.0, f"{time.time()-t0:.1f}s")
        check("Stopp-Meldung ausgegeben", "Stopp angefordert" in r.stdout, r.stdout[-200:])

        r = harness(cfg, "run", "P-BMAD-1")
        st2 = ws.state("P-BMAD-1")
        check("Fortsetzung terminiert regulaer", st2["termination_reason"] == "regular",
              str(st2.get("termination_reason")))
        check("Fortsetzung setzt die Schrittzaehlung fort",
              st2["step_index"] > steps_before, f"{steps_before} -> {st2['step_index']}")
        idx = [s["index"] for s in st2["completed_steps"]]
        check("Schrittindizes lueckenlos", idx == list(range(len(idx))), str(idx))
        check("STOP-Markierung wurde geloescht", not (rd / "STOP").exists())
    finally:
        ws.cleanup()


def t4_rate_limit() -> None:
    print("\n[4] Rate-Limit – Wartezeit getrennt von T1")
    ws = Workspace("ratelimit")
    try:
        cfg = ws.config(wait_minutes=0.03)
        harness(cfg, "init", "P-BMAD-1")
        r = harness(cfg, "run", "P-BMAD-1", env={"FAKE_RATE_LIMIT_AT": "2"})
        st = ws.state("P-BMAD-1")
        check("Rate-Limit erkannt", "Rate-Limit erkannt" in r.stdout, r.stdout[-300:])
        check("Wartezeit getrennt gezaehlt", st["wait_seconds"] >= 1.0,
              f"{st['wait_seconds']:.2f}s")
        check("Wartezeit nicht in T1 enthalten",
              st["active_seconds"] < st["wait_seconds"] + 5.0,
              f"aktiv {st['active_seconds']:.2f}s / warten {st['wait_seconds']:.2f}s")
        check("Lauf setzt nach der Pause fort und endet regulaer",
              st["termination_reason"] == "regular", str(st.get("termination_reason")))
        pk = (ws.run_dir("P-BMAD-1") / "logs" / "protokoll.csv").read_text(encoding="utf-8")
        check("Pause in protokoll.csv dokumentiert",
              "rate_limit_wait_start" in pk and "rate_limit_wait_end" in pk)
    finally:
        ws.cleanup()


def t5_blocker() -> None:
    print("\n[5] H7 – Blocker")
    ws = Workspace("blocker")
    try:
        cfg = ws.config()
        harness(cfg, "init", "P-BMAD-1")
        r = harness(cfg, "run", "P-BMAD-1", env={"FAKE_BLOCK_AT": "1"})
        st = ws.state("P-BMAD-1")
        check("Terminierungsgrund ist blocker", st["termination_reason"] == "blocker",
              str(st.get("termination_reason")))
        check("Blockergrund protokolliert",
              any(s.get("reason") for s in st["completed_steps"]), r.stdout[-200:])
        check("Lauf gilt als beendet", st["finished"])
    finally:
        ws.cleanup()


def t6_t1_cap() -> None:
    print("\n[6] T1 – Aktivzeitdeckel")
    ws = Workspace("t1")
    try:
        cfg = ws.config(t1=0.0017)          # ca. 6 Sekunden
        harness(cfg, "init", "P-BMAD-1")
        harness(cfg, "run", "P-BMAD-1", env={"FAKE_STEP_SECONDS": "3"})
        st = ws.state("P-BMAD-1")
        check("Terminierungsgrund ist T1", st["termination_reason"] == "T1",
              str(st.get("termination_reason")))
        check("Aktivzeit hat den Deckel erreicht", st["active_seconds"] >= 6.0,
              f"{st['active_seconds']:.2f}s")
        check("Abbruch erfolgte an einer Schrittgrenze",
              st["step_index"] == len(st["completed_steps"]))
    finally:
        ws.cleanup()


def t7_step_timeout() -> None:
    print("\n[7] T3 – haengender Schritt")
    ws = Workspace("t3")
    try:
        cfg = ws.config(t3=0.05)            # 3 Sekunden
        harness(cfg, "init", "P-BMAD-1")
        t0 = time.time()
        harness(cfg, "run", "P-BMAD-1",
                env={"FAKE_SLEEP_AT": "1", "FAKE_SLEEP_SECONDS": "60"}, timeout=60)
        dur = time.time() - t0
        st = ws.state("P-BMAD-1")
        check("haengender Schritt wurde abgebrochen", dur < 30, f"{dur:.1f}s")
        check("Terminierungsgrund ist blocker", st["termination_reason"] == "blocker",
              str(st.get("termination_reason")))
    finally:
        ws.cleanup()


def t8_solo_arm() -> None:
    print("\n[8] Solo-Bedingung")
    ws = Workspace("solo")
    try:
        cfg = ws.config()
        harness(cfg, "init", "P-SOLO-1")
        rd = ws.run_dir("P-SOLO-1")
        check("keine BMAD-Definitionen in der Solo-Bedingung",
              not (rd / "code" / "_bmad").exists() and not (rd / "code" / ".claude").exists())
        harness(cfg, "run", "P-SOLO-1", env={"FAKE_SOLO_DONE_AT": "3"})
        st = ws.state("P-SOLO-1")
        check("Lauf endet bei erklaerter Fertigstellung",
              st["termination_reason"] == "regular", str(st.get("termination_reason")))
        check("Fertigmeldung erkannt",
              any(s.get("declared_done") for s in st["completed_steps"]))
        check("kein Router-Aufruf in der Solo-Bedingung",
              not list((rd / "logs").glob("*router*")))
        check("Iterationen begrenzt", st["step_index"] <= 10, str(st["step_index"]))
    finally:
        ws.cleanup()


def t10_budgets_and_reopen() -> None:
    print("\n[10] Budgets ohne Cache-Reads, Wiedereroeffnung")
    ws = Workspace("budget")
    try:
        # FAKE_TOKENS=500 -> je Aufruf 1500 abrechenbar + 50 Cache-Read.
        cfg = ws.config(token_budget=4000)
        harness(cfg, "init", "P-BMAD-1")
        harness(cfg, "run", "P-BMAD-1")
        st = ws.state("P-BMAD-1")
        check("Tokenbudget terminiert den Lauf",
              st["termination_reason"] == "token_budget", str(st.get("termination_reason")))
        check("Budget greift auf abrechenbare Tokens, nicht auf die Rohsumme",
              st["tokens"]["cache_read"] > 0
              and st["tokens"]["input"] + st["tokens"]["output"]
              + st["tokens"]["cache_creation"] >= 4000,
              str(st["tokens"]))
        steps_at_stop = st["step_index"]

        r = harness(cfg, "run", "P-BMAD-1")
        check("beendeter Lauf startet nicht von selbst neu",
              "bereits beendet" in r.stdout, r.stdout[-200:])

        r = harness(cfg, "reopen", "P-BMAD-1")
        check("reopen oeffnet den Lauf wieder", r.returncode == 0 and "geoeffnet" in r.stdout,
              r.stdout[-200:])
        st = ws.state("P-BMAD-1")
        check("Zustand ist wieder offen", not st["finished"])
        pk = (ws.run_dir("P-BMAD-1") / "logs" / "protokoll.csv").read_text(encoding="utf-8")
        check("Wiedereroeffnung als Eingriff protokolliert",
              "reopened_by_operator" in pk and "token_budget" in pk)

        cfg = ws.config(token_budget=None)
        harness(cfg, "run", "P-BMAD-1")
        st = ws.state("P-BMAD-1")
        check("Fortsetzung nach reopen laeuft zu Ende",
              st["termination_reason"] == "regular", str(st.get("termination_reason")))
        check("Schrittzaehlung fortgesetzt", st["step_index"] > steps_at_stop,
              f"{steps_at_stop} -> {st['step_index']}")

        idx = [s["index"] for s in st["completed_steps"]]
        check("Schrittindizes weiterhin lueckenlos", idx == list(range(len(idx))), str(idx))
        check("Router-Aufrufe mit Kosten protokolliert",
              any(row.split(",")[3] == "router" and row.split(",")[10]
                  for row in pk.splitlines()[1:] if len(row.split(",")) > 10),
              "keine Kostenspalte beim Router")
    finally:
        ws.cleanup()


def t11_network_audit() -> None:
    print("\n[11] Netzwerkpruefung")
    ws = Workspace("netz")
    try:
        cfg = ws.config()
        harness(cfg, "init", "P-BMAD-1")
        r = harness(cfg, "run", "P-BMAD-1",
                    env={"FAKE_BASH_CMD": "curl -s https://github.com/foo/bar/raw/main/README.md"})
        st = ws.state("P-BMAD-1")
        check("externer Abruf erkannt", st["network"]["fetch"] > 0, str(st["network"]))
        check("Befund in protokoll.csv",
              "network_fetch" in (ws.run_dir("P-BMAD-1") / "logs" / "protokoll.csv")
              .read_text(encoding="utf-8"))
        check("Kommando im Klartext gesichert",
              any("github.com" in c for c in st["network"]["commands"]),
              str(st["network"]["commands"][:2]))
        check("Warnung ausgegeben", "[netz]" in r.stdout, r.stdout[-200:])
        check("Lauf wird nicht abgebrochen", st["termination_reason"] == "regular",
              str(st.get("termination_reason")))
    finally:
        ws.cleanup()


def t9_hook_guard() -> None:
    print("\n[9] Hook-Absicherung")
    ws = Workspace("hooks")
    dirty = Path(tempfile.mkdtemp(prefix="harness-selftest-dirtycfg-"))
    try:
        (dirty / "settings.json").write_text(json.dumps({
            "hooks": {
                "SessionStart": [{"hooks": [{"type": "command", "command": "echo hi"}]}],
                "Stop": [{"hooks": [{"type": "command", "command": "echo bye"}]}],
            }}), encoding="utf-8")

        cfg = ws.config(require_no_hooks=True)
        r = harness(cfg, "init", "P-BMAD-1", env={"CLAUDE_CONFIG_DIR": str(dirty)})
        out = r.stdout + r.stderr
        check("init bricht bei aktiven Hooks ab", r.returncode != 0, out[-200:])
        check("Meldung nennt die betroffenen Ereignisse",
              "SessionStart" in out and "Stop" in out, out[-300:])

        check("kein halb angelegtes Laufverzeichnis zurueckgeblieben",
              not (ws.root / "runs").exists())

        ws2 = Workspace("hooks2")
        try:
            cfg2 = ws2.config(require_no_hooks=False)
            r = harness(cfg2, "init", "P-BMAD-1", env={"CLAUDE_CONFIG_DIR": str(dirty)})
            check("mit require_no_hooks=false laeuft init durch, mit Warnung",
                  r.returncode == 0 and "WARNUNG" in r.stdout,
                  (r.stdout + r.stderr)[-200:])
            conf = json.loads(
                (ws2.run_dir("P-BMAD-1") / "config.json").read_text(encoding="utf-8"))
            check("Hooks werden in config.json dokumentiert",
                  conf["claude_settings"]["active_hook_events"] == ["SessionStart", "Stop"],
                  str(conf["claude_settings"]["active_hook_events"]))
        finally:
            ws2.cleanup()
    finally:
        shutil.rmtree(dirty, ignore_errors=True)
        ws.cleanup()


def t12_seed_freeze_and_database() -> None:
    print("\n[12] Seed-Freeze-Pruefung und Datenbank je Lauf")
    ws = Workspace("db")
    try:
        seed = ws.root / "seed"
        daten = seed / "Data"
        daten.mkdir()
        (daten / "anfangsdatenbestand.json").write_text('{"a":1}\n', encoding="utf-8")
        for args in (("init", "-q"), ("add", "-A")):
            subprocess.run(["git", *args], cwd=seed, check=True, capture_output=True)
        subprocess.run(["git", "-c", "user.email=t@t", "-c", "user.name=T",
                        "commit", "-q", "-m", "seed"], cwd=seed, check=True,
                       capture_output=True)
        commit = subprocess.run(["git", "rev-parse", "HEAD"], cwd=seed, check=True,
                                capture_output=True, text=True).stdout.strip()
        import hashlib
        h = hashlib.sha256((daten / "anfangsdatenbestand.json").read_bytes()).hexdigest()

        # Attrappe fuer sqlcmd: protokolliert die Aufrufe, statt eine echte
        # Instanz zu brauchen. Geprueft wird die Steuerung, nicht SQL Server.
        log = ws.root / "sqlcmd.log"
        stub = ws.root / "sqlcmd-stub"
        stub.write_text(f'#!/bin/sh\nprintf "%s\\n" "$*" >> "{log}"\nexit 0\n',
                        encoding="utf-8")
        stub.chmod(0o755)

        cfgp = ws.config()
        cfg = json.loads(cfgp.read_text(encoding="utf-8"))
        cfg.pop("seed_repo")
        cfg["seed_repos"] = {"P": str(seed)}
        cfg["freeze"] = {"P": {
            "seed_commit": commit,
            "anfangsdatenbestand_datei": "Data/anfangsdatenbestand.json",
            "anfangsdatenbestand_sha256": h,
        }}
        cfg["database"] = {
            "enabled": True, "mode": "local", "sqlcmd": str(stub),
            "server": "localhost,1433", "user": "sa",
            "password_env": "SELFTEST_DB_PW",
            "name_template": "{project}_{condition}_{repetition}",
            "env_var": "ConnectionStrings__Default",
        }
        cfgp.write_text(json.dumps(cfg, indent=2), encoding="utf-8")

        r = harness(cfgp, "init", "P-BMAD-1", env={"SELFTEST_DB_PW": "GeheimA1!"})
        check("init laeuft mit Seed-Freeze und Datenbank durch", r.returncode == 0,
              r.stderr[-300:])
        aufrufe = log.read_text(encoding="utf-8") if log.exists() else ""
        check("Datenbank je Lauf wird angelegt",
              "CREATE DATABASE [p_bmad_1]" in aufrufe, aufrufe[:200])
        check("vorhandene Datenbank wird vorher verworfen",
              "DROP DATABASE [p_bmad_1]" in aufrufe, aufrufe[:200])

        conf = json.loads((ws.run_dir("P-BMAD-1") / "config.json").read_text(
            encoding="utf-8"))
        check("Datenbankname in config.json dokumentiert",
              conf["database"]["name"] == "p_bmad_1", str(conf.get("database")))
        check("Kennwort steht nicht in config.json",
              "GeheimA1!" not in json.dumps(conf)
              and "***" in conf["database"]["connection_string"],
              conf["database"]["connection_string"])
        check("erwarteter Seed-Commit mitgeschrieben",
              conf["seed_commit_erwartet"] == commit, str(conf.get("seed_commit_erwartet")))

        # Fehlendes Kennwort ist ein Abbruch, keine stille Vorbelegung.
        r = harness(cfgp, "init", "P-BMAD-2")
        check("fehlendes Kennwort bricht init ab", r.returncode != 0)
        check("Meldung nennt die Umgebungsvariable",
              "SELFTEST_DB_PW" in (r.stdout + r.stderr), (r.stdout + r.stderr)[-200:])

        # Veraendertes Seed-Repository faellt beim init auf, nicht erst spaeter.
        cfg["freeze"]["P"]["seed_commit"] = "0" * 40
        cfgp.write_text(json.dumps(cfg, indent=2), encoding="utf-8")
        r = harness(cfgp, "init", "P-BMAD-3", env={"SELFTEST_DB_PW": "GeheimA1!"})
        check("abweichender Seed-Commit bricht init ab", r.returncode != 0)
        check("Meldung nennt den Seed",
              "Seed-Repository" in (r.stdout + r.stderr), (r.stdout + r.stderr)[-200:])

        # Ohne database-Abschnitt bleibt alles wie im Probelauf.
        cfg["freeze"]["P"]["seed_commit"] = commit
        cfg["database"]["enabled"] = False
        cfgp.write_text(json.dumps(cfg, indent=2), encoding="utf-8")
        r = harness(cfgp, "init", "P-SOLO-1")
        conf = json.loads((ws.run_dir("P-SOLO-1") / "config.json").read_text(
            encoding="utf-8"))
        check("abgeschaltete Datenbank laesst init unveraendert",
              r.returncode == 0 and "database" not in conf, r.stderr[-200:])
    finally:
        ws.cleanup()


# ---------------------------------------------------------------------------


def main() -> int:
    print("Selbsttest des Harness – Attrappe statt echter CLI, kein Tokenverbrauch.")
    if not HARNESS.exists() or not FAKE.exists():
        print("harness.py oder tools/fake_claude.py fehlt.")
        return 2
    if shutil.which("git") is None:
        print("git ist nicht installiert.")
        return 2

    for fn in (t1_happy_path, t2_hash_guard, t3_stop_and_resume, t4_rate_limit,
               t5_blocker, t6_t1_cap, t7_step_timeout, t8_solo_arm, t9_hook_guard, t10_budgets_and_reopen, t11_network_audit,
               t12_seed_freeze_and_database):
        try:
            fn()
        except Exception as exc:                          # noqa: BLE001
            FAILED.append((fn.__name__, repr(exc)))
            print(f"  \033[31mFEHL\033[0m {fn.__name__} – Ausnahme: {exc!r}")

    print("\n" + "=" * 62)
    print(f"bestanden: {len(PASSED)}   fehlgeschlagen: {len(FAILED)}")
    for name, detail in FAILED:
        print(f"  - {name}: {detail}")
    print("=" * 62)
    return 1 if FAILED else 0


if __name__ == "__main__":
    sys.exit(main())
