#!/usr/bin/env python3
"""
Harness fuer die automatisierte Durchfuehrung der KI-Laeufe.

Bachelorarbeit: KI-gestuetzte Softwareentwicklung im Vergleich.
Setzt H1-H7 sowie das Zeitregime T1-T3 in ausfuehrbaren Code um.

Nur Standardbibliothek. Getestet gegen Python 3.10+.

Aufruf:
    python3 harness.py init   --config config.json --run A-BMAD-1
    python3 harness.py run    --config config.json --run A-BMAD-1
    python3 harness.py status --config config.json --run A-BMAD-1
    python3 harness.py stop   --config config.json --run A-BMAD-1

Fortsetzen nach einem Stopp: erneut 'run' mit derselben --run-ID.
Der Zustand liegt in runs/<run-id>/state.json und wird nach jedem
abgeschlossenen Schritt fortgeschrieben.
"""

from __future__ import annotations

import argparse
import csv
import hashlib
import json
import os
import re
import shutil
import signal
import subprocess
import sys
import threading
import time
from dataclasses import dataclass, field, asdict
from pathlib import Path
from typing import Any, Optional

import database as dbmod
from audit import (classify_command, hook_guard, inspect_settings,
                   sha256_file)
from prompts import (HEADLESS_PREFIX, ROUTER_PROMPT, bmad_first_prompt,
                     bmad_step_prompt, solo_continue_prompt, solo_first_prompt)

SCHEMA_VERSION = "1"
HARNESS_VERSION = "0.2.0-messlauf"

# ---------------------------------------------------------------------------
# Signalbehandlung: laufenden Schritt zu Ende fuehren, danach anhalten.
# ---------------------------------------------------------------------------

_STOP_REQUESTED = False


def _handle_signal(signum, _frame):
    global _STOP_REQUESTED
    _STOP_REQUESTED = True
    sys.stderr.write(
        f"\n[harness] Signal {signum} empfangen. Der laufende Schritt wird zu Ende "
        f"gefuehrt, danach wird angehalten. Nochmaliges Senden bricht hart ab.\n"
    )
    signal.signal(signum, signal.SIG_DFL)


signal.signal(signal.SIGINT, _handle_signal)
signal.signal(signal.SIGTERM, _handle_signal)


# ---------------------------------------------------------------------------
# Konfiguration
# ---------------------------------------------------------------------------


# Konfigurationszugriff: flache Schluessel und verschachtelte Pfade werden
# ueber __getattr__ aufgeloest, damit nicht je Wert eine Property noetig ist.
# (pfad, default, faktor) - faktor rechnet Stunden/Minuten in Sekunden um.
_FLAT = {
    "claude_bin": "claude", "permission_mode": "bypassPermissions",
    "allowed_tools": [], "disallowed_tools": [],
    "require_no_hooks": True, "progress": True,
}
_NESTED = {
    "t1_active_seconds": ("limits.t1_active_hours", None, 3600),
    "t2_calendar_seconds": ("limits.t2_calendar_hours", None, 3600),
    "t3_step_seconds": ("limits.t3_step_minutes", None, 60),
    # Tokenbudget zaehlt ohne Cache-Reads: die machen ueber 90 Prozent der
    # Rohsumme aus und sagen nichts ueber den Arbeitsaufwand.
    "token_budget": ("limits.token_budget", None, 1),
    "cost_budget_usd": ("limits.cost_budget_usd", None, 1),
    "max_steps": ("limits.max_steps", 400, 1),
    "rate_limit_wait_seconds": ("rate_limit.wait_minutes", 20, 60),
    "rate_limit_max_retries": ("rate_limit.max_retries", 24, 1),
    # "help" = Router entscheidet (Variante B), "fixed" = feste Pflichtkette.
    "routing_mode": ("sequence.routing_mode", "help", 1),
    "fallback_chain": ("sequence.fallback_chain", [], 1),
    # Story-Zyklus laut BMADs eigenem Katalog (module-help.csv). Ihn dort
    # abzulesen statt zu erfragen spart je Story drei Routing-Aufrufe; nach
    # dem letzten Element entscheidet wieder der Router, damit die
    # Verzweigung "bei Befunden zurueck zu dev" erhalten bleibt.
    "story_cycle": ("sequence.story_cycle",
                    ["bmad-create-story", "bmad-dev-story", "bmad-code-review"], 1),
    "route_within_cycle": ("sequence.route_within_cycle", False, 1),
    "solo_max_iterations": ("sequence.solo_max_iterations", 60, 1),
}


@dataclass
class Config:
    raw: dict

    def __getattr__(self, name: str):
        if name in _FLAT:
            return self.raw.get(name, _FLAT[name])
        if name in _NESTED:
            path, default, factor = _NESTED[name]
            cur: Any = self.raw
            for part in path.split("."):
                cur = cur.get(part) if isinstance(cur, dict) else None
                if cur is None:
                    break
            if cur is None:
                cur = default
            return float(cur) * factor if (factor != 1 and cur is not None) else cur
        raise AttributeError(name)

    @property
    def model(self) -> str:
        return self.raw["model"]

    @property
    def runs_root(self) -> Path:
        return Path(self.raw["runs_root"]).expanduser().resolve()

    @property
    def seed_repo(self) -> Path:
        return Path(self.raw["seed_repo"]).expanduser().resolve()

    def seed_repo_for(self, project: str) -> Path:
        """Je Projekt ein eigenes Seed-Repository.

        Die Messlaeufe brauchen zwei getrennte Seeds (Projekt A und B), der
        Probelauf hatte nur einen. Deshalb wird `seed_repos` bevorzugt und auf
        das alte Einzelfeld `seed_repo` zurueckgefallen.
        """
        repos = self.raw.get("seed_repos")
        if repos:
            if project not in repos:
                raise SystemExit(f"Kein seed_repos-Eintrag fuer Projekt '{project}'.")
            return Path(repos[project]).expanduser().resolve()
        return self.seed_repo

    def freeze(self, project: str) -> dict:
        """Sollwerte aus dem Einfrieren: Seed-Commit und Anfangsdatenbestand."""
        return (self.raw.get("freeze") or {}).get(project, {})

    def input_file(self, project: str) -> Path:
        return Path(self.raw["input_files"][project]).expanduser().resolve()

    @property
    def database(self) -> Optional[dict]:
        return dbmod.db_config(self.raw)

    def bmad_source(self) -> Optional[Path]:
        p = self.raw.get("bmad_source")
        return Path(p).expanduser().resolve() if p else None

    @property
    def settings_files(self) -> list[Path]:
        cd = os.environ.get("CLAUDE_CONFIG_DIR")
        base = Path(cd).expanduser() if cd else Path.home() / ".claude"
        return [base / "settings.json"]


def load_config(path: Path) -> Config:
    with path.open(encoding="utf-8") as fh:
        return Config(json.load(fh))


# ---------------------------------------------------------------------------
# Laufzustand
# ---------------------------------------------------------------------------


@dataclass
class RunState:
    run_id: str
    project: str
    condition: str      # "bmad" | "solo" - Studienbedingung, nicht "Arm":
    #                   es gibt weder Teilnehmer noch Randomisierung
    repetition: int

    schema_version: str = SCHEMA_VERSION
    harness_version: str = HARNESS_VERSION

    created_at: float = 0.0
    first_started_at: float = 0.0     # fuer T2
    active_seconds: float = 0.0       # T1, ohne Wartezeiten
    wait_seconds: float = 0.0         # Rate-Limit- und Blockerwartezeit
    step_index: int = 0
    completed_steps: list[dict] = field(default_factory=list)
    tokens: dict = field(default_factory=lambda: {
        "input": 0, "output": 0, "cache_creation": 0, "cache_read": 0})
    cost_usd: float = 0.0
    turns: int = 0
    # Werkzeugzensus: welches Werkzeug wie oft, getrennt nach Haupt- und
    # Subagent. Macht das faktisch genutzte Werkzeugset zur Messgroesse statt
    # zur Annahme.
    tools: dict = field(default_factory=lambda: {"main": {}, "sub": {}})
    network: dict = field(default_factory=lambda: {"fetch": 0, "package": 0,
                                                    "commands": []})
    finished: bool = False
    termination_reason: Optional[str] = None   # regular|T1|T2|token_budget|max_steps|blocker|stopped
    last_router_suggestion: Optional[str] = None

    def path(self, cfg: Config) -> Path:
        return run_dir(cfg, self) / "state.json"

    def save(self, cfg: Config) -> None:
        p = self.path(cfg)
        p.parent.mkdir(parents=True, exist_ok=True)
        tmp = p.with_suffix(".json.tmp")
        with tmp.open("w", encoding="utf-8") as fh:
            json.dump(asdict(self), fh, indent=2, ensure_ascii=False)
        os.replace(tmp, p)   # atomar

    @property
    def total_tokens(self) -> int:
        return sum(self.tokens.values())

    @property
    def billable_tokens(self) -> int:
        """Ohne Cache-Reads - siehe Config.token_budget."""
        return (self.tokens["input"] + self.tokens["output"]
                + self.tokens["cache_creation"])


def run_dir(cfg: Config, st: RunState) -> Path:
    return cfg.runs_root / st.project / st.condition / f"{st.repetition:02d}"


def load_state(cfg: Config, run_id: str) -> Optional[RunState]:
    for p in cfg.runs_root.rglob("state.json"):
        try:
            with p.open(encoding="utf-8") as fh:
                data = json.load(fh)
        except (OSError, json.JSONDecodeError):
            continue
        if data.get("run_id") == run_id:
            # Migration: das Feld hiess frueher "arm".
            if "arm" in data:
                data["condition"] = data.pop("arm")
            return RunState(**data)
    return None


def parse_run_id(run_id: str) -> tuple[str, str, int]:
    """A-BMAD-1 -> ("A", "bmad", 1)"""
    m = re.fullmatch(r"([A-Za-z0-9]+)-(BMAD|SOLO)-(\d+)", run_id)
    if not m:
        raise SystemExit(
            f"Ungueltige Lauf-ID '{run_id}'. Erwartet: <Projekt>-BMAD|SOLO-<Nr>, z.B. A-BMAD-1")
    return m.group(1), m.group(2).lower(), int(m.group(3))


# ---------------------------------------------------------------------------
# Hilfsfunktionen
# ---------------------------------------------------------------------------


def git(workdir: Path, *args: str, check: bool = True) -> subprocess.CompletedProcess:
    return subprocess.run(["git", *args], cwd=workdir, check=check,
                          capture_output=True, text=True)


def now() -> float:
    return time.time()


def iso(ts: float) -> str:
    return time.strftime("%Y-%m-%dT%H:%M:%S%z", time.localtime(ts))


def account(st: RunState, res: "StepResult") -> None:
    """Verbrauch eines Aufrufs in den Laufzustand uebernehmen."""
    st.active_seconds += res.duration_s
    for k in st.tokens:
        st.tokens[k] += res.usage.get(k, 0)
    st.turns += res.turns
    st.cost_usd += res.cost_usd


def usage_fields(res: "StepResult") -> dict:
    """Die Verbrauchsspalten fuer protokoll.csv."""
    return {"duration_s": round(res.duration_s, 1),
            "in_tok": res.usage.get("input"), "out_tok": res.usage.get("output"),
            "cache_c_tok": res.usage.get("cache_creation"),
            "cache_r_tok": res.usage.get("cache_read"),
            "cost_usd": round(res.cost_usd, 6)}


def protokoll_append(cfg: Config, st: RunState, row: dict) -> None:
    p = run_dir(cfg, st) / "logs" / "protokoll.csv"
    p.parent.mkdir(parents=True, exist_ok=True)
    fields = ["timestamp", "run_id", "step_index", "event", "skill",
              "duration_s", "in_tok", "out_tok", "cache_c_tok", "cache_r_tok",
              "cost_usd", "status", "detail"]
    new = not p.exists()
    with p.open("a", newline="", encoding="utf-8") as fh:
        w = csv.DictWriter(fh, fieldnames=fields, extrasaction="ignore")
        if new:
            w.writeheader()
        w.writerow(row)


# ---------------------------------------------------------------------------
# Aufruf der Claude-Code-CLI
# ---------------------------------------------------------------------------

RATE_LIMIT_PATTERNS = [
    r"rate.?limit", r"usage limit", r"limit reached", r"resets? at",
    r"too many requests", r"\b429\b", r"quota",
]


def looks_like_rate_limit(text: str) -> bool:
    t = (text or "").lower()
    return any(re.search(p, t) for p in RATE_LIMIT_PATTERNS)


@dataclass
class StepResult:
    ok: bool
    is_rate_limited: bool
    result_text: str
    session_id: Optional[str]
    duration_s: float
    usage: dict
    turns: int
    cost_usd: float = 0.0
    model_seen: Optional[str] = None
    tools: dict = field(default_factory=lambda: {"main": {}, "sub": {}})
    network: dict = field(default_factory=lambda: {"fetch": 0, "package": 0,
                                                   "commands": []})


def invoke_claude(cfg: Config, workdir: Path, prompt: str,
                  log_path: Path, timeout_s: float) -> StepResult:
    """Ein Prompt, eine frische Session, JSON-Stream-Ausgabe (H3).

    Die Session wird bewusst NICHT fortgesetzt: jede Phase startet mit leerem
    Kontext und muss die von BMAD erzeugten Dateien selbst einlesen. Das ist
    die getroffene Kontextentscheidung und zugleich der Nachweis, dass die
    Artefakte tatsaechlich genutzt werden.
    """
    cmd = [
        cfg.claude_bin,
        "-p", prompt,
        "--output-format", "stream-json",
        "--verbose",
        "--model", cfg.model,
        "--permission-mode", cfg.permission_mode,
    ]
    if cfg.allowed_tools:
        cmd += ["--allowed-tools", ",".join(cfg.allowed_tools)]
    if cfg.disallowed_tools:
        cmd += ["--disallowed-tools", ",".join(cfg.disallowed_tools)]

    log_path.parent.mkdir(parents=True, exist_ok=True)
    usage = {"input": 0, "output": 0, "cache_creation": 0, "cache_read": 0}
    session_id = None
    result_text = ""
    turns = 0
    subtype = ""
    cost_usd = 0.0
    model_seen = None
    tools: dict = {"main": {}, "sub": {}}
    network: dict = {"fetch": 0, "package": 0, "commands": []}
    started = now()

    with log_path.open("w", encoding="utf-8") as log:
        try:
            proc = subprocess.Popen(
                cmd, cwd=workdir, stdout=subprocess.PIPE, stderr=subprocess.PIPE,
                text=True, bufsize=1,
            )
        except FileNotFoundError:
            return StepResult(False, False, f"CLI '{cfg.claude_bin}' nicht gefunden.",
                              None, 0.0, usage, 0)

        # T3-Wachhund: greift auch dann, wenn der Prozess gar nichts ausgibt.
        timed_out = threading.Event()

        def _kill_on_timeout() -> None:
            timed_out.set()
            try:
                proc.kill()
            except Exception:                             # noqa: BLE001
                pass

        watchdog = threading.Timer(timeout_s, _kill_on_timeout)
        watchdog.daemon = True
        watchdog.start()

        try:
            for line in proc.stdout:                     # type: ignore[union-attr]
                log.write(line)
                line = line.strip()
                if not line:
                    continue
                try:
                    ev = json.loads(line)
                except json.JSONDecodeError:
                    continue

                etype = ev.get("type")
                if etype == "system" and ev.get("subtype") == "init":
                    session_id = ev.get("session_id") or session_id
                    model_seen = ev.get("model") or model_seen
                elif etype == "assistant":
                    turns += 1
                    msg = ev.get("message") or {}
                    model_seen = msg.get("model") or model_seen
                    bucket = "sub" if ev.get("parent_tool_use_id") else "main"
                    for c in msg.get("content") or []:
                        if not isinstance(c, dict):
                            continue
                        if c.get("type") == "tool_use":
                            name = c.get("name") or "?"
                            tools[bucket][name] = tools[bucket].get(name, 0) + 1
                            if name == "Bash":
                                cmd = (c.get("input") or {}).get("command") or ""
                                kind = classify_command(cmd)
                                if kind == "package":
                                    network["package"] += 1
                                elif kind == "fetch":
                                    network["fetch"] += 1
                                    if len(network["commands"]) < 50:
                                        network["commands"].append(
                                            " ".join(cmd.split())[:300])
                            if cfg.progress:
                                mark = "  ." if bucket == "sub" else ""
                                print(f"           [{name}]{mark}", flush=True)
                        elif (cfg.progress and c.get("type") == "text"
                              and (c.get("text") or "").strip()):
                            txt = " ".join(c["text"].split())[:150]
                            print(f"           > {txt}", flush=True)
                    u = msg.get("usage") or {}
                    usage["input"] += int(u.get("input_tokens") or 0)
                    usage["output"] += int(u.get("output_tokens") or 0)
                    usage["cache_creation"] += int(u.get("cache_creation_input_tokens") or 0)
                    usage["cache_read"] += int(u.get("cache_read_input_tokens") or 0)
                elif etype == "result":
                    subtype = ev.get("subtype") or ""
                    result_text = ev.get("result") or ev.get("error") or ""
                    u = ev.get("usage") or {}
                    # Falls die CLI eine Gesamtsumme liefert, hat sie Vorrang.
                    if u:
                        usage = {
                            "input": int(u.get("input_tokens") or usage["input"]),
                            "output": int(u.get("output_tokens") or usage["output"]),
                            "cache_creation": int(u.get("cache_creation_input_tokens")
                                                  or usage["cache_creation"]),
                            "cache_read": int(u.get("cache_read_input_tokens")
                                              or usage["cache_read"]),
                        }
                    if ev.get("num_turns"):
                        turns = int(ev["num_turns"])
                    # Kosten: die CLI rechnet sie auch im Abo-Betrieb aus.
                    if ev.get("total_cost_usd") is not None:
                        cost_usd = float(ev["total_cost_usd"])
                    mu = ev.get("modelUsage") or {}
                    if isinstance(mu, dict) and mu:
                        model_seen = model_seen or next(iter(mu))
                        if not cost_usd:
                            cost_usd = sum(float(v.get("costUSD") or 0)
                                           for v in mu.values() if isinstance(v, dict))
                        # Subagenten erscheinen hier mit; Summe hat Vorrang.
                        agg = {"input": 0, "output": 0,
                               "cache_creation": 0, "cache_read": 0}
                        for v in mu.values():
                            if not isinstance(v, dict):
                                continue
                            agg["input"] += int(v.get("inputTokens") or 0)
                            agg["output"] += int(v.get("outputTokens") or 0)
                            agg["cache_creation"] += int(v.get("cacheCreationInputTokens") or 0)
                            agg["cache_read"] += int(v.get("cacheReadInputTokens") or 0)
                        if sum(agg.values()) > sum(usage.values()):
                            usage = agg

                if timed_out.is_set():
                    break
        finally:
            watchdog.cancel()
            stderr = ""
            if proc.stderr:
                try:
                    stderr = proc.stderr.read()
                except Exception:                         # noqa: BLE001
                    stderr = ""
                if stderr:
                    log.write("\n--- stderr ---\n" + stderr)
            code = proc.wait()

    if timed_out.is_set():
        return StepResult(False, False,
                          f"T3 ueberschritten ({timeout_s/60:.1f} min) - Blocker.",
                          session_id, now() - started, usage, turns)

    duration = now() - started
    haystack = f"{result_text}\n{subtype}\n{stderr}"
    rl = looks_like_rate_limit(haystack) and (code != 0 or subtype.startswith("error"))
    ok = code == 0 and not subtype.startswith("error")
    return StepResult(ok, rl, result_text, session_id, duration, usage,
                      turns, cost_usd, model_seen, tools, network)


# ---------------------------------------------------------------------------
# Antwortauswertung (H4 / H7)
# ---------------------------------------------------------------------------

_JSON_BLOCK = re.compile(r"\{.*\}", re.DOTALL)

def extract_json(text: str) -> Optional[dict]:
    """Holt das letzte JSON-Objekt aus einer Antwort.

    BMAD-Skills im Headless-Modus schliessen mit einem JSON-Status ab
    (status: complete|partial|blocked, assumptions[], open_questions[]).
    """
    if not text:
        return None
    for m in reversed(list(_JSON_BLOCK.finditer(text))):
        try:
            obj = json.loads(m.group(0))
            if isinstance(obj, dict):
                return obj
        except json.JSONDecodeError:
            continue
    return None


def classify(result: StepResult) -> tuple[str, dict]:
    """-> ("complete"|"partial"|"blocked"|"error", payload)"""
    if not result.ok:
        return "error", {"reason": result.result_text[:2000]}
    payload = extract_json(result.result_text) or {}
    status = str(payload.get("status", "")).lower()
    if status in ("complete", "partial", "blocked"):
        return status, payload
    # Kein JSON-Status: Skills ohne Headless-Spezifikation (Phase 4).
    return "complete", payload


# ---------------------------------------------------------------------------
# Prompts
# ---------------------------------------------------------------------------

# ---------------------------------------------------------------------------
# init
# ---------------------------------------------------------------------------


def verify_seed(cfg: Config, project: str, seed_src: Path) -> None:
    """Gegenprobe gegen die Freeze-Werte aus seed.meta.json.

    Der Harness legt im Arbeitsverzeichnis einen eigenen Commit an, dessen Hash
    zwangslaeufig ein anderer ist als der des Seed-Repositorys. Geprueft wird
    deshalb die Quelle, nicht die Kopie. Ohne diese Pruefung faellt ein
    nachtraeglich veraendertes Seed-Repository erst bei der Auswertung auf,
    also zu spaet.
    """
    soll = cfg.freeze(project)
    if not soll:
        return
    erwartet = soll.get("seed_commit")
    if erwartet:
        got = git(seed_src, "rev-parse", "HEAD", check=False).stdout.strip()
        if got != erwartet:
            raise SystemExit(
                f"Seed-Repository {seed_src} weicht vom eingefrorenen Commit ab.\n"
                f"  erwartet {erwartet}\n  gefunden {got or '(kein Git-Repository)'}")
    datei = soll.get("anfangsdatenbestand_datei")
    hashwert = soll.get("anfangsdatenbestand_sha256")
    if datei and hashwert:
        p = seed_src / datei
        if not p.exists():
            raise SystemExit(f"Anfangsdatenbestand fehlt im Seed: {p}")
        got = sha256_file(p)
        if got != hashwert:
            raise SystemExit(
                f"Anfangsdatenbestand weicht vom eingefrorenen Hash ab: {p}\n"
                f"  erwartet {hashwert}\n  gefunden {got}")


def cmd_init(cfg: Config, run_id: str) -> None:
    project, condition, rep = parse_run_id(run_id)
    # Zuerst pruefen, damit bei aktiven Hooks kein halb angelegtes
    # Laufverzeichnis zurueckbleibt.
    hook_guard(cfg, inspect_settings(cfg))
    st = RunState(run_id=run_id, project=project, condition=condition, repetition=rep,
                  created_at=now())
    rd = run_dir(cfg, st)
    if rd.exists() and any(rd.iterdir()):
        raise SystemExit(f"{rd} existiert bereits und ist nicht leer. Abbruch (H1).")

    for sub in ("logs", "artifacts", "code", "metrics"):
        (rd / sub).mkdir(parents=True, exist_ok=True)

    # H1: frisches Arbeitsverzeichnis aus identischem Seed-Commit.
    workdir = rd / "code"
    if workdir.exists():
        shutil.rmtree(workdir)
    seed_src = cfg.seed_repo_for(project)
    verify_seed(cfg, project, seed_src)
    shutil.copytree(seed_src, workdir,
                    ignore=shutil.ignore_patterns(".git", "node_modules", "bin", "obj"))

    # BMAD-Definitionen nur in der BMAD-Bedingung einspielen: einziger struktureller
    # Unterschied zwischen den Bedingungen.
    src = cfg.bmad_source()
    if condition == "bmad":
        if not src or not src.exists():
            raise SystemExit("bmad_source ist nicht gesetzt oder existiert nicht.")
        for item in ("_bmad", ".claude"):
            s = src / item
            if s.exists():
                shutil.copytree(s, workdir / item, dirs_exist_ok=True)
    else:
        for item in ("_bmad", ".claude"):
            p = workdir / item
            if p.exists():
                shutil.rmtree(p)

    git(workdir, "init", "-q")
    git(workdir, "add", "-A")
    git(workdir, "-c", "user.email=harness@local", "-c", "user.name=Harness",
        "commit", "-q", "-m", f"seed: {run_id}")
    seed_commit = git(workdir, "rev-parse", "HEAD").stdout.strip()

    # Wirksame Konfiguration inklusive Projektebene fuer die Dokumentation.
    settings = inspect_settings(cfg, workdir)
    hook_guard(cfg, settings)

    # H6: config.yaml mit allen Reproduktionsangaben.
    inp = cfg.input_file(project)
    conf = {
        "run_id": run_id, "project": project, "condition": condition, "repetition": rep,
        "created_at": iso(now()),
        "harness_version": HARNESS_VERSION,
        "model": cfg.model,
        "permission_mode": cfg.permission_mode,
        "allowed_tools": cfg.allowed_tools,
        "disallowed_tools": cfg.disallowed_tools,
        "input_file": str(inp),
        "input_sha256": sha256_file(inp),
        "seed_repo": str(seed_src),
        "seed_commit": seed_commit,
        "seed_commit_erwartet": cfg.freeze(project).get("seed_commit"),
        "limits": cfg.raw["limits"],
        "routing_mode": cfg.routing_mode,
        "claude_settings": settings,
    }
    if condition == "bmad" and src:
        man = src / "_bmad" / "_config" / "manifest.yaml"
        if man.exists():
            conf["bmad_manifest_sha256"] = sha256_file(man)
            conf["bmad_manifest"] = man.read_text(encoding="utf-8")
        fm = src / "_bmad" / "_config" / "files-manifest.csv"
        if fm.exists():
            conf["bmad_files_manifest_sha256"] = sha256_file(fm)
    # Persistenz: leere Datenbank je Lauf, Verbindung erst zur Laufzeit.
    dbcfg = cfg.database
    if dbcfg:
        name = dbmod.database_name(dbcfg, project, condition, rep)
        dbmod.reset_database(dbcfg, name)
        conf["database"] = {
            "name": name,
            "env_var": dbmod.env_var(dbcfg),
            "connection_string": dbmod.connection_string(dbcfg, name, redact=True),
            "engine": dbmod.engine_fingerprint(dbcfg),
        }

    (rd / "config.json").write_text(
        json.dumps(conf, indent=2, ensure_ascii=False), encoding="utf-8")

    st.save(cfg)
    print(f"[init] {run_id} vorbereitet unter {rd}")
    print(f"[init] Seed-Commit {seed_commit[:12]} · Input-Hash {conf['input_sha256'][:12]}")
    if dbcfg:
        print(f"[init] Datenbank {conf['database']['name']} neu angelegt (leer)")


# ---------------------------------------------------------------------------
# run
# ---------------------------------------------------------------------------


def stop_flag(cfg: Config, st: RunState) -> Path:
    return run_dir(cfg, st) / "STOP"


def should_stop(cfg: Config, st: RunState) -> bool:
    return _STOP_REQUESTED or stop_flag(cfg, st).exists()


def check_limits(cfg: Config, st: RunState) -> Optional[str]:
    if st.active_seconds >= cfg.t1_active_seconds:
        return "T1"
    if st.first_started_at and (now() - st.first_started_at) >= cfg.t2_calendar_seconds:
        return "T2"
    if cfg.token_budget and st.billable_tokens >= cfg.token_budget:
        return "token_budget"
    if cfg.cost_budget_usd and st.cost_usd >= cfg.cost_budget_usd:
        return "cost_budget"
    if st.step_index >= cfg.max_steps:
        return "max_steps"
    return None


def wait_for_rate_limit(cfg: Config, st: RunState, attempt: int) -> None:
    """Wartezeit zaehlt NICHT auf T1 - sie ist ein Umgebungseffekt, kein Aufwand
    des Werkzeugs. Sie wird getrennt protokolliert, damit C1c bereinigt werden
    kann."""
    wait = cfg.rate_limit_wait_seconds
    t0 = now()
    print(f"[limit] Rate-Limit erkannt (Versuch {attempt}). Warte {wait/60:.0f} min.")
    protokoll_append(cfg, st, {
        "timestamp": iso(t0), "run_id": st.run_id, "step_index": st.step_index,
        "event": "rate_limit_wait_start", "detail": f"attempt={attempt}"})
    end = t0 + wait
    while now() < end:
        if should_stop(cfg, st):
            break
        time.sleep(min(30.0, end - now()))
    st.wait_seconds += now() - t0
    protokoll_append(cfg, st, {
        "timestamp": iso(now()), "run_id": st.run_id, "step_index": st.step_index,
        "event": "rate_limit_wait_end", "duration_s": round(now() - t0, 1)})
    st.save(cfg)


def next_step_bmad(cfg: Config, st: RunState, workdir: Path,
                   input_text: str) -> Optional[dict]:
    """Variante B: das Framework bestimmt den naechsten Schritt (R2/R3)."""
    if st.step_index == 0:
        return {"kind": "first", "skill": "bmad-help+entry", "action": None,
                "prompt": bmad_first_prompt(input_text)}

    if cfg.routing_mode == "fixed":
        chain = cfg.fallback_chain
        idx = st.step_index - 1
        if idx >= len(chain):
            return None
        e = chain[idx]
        return {"kind": "step", "skill": e["skill"], "action": e.get("action"),
                "prompt": bmad_step_prompt(e["skill"], e.get("action"), input_text)}

    # Innerhalb des Story-Zyklus folgt der Harness der vom Framework selbst
    # dokumentierten Reihenfolge, statt sie je Schritt zu erfragen.
    if not cfg.route_within_cycle and st.completed_steps:
        cycle = cfg.story_cycle
        last = st.completed_steps[-1].get("skill")
        if last in cycle and cycle.index(last) < len(cycle) - 1:
            nxt_skill = cycle[cycle.index(last) + 1]
            return {"kind": "step", "skill": nxt_skill, "action": None,
                    "source": "cycle",
                    "prompt": bmad_step_prompt(nxt_skill, None, input_text)}

    # Routing-Aufruf
    log = run_dir(cfg, st) / "logs" / f"step-{st.step_index:03d}-router.jsonl"
    t0 = now()
    res = invoke_claude(cfg, workdir, ROUTER_PROMPT, log, cfg.t3_step_seconds)
    account(st, res)
    protokoll_append(cfg, st, {
        "timestamp": iso(t0), "run_id": st.run_id, "step_index": st.step_index,
        "event": "router", "skill": "bmad-help",
        "status": "ok" if res.ok else "error",
        "detail": (res.result_text or "")[:300].replace("\n", " "),
        **usage_fields(res)})

    if res.is_rate_limited:
        return {"kind": "rate_limited"}
    payload = extract_json(res.result_text) or {}
    if payload.get("done") is True:
        return None
    skill = payload.get("next_skill")
    if not skill:
        # Rueckfallebene: feste Pflichtkette an der aktuellen Position.
        chain = cfg.fallback_chain
        used = {s.get("skill") for s in st.completed_steps}
        for e in chain:
            if e["skill"] not in used:
                st.last_router_suggestion = "fallback"
                return {"kind": "step", "skill": e["skill"], "action": e.get("action"),
                        "prompt": bmad_step_prompt(e["skill"], e.get("action"), input_text)}
        return None
    st.last_router_suggestion = skill
    action = payload.get("action") or None
    return {"kind": "step", "skill": skill, "action": action,
            "prompt": bmad_step_prompt(skill, action, input_text)}


def next_step_solo(cfg: Config, st: RunState, input_text: str) -> Optional[dict]:
    if st.step_index == 0:
        return {"kind": "step", "skill": "solo", "action": None,
                "prompt": solo_first_prompt(input_text)}
    if st.step_index >= cfg.solo_max_iterations:
        return None
    last = st.completed_steps[-1] if st.completed_steps else {}
    if last.get("declared_done"):
        return None
    return {"kind": "step", "skill": "solo", "action": None,
            "prompt": solo_continue_prompt(input_text)}


def cmd_run(cfg: Config, run_id: str) -> None:
    st = load_state(cfg, run_id)
    if st is None:
        raise SystemExit(f"Kein Zustand fuer '{run_id}'. Zuerst 'init' ausfuehren.")
    if st.finished:
        print(f"[run] {run_id} ist bereits beendet ({st.termination_reason}).")
        return

    rd = run_dir(cfg, st)
    workdir = rd / "code"
    input_text = cfg.input_file(st.project).read_text(encoding="utf-8")

    # H2: Hash-Abgleich vor jedem Lauf, Abbruch bei Abweichung.
    conf = json.loads((rd / "config.json").read_text(encoding="utf-8"))
    actual = sha256_file(cfg.input_file(st.project))
    if actual != conf["input_sha256"]:
        raise SystemExit(
            "Eingabedatei weicht vom eingefrorenen Hash ab. Abbruch (H2).\n"
            f"  erwartet {conf['input_sha256']}\n  gefunden {actual}")

    # Laufzeit-Injektion der Verbindungszeichenfolge (H1 auf der
    # Persistenzseite). Bewusst ueber die Umgebung des Harness-Prozesses: die
    # CLI erbt sie, und der Seed-Commit bleibt ueber alle sechs Laeufe eines
    # Projekts byteidentisch.
    dbcfg = cfg.database
    if dbcfg:
        name = (conf.get("database") or {}).get(
            "name") or dbmod.database_name(dbcfg, st.project, st.condition, st.repetition)
        os.environ[dbmod.env_var(dbcfg)] = dbmod.connection_string(dbcfg, name)
        print(f"[run] Datenbank {name} über {dbmod.env_var(dbcfg)} bereitgestellt")

    if stop_flag(cfg, st).exists():
        stop_flag(cfg, st).unlink()

    if not st.first_started_at:
        st.first_started_at = now()
        st.save(cfg)

    print(f"[run] {run_id} · Bedingung={st.condition} · Schritt {st.step_index} · "
          f"T1 {st.active_seconds/3600:.2f}/{cfg.t1_active_seconds/3600:.1f} h")

    rl_attempts = 0

    while True:
        reason = check_limits(cfg, st)
        if reason:
            terminate(cfg, st, reason)
            return
        if should_stop(cfg, st):
            print("[run] Stopp angefordert. Zustand gesichert, Lauf pausiert.")
            st.save(cfg)
            return

        if st.condition == "bmad":
            nxt = next_step_bmad(cfg, st, workdir, input_text)
        else:
            nxt = next_step_solo(cfg, st, input_text)

        if nxt is None:
            terminate(cfg, st, "regular")
            return
        if nxt.get("kind") == "rate_limited":
            rl_attempts += 1
            if rl_attempts > cfg.rate_limit_max_retries:
                terminate(cfg, st, "blocker")
                return
            wait_for_rate_limit(cfg, st, rl_attempts)
            continue

        # --- Schritt ausfuehren -----------------------------------------
        idx = st.step_index
        skill = nxt["skill"]
        log = rd / "logs" / f"step-{idx:03d}-{re.sub(r'[^a-z0-9-]', '_', skill.lower())}.jsonl"
        t0 = now()
        print(f"[step {idx:03d}] {skill} ...")
        res = invoke_claude(cfg, workdir, nxt["prompt"], log, cfg.t3_step_seconds)

        if res.is_rate_limited:
            rl_attempts += 1
            if rl_attempts > cfg.rate_limit_max_retries:
                terminate(cfg, st, "blocker")
                return
            wait_for_rate_limit(cfg, st, rl_attempts)
            continue
        rl_attempts = 0

        account(st, res)
        for bucket in ("main", "sub"):
            for name, cnt in res.tools.get(bucket, {}).items():
                st.tools[bucket][name] = st.tools[bucket].get(name, 0) + cnt
        st.network["fetch"] += res.network.get("fetch", 0)
        st.network["package"] += res.network.get("package", 0)
        st.network["commands"].extend(res.network.get("commands", [])[:20])
        if res.network.get("fetch"):
            # Fremdinhalte geholt: Befund, kein Abbruch. Relevant fuer die
            # Datenleckage-Diskussion und fuer FF4.
            for cmd in res.network["commands"]:
                protokoll_append(cfg, st, {
                    "timestamp": iso(now()), "run_id": st.run_id,
                    "step_index": idx, "event": "network_fetch",
                    "skill": skill, "status": "befund", "detail": cmd})
            print(f"[netz] {res.network['fetch']} externe Abrufe in diesem Schritt "
                  f"- siehe protokoll.csv")

        # Modelldrift ist ein Befund, kein Detail: der Anbieter kann einen
        # Alias jederzeit umhaengen.
        if res.model_seen and res.model_seen != cfg.model:
            protokoll_append(cfg, st, {
                "timestamp": iso(now()), "run_id": st.run_id, "step_index": idx,
                "event": "model_mismatch", "status": "warn",
                "detail": f"konfiguriert={cfg.model} verwendet={res.model_seen}"})
            print(f"[warnung] Modellabweichung: konfiguriert {cfg.model}, "
                  f"verwendet {res.model_seen}")

        status, payload = classify(res)
        declared_done = bool(re.search(r"FERTIG:\s*JA", res.result_text or "",
                                       re.IGNORECASE))

        # H5: Commit und Tag je Schritt.
        tag = f"step-{idx:03d}-{re.sub(r'[^a-zA-Z0-9-]', '-', skill)[:40]}"
        try:
            git(workdir, "add", "-A", check=False)
            git(workdir, "-c", "user.email=harness@local", "-c", "user.name=Harness",
                "commit", "-q", "--allow-empty", "-m", f"{tag} [{status}]", check=False)
            git(workdir, "tag", "-f", tag, check=False)
            commit = git(workdir, "rev-parse", "HEAD", check=False).stdout.strip()
        except Exception:                                   # noqa: BLE001
            commit = ""

        st.completed_steps.append({
            "index": idx, "skill": skill, "action": nxt.get("action"),
            "started_at": iso(t0), "duration_s": round(res.duration_s, 1),
            "status": status, "session_id": res.session_id, "commit": commit,
            "source": nxt.get("source", "router"),
            "usage": res.usage, "turns": res.turns,
            "cost_usd": round(res.cost_usd, 6), "model_seen": res.model_seen,
            "tools": res.tools,
            "assumptions": payload.get("assumptions", []),
            "open_questions": payload.get("open_questions", []),
            "reason": payload.get("reason"),
            "declared_done": declared_done,
            "log": str(log.relative_to(rd)),
        })
        st.step_index += 1
        st.save(cfg)

        protokoll_append(cfg, st, {
            "timestamp": iso(t0), "run_id": st.run_id, "step_index": idx,
            "event": "step", "skill": skill,
            "status": status, **usage_fields(res),
            "detail": (payload.get("reason") or res.result_text or "")[:300].replace("\n", " ")})

        print(f"[step {idx:03d}] {skill} -> {status} · {res.duration_s/60:.1f} min · "
              f"{sum(res.usage.values()):,} Tokens · {res.cost_usd:.4f} USD")

        # H7: Blocker haelt den Lauf an und erzwingt einen protokollierten Eingriff.
        if status in ("blocked", "error"):
            print(f"[blocker] {payload.get('reason') or res.result_text[:200]}")
            terminate(cfg, st, "blocker")
            return


def terminate(cfg: Config, st: RunState, reason: str) -> None:
    st.finished = True
    st.termination_reason = reason
    st.save(cfg)
    protokoll_append(cfg, st, {
        "timestamp": iso(now()), "run_id": st.run_id, "step_index": st.step_index,
        "event": "terminated", "status": reason,
        "detail": f"T1={st.active_seconds/3600:.2f}h wait={st.wait_seconds/3600:.2f}h "
                  f"tokens={st.total_tokens}"})
    print(f"\n[ende] {st.run_id} terminiert: {reason}")
    print(f"       T1 aktiv {st.active_seconds/3600:.2f} h · "
          f"Wartezeit {st.wait_seconds/3600:.2f} h · "
          f"Schritte {st.step_index} · Turns {st.turns} · "
          f"Tokens {st.total_tokens:,} · {st.cost_usd:.2f} USD")


# ---------------------------------------------------------------------------
# status / stop
# ---------------------------------------------------------------------------


def cmd_status(cfg: Config, run_id: str) -> None:
    st = load_state(cfg, run_id)
    if st is None:
        raise SystemExit(f"Kein Zustand fuer '{run_id}'.")
    print(json.dumps({
        "run_id": st.run_id, "condition": st.condition, "project": st.project,
        "repetition": st.repetition, "finished": st.finished,
        "termination_reason": st.termination_reason,
        "steps": st.step_index, "turns": st.turns,
        "t1_active_h": round(st.active_seconds / 3600, 2),
        "t1_budget_h": cfg.t1_active_seconds / 3600,
        "wait_h": round(st.wait_seconds / 3600, 2),
        "t2_elapsed_h": round((now() - st.first_started_at) / 3600, 2)
                        if st.first_started_at else 0,
        "tokens": st.tokens, "tokens_total": st.total_tokens,
        "tokens_billable": st.billable_tokens,
        "cost_usd": round(st.cost_usd, 4),
        "network": {"fetch": st.network["fetch"], "package": st.network["package"]},
        "tools": {k: dict(sorted(v.items(), key=lambda x: -x[1]))
                  for k, v in st.tools.items()},
        "last_skills": [s["skill"] for s in st.completed_steps[-5:]],
    }, indent=2, ensure_ascii=False))


def cmd_reopen(cfg: Config, run_id: str) -> None:
    """Einen terminierten Lauf wieder oeffnen.

    Das ist ein Operator-Eingriff nach R6 und wird als solcher protokolliert.
    Fuer die Messlaeufe gilt: nach Terminierung findet keine Nachbesserung
    statt. Der Befehl existiert fuer den Probelauf und fuer Faelle, in denen
    ein Lauf aus Umgebungsgruenden (T2, Rate-Limit) endete - dort ist die
    Fortsetzung kein Verstoss, sondern die korrekte Behandlung.
    """
    st = load_state(cfg, run_id)
    if st is None:
        raise SystemExit(f"Kein Zustand fuer '{run_id}'.")
    if not st.finished:
        print(f"[reopen] {run_id} laeuft noch, nichts zu tun.")
        return
    old = st.termination_reason
    st.finished = False
    st.termination_reason = None
    st.save(cfg)
    protokoll_append(cfg, st, {
        "timestamp": iso(now()), "run_id": st.run_id, "step_index": st.step_index,
        "event": "reopened_by_operator", "status": "intervention",
        "detail": f"vorheriger Terminierungsgrund: {old}"})
    print(f"[reopen] {run_id} wieder geoeffnet (war: {old}).")
    print("[reopen] Als Operator-Eingriff in protokoll.csv vermerkt (C4).")
    print("[reopen] Deckel in der Konfiguration anpassen, sonst greift derselbe sofort wieder.")


def cmd_stop(cfg: Config, run_id: str) -> None:
    st = load_state(cfg, run_id)
    if st is None:
        raise SystemExit(f"Kein Zustand fuer '{run_id}'.")
    f = stop_flag(cfg, st)
    f.parent.mkdir(parents=True, exist_ok=True)
    f.write_text(iso(now()), encoding="utf-8")
    print(f"[stop] Stoppmarkierung gesetzt: {f}\n"
          f"       Der laufende Schritt wird zu Ende gefuehrt, danach haelt der Lauf an.\n"
          f"       Fortsetzen mit: run --run {run_id}")


# ---------------------------------------------------------------------------


def cmd_archive(cfg: Config, run_id: str) -> None:
    """Einen abgeschlossenen Lauf in eine veroeffentlichbare Form bringen.

    Auf der Platte ist ein Lauf einige hundert Megabyte gross, davon der
    weitaus groesste Teil `node_modules` und Build-Ausgabe, also
    wiederherstellbarer Ballast. Die eigentlichen Forschungsdaten sind die
    Git-Historie des Arbeitsverzeichnisses samt der Tags je Schritt und die
    JSONL-Logs. `git bundle` packt die vollstaendige Historie in eine einzige
    Datei, die sich mit `git clone <datei>` wieder auspacken laesst.
    """
    st = load_state(cfg, run_id)
    if st is None:
        raise SystemExit(f"Kein Zustand fuer '{run_id}'.")
    if not st.finished:
        print(f"[archive] Warnung: {run_id} ist noch nicht beendet "
              f"(Schritt {st.step_index}). Das Archiv ist damit ein Zwischenstand.")

    rd = run_dir(cfg, st)
    out = cfg.runs_root.parent / "archiv" / run_id
    out.mkdir(parents=True, exist_ok=True)

    bundle = out / f"{run_id}-code.bundle"
    git(rd / "code", "bundle", "create", str(bundle), "--all")

    logs = out / f"{run_id}-logs.tar.gz"
    subprocess.run(["tar", "-czf", str(logs), "-C", str(rd), "logs"], check=True)

    for name in ("config.json", "state.json"):
        if (rd / name).exists():
            shutil.copy2(rd / name, out / name)

    manifest = {
        "run_id": run_id, "project": st.project, "condition": st.condition,
        "repetition": st.repetition,
        "archiviert_am": iso(now()),
        "harness_version": HARNESS_VERSION,
        "beendet": st.finished,
        "termination_reason": st.termination_reason,
        "schritte": st.step_index,
        "aktivzeit_stunden": round(st.active_seconds / 3600, 3),
        "wartezeit_stunden": round(st.wait_seconds / 3600, 3),
        "tokens": st.tokens,
        "kosten_usd": round(st.cost_usd, 4),
        "turns": st.turns,
        "dateien": {f.name: {"sha256": sha256_file(f), "bytes": f.stat().st_size}
                    for f in sorted(out.iterdir()) if f.name != "MANIFEST.json"},
    }
    (out / "MANIFEST.json").write_text(
        json.dumps(manifest, indent=2, ensure_ascii=False), encoding="utf-8")

    gesamt = sum(f.stat().st_size for f in out.iterdir())
    print(f"[archive] {run_id} -> {out}")
    print(f"[archive] {bundle.stat().st_size/1e6:.1f} MB Historie · "
          f"{logs.stat().st_size/1e6:.1f} MB Logs · {gesamt/1e6:.1f} MB gesamt")
    print(f"[archive] Auspacken mit: git clone {bundle.name} <ziel>")


def cmd_dbcheck(cfg: Config, _run_id: Optional[str] = None) -> None:
    """Erreichbarkeit der Datenbank pruefen, ohne einen Lauf zu starten.

    Gedacht als Vorabpruefung vor dem ersten init: ein fehlendes Kennwort oder
    ein gestoppter Container faellt sonst erst nach dem Anlegen des
    Laufverzeichnisses auf.
    """
    dbcfg = cfg.database
    if not dbcfg:
        print("[dbcheck] Kein aktiver database-Abschnitt in der Konfiguration.")
        return
    print(f"[dbcheck] {dbmod.check(dbcfg)}")
    probe = "harness_dbcheck_probe"
    dbmod.reset_database(dbcfg, probe)
    dbmod.run_sql(dbcfg, f"DROP DATABASE [{probe}];")
    print("[dbcheck] Anlegen und Loeschen einer Datenbank erfolgreich.")
    for project in sorted((cfg.raw.get("input_files") or {})):
        for condition in ("bmad", "solo"):
            for r in (1, 2, 3):
                print(f"[dbcheck] geplant: "
                      f"{dbmod.database_name(dbcfg, project, condition, r)}")


def main() -> None:
    ap = argparse.ArgumentParser(description="Harness fuer die automatisierten KI-Laeufe")
    ap.add_argument("command",
                    choices=["init", "run", "status", "stop", "reopen",
                             "dbcheck", "archive"])
    ap.add_argument("--config", required=True, type=Path)
    ap.add_argument("--run", dest="run_id")
    args = ap.parse_args()

    if args.command != "dbcheck" and not args.run_id:
        ap.error("--run ist fuer diesen Befehl erforderlich")

    cfg = load_config(args.config)
    {"init": cmd_init, "run": cmd_run, "status": cmd_status,
     "stop": cmd_stop, "reopen": cmd_reopen,
     "dbcheck": cmd_dbcheck, "archive": cmd_archive}[args.command](cfg, args.run_id)


if __name__ == "__main__":
    main()
