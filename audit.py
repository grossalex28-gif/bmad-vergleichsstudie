"""
Kontrollen der Laufumgebung.

Zwei Dinge, die keine Messgroesse der Studie sind, aber jede Messung
verfaelschen koennen, wenn sie unbemerkt bleiben:

- **Hooks.** Sie feuern bei jedem Sessionstart. Der Harness startet je Schritt
  eine neue Session, also feuern sie hunderte Male je Lauf; ein Stop-Hook kann
  eine beendete Session sogar automatisch fortsetzen und damit die
  Schrittgrenzen aushebeln. Hooks sind eine Behandlungsvariable und gehoeren
  ins Berechtigungsprofil nach H6.
- **Netzwerkzugriffe.** Bash laesst sich nicht sperren, weil npm install und
  dotnet restore es brauchen. Statt zu verhindern wird gemessen: Holt ein Lauf
  fremde Inhalte, ist das ein Befund fuer die Datenleckage-Diskussion und fuer
  FF4.
"""

from __future__ import annotations

import hashlib
import json
import re
from pathlib import Path
from typing import Optional


def sha256_file(p: Path) -> str:
    h = hashlib.sha256()
    with p.open("rb") as fh:
        for chunk in iter(lambda: fh.read(1 << 16), b""):
            h.update(chunk)
    return h.hexdigest()



# --- Netzwerkpruefung -------------------------------------------------------
# Bash laesst sich nicht sperren: npm install und dotnet restore brauchen es.
# Statt zu verhindern wird gemessen. Zwei Klassen:
#   FETCH   - holt fremde Inhalte (curl, wget, git clone, Repository-URLs).
#             Jeder Treffer wird als Befund protokolliert.
#   PACKAGE - Paketmanager. Erwartet, wird nur gezaehlt.
_NET_FETCH = re.compile(
    r"\b(curl|wget|lynx|w3m|aria2c|scp|sftp|rsync\s+[^|]*::)\b"
    r"|\bgit\s+(clone|fetch|pull|ls-remote|archive\s+--remote)\b"
    r"|\bgh\s+(repo|api|release)\b"
    r"|https?://(?!localhost|127\.0\.0\.1)",
    re.I)
_NET_PACKAGE = re.compile(
    r"\b(npm|pnpm|yarn|npx|dotnet|nuget|pip|pip3|uv)\b\s*"
    r"(i\b|install|restore|add|ci|run|exec|tool|--version)?",
    re.I)


# Aufrufe gegen die eigene laufende Anwendung sind Selbsttests und kein
# Datenabfluss. Sie werden als "local" gezaehlt, damit C9 keinen externen
# Abruf behauptet, den es nicht gab.
_NET_URL = re.compile(r"https?://([^/\s'\"]+)", re.I)
_LOCAL_HOSTS = re.compile(
    r"^(localhost|127\.0\.0\.1|0\.0\.0\.0|\[::1\]|host\.docker\.internal)"
    r"(:\d+)?$", re.I)


def _only_local_urls(cmd: str) -> bool:
    urls = _NET_URL.findall(cmd)
    return bool(urls) and all(_LOCAL_HOSTS.match(u) for u in urls)


def classify_command(cmd: str) -> Optional[str]:
    if not cmd:
        return None
    if _only_local_urls(cmd) and not _NET_PACKAGE.match(cmd.strip()):
        return "local"
    if _NET_FETCH.search(cmd):
        # Paketmanager-Aufrufe mit eingebetteter URL bleiben Paketmanager.
        if _NET_PACKAGE.match(cmd.strip()):
            return "package"
        return "fetch"
    if _NET_PACKAGE.match(cmd.strip()):
        return "package"
    return None



def inspect_settings(cfg: Config, workdir: Optional[Path] = None) -> dict:
    """Erfasst die wirksame Claude-Code-Konfiguration.

    Hooks feuern bei jedem Sessionstart. Der Harness startet je Schritt eine
    neue Session, also feuern sie ueber einen Lauf hinweg hunderte Male. Ein
    Stop-Hook kann eine Session sogar automatisch fortsetzen und damit die
    Schrittgrenzen des Harness aushebeln. Das ist eine Behandlungsvariable und
    gehoert ins Berechtigungsprofil nach H6.
    """
    found = []
    paths = list(cfg.settings_files)
    if workdir:
        paths += [workdir / ".claude" / "settings.json",
                  workdir / ".claude" / "settings.local.json"]
    for p in paths:
        if not p.exists():
            continue
        try:
            data = json.loads(p.read_text(encoding="utf-8"))
        except (OSError, json.JSONDecodeError) as exc:
            found.append({"path": str(p), "error": str(exc)})
            continue
        found.append({
            "path": str(p),
            "sha256": sha256_file(p),
            "hooks": data.get("hooks") or {},
            "hook_events": sorted((data.get("hooks") or {}).keys()),
            "content": data,
        })
    active = sorted({e for f in found for e in f.get("hook_events", [])})
    return {"files": found, "active_hook_events": active}



def hook_guard(cfg: Config, settings: dict) -> None:
    if not settings["active_hook_events"]:
        return
    msg = (
        "Es sind Claude-Code-Hooks aktiv: "
        + ", ".join(settings["active_hook_events"])
        + "\nQuellen:\n  "
        + "\n  ".join(f["path"] for f in settings["files"])
        + "\n\nHooks feuern bei jedem Sessionstart. Der Harness startet je "
          "Schritt eine neue Session, also feuern sie hunderte Male je Lauf. "
          "Ein Stop-Hook kann eine Session automatisch fortsetzen und damit die "
          "Schrittgrenzen des Harness aushebeln.\n"
          "Entweder die Hooks fuer die Messlaeufe abschalten, oder in der "
          "Konfiguration \"require_no_hooks\": false setzen - dann laufen sie mit "
          "und werden in config.json dokumentiert."
    )
    if cfg.require_no_hooks:
        raise SystemExit("[init] Abbruch.\n" + msg)
    print("[init] WARNUNG.\n" + msg)


