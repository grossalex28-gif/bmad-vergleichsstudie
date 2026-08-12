#!/usr/bin/env python3
"""
Attrappe der Claude-Code-CLI fuer den Selbsttest des Harness.

Ahmt `claude -p "<prompt>" --output-format stream-json --verbose ...` nach:
gibt JSONL im selben Format aus, schreibt Dateien ins Arbeitsverzeichnis und
laesst sich ueber Umgebungsvariablen in Fehlerszenarien zwingen.

Verbraucht keine Tokens und kontaktiert kein Netzwerk.

Steuerung ueber Umgebungsvariablen (jeweils 1-basierte Aufrufnummer):
    FAKE_RATE_LIMIT_AT   Aufruf endet mit Rate-Limit-Fehler
    FAKE_BLOCK_AT        Aufruf gibt status "blocked" zurueck
    FAKE_SLEEP_AT        Aufruf schlaeft FAKE_SLEEP_SECONDS ohne Ausgabe
    FAKE_SLEEP_SECONDS   Standard 120
    FAKE_STEP_SECONDS    kuenstliche Dauer je normalem Aufruf, Standard 0
    FAKE_TOKENS          Tokens je Aufruf (Output), Standard 500
    FAKE_SOLO_DONE_AT    Aufruf, ab dem die Solo-Bedingung "FERTIG: JA" meldet
    FAKE_CHAIN           Komma-Liste der Skills, die der Router nacheinander nennt
"""

from __future__ import annotations

import json
import os
import sys
import time
from pathlib import Path

COUNTER = Path(".fake_claude_counter")

DEFAULT_CHAIN = [
    "bmad-prd",
    "bmad-architecture",
    "bmad-create-epics-and-stories",
    "bmad-check-implementation-readiness",
    "bmad-sprint-planning",
    "bmad-create-story",
    "bmad-dev-story",
]


def env_int(name: str, default: int = 0) -> int:
    try:
        return int(os.environ.get(name, "") or default)
    except ValueError:
        return default


def get_prompt(argv: list[str]) -> str:
    for i, a in enumerate(argv):
        if a == "-p" and i + 1 < len(argv):
            return argv[i + 1]
    return ""


def bump() -> int:
    n = 0
    if COUNTER.exists():
        try:
            n = int(COUNTER.read_text().strip() or 0)
        except ValueError:
            n = 0
    n += 1
    COUNTER.write_text(str(n))
    return n


def emit(obj: dict) -> None:
    sys.stdout.write(json.dumps(obj, ensure_ascii=False) + "\n")
    sys.stdout.flush()


def main() -> int:
    prompt = get_prompt(sys.argv[1:])
    n = bump()
    tokens = env_int("FAKE_TOKENS", 500)

    # --- Szenario: Prozess haengt (T3-Wachhund testen) -------------------
    if n == env_int("FAKE_SLEEP_AT", -1):
        time.sleep(env_int("FAKE_SLEEP_SECONDS", 120))
        return 0

    # Vor dem init-Ereignis koennen Hook-Ereignisse stehen - der Parser muss
    # damit umgehen (real beobachtet bei aktivem SessionStart-Hook).
    emit({"type": "system", "subtype": "hook_started",
          "hook_name": "SessionStart:startup", "session_id": f"fake-session-{n:04d}"})
    emit({"type": "system", "subtype": "init", "session_id": f"fake-session-{n:04d}",
          "model": os.environ.get("FAKE_MODEL", "test-model"),
          "cwd": str(Path.cwd())})

    # --- Szenario: Rate-Limit -------------------------------------------
    if n == env_int("FAKE_RATE_LIMIT_AT", -1):
        emit({"type": "result", "subtype": "error_during_execution",
              "is_error": True,
              "error": "API Error 429: rate limit exceeded, resets at 18:00",
              "usage": {"input_tokens": 10, "output_tokens": 0},
              "num_turns": 0})
        sys.stderr.write("rate limit exceeded (429)\n")
        return 1

    delay = env_int("FAKE_STEP_SECONDS", 0)
    if delay:
        time.sleep(delay)

    # Ein Haupt- und ein Subagenten-Ereignis, damit der Werkzeugzensus greift.
    emit({"type": "assistant", "parent_tool_use_id": "toolu_fake",
          "message": {"role": "assistant",
                      "content": [{"type": "tool_use", "name": "Read", "input": {}}],
                      "usage": {"input_tokens": 10, "output_tokens": 5}}})
    emit({"type": "assistant", "message": {
        "role": "assistant",
        "content": [{"type": "text", "text": "..."},
                    {"type": "tool_use", "name": "Bash",
                     "input": {"command": os.environ.get(
                         "FAKE_BASH_CMD", "npm test")}}],
        "usage": {"input_tokens": tokens * 2, "output_tokens": tokens,
                  "cache_creation_input_tokens": 100,
                  "cache_read_input_tokens": 50}}})

    is_router = "bmad-help" in prompt and "Fuehre ihn NICHT aus" in prompt
    is_solo = "AUFTRAG" in prompt and "bmad" not in prompt.lower()

    chain = [s.strip() for s in os.environ.get("FAKE_CHAIN", "").split(",") if s.strip()]
    chain = chain or DEFAULT_CHAIN

    if is_router:
        done_marker = Path(".fake_chain_pos")
        pos = int(done_marker.read_text().strip()) if done_marker.exists() else 0
        if pos >= len(chain):
            result = json.dumps({"status": "complete", "done": True,
                                 "next_skill": None, "reason": "alle Pflichtschritte erledigt"})
        else:
            result = json.dumps({"status": "complete", "done": False,
                                 "next_skill": chain[pos], "action": None,
                                 "phase": "auto", "required": True,
                                 "reason": "naechster Pflichtschritt"})
            done_marker.write_text(str(pos + 1))
    elif n == env_int("FAKE_BLOCK_AT", -1):
        result = json.dumps({"status": "blocked",
                             "reason": "Simulierter Blocker fuer den Selbsttest"})
    elif is_solo:
        solo_done_at = env_int("FAKE_SOLO_DONE_AT", 3)
        Path(f"solo-output-{n:03d}.txt").write_text(f"Solo-Schritt {n}\n", encoding="utf-8")
        fertig = "JA" if n >= solo_done_at else "NEIN"
        result = f"Arbeitsschritt {n} ausgefuehrt.\nFERTIG: {fertig}"
    else:
        # Normaler BMAD-Schritt: erzeugt ein Artefakt, damit der Git-Commit
        # etwas zu committen hat.
        out = Path("_bmad-output/planning-artifacts")
        out.mkdir(parents=True, exist_ok=True)
        (out / f"artefakt-{n:03d}.md").write_text(
            f"# Artefakt aus Aufruf {n}\n\nPrompt-Auszug: {prompt[:120]}\n",
            encoding="utf-8")
        result = json.dumps({
            "status": "complete",
            "assumptions": [f"Annahme aus Aufruf {n}"],
            "open_questions": [] if n % 3 else [f"Offene Frage aus Aufruf {n}"],
            "artifact": str(out / f"artefakt-{n:03d}.md")})

    # modelUsage bildet Haupt- und Subagentenverbrauch ab; der Harness nimmt
    # die groessere der beiden Summen.
    emit({"type": "result", "subtype": "success", "is_error": False,
          "result": result,
          "duration_ms": int(delay * 1000),
          "num_turns": 2,
          "total_cost_usd": round(tokens * 0.00002, 6),
          "modelUsage": {
              "test-model": {
                  "inputTokens": tokens * 2, "outputTokens": tokens,
                  "cacheCreationInputTokens": 100, "cacheReadInputTokens": 50,
                  "costUSD": round(tokens * 0.00002, 6),
                  "canonicalModel": "test-model"}},
          "usage": {"input_tokens": tokens * 2, "output_tokens": tokens,
                    "cache_creation_input_tokens": 100,
                    "cache_read_input_tokens": 50}})
    return 0


if __name__ == "__main__":
    sys.exit(main())
