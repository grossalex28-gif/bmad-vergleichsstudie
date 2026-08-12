#!/usr/bin/env python3
"""
Katalogableitung aus einem Referenz-Repository - EINMALIG, VOR DER ERHEBUNG.

Dies ist KEIN Studienarm und kein Teil der zwoelf Messlaeufe. Es ist ein
Werkzeugschritt, der einen Katalogentwurf erzeugt, den du anschliessend
pruefst, kuerzt und einfrierst. Danach geht genau dieser eine, byteidentische
Katalog in alle Laeufe.

Warum getrennt: In den Messlaeufen darf das Original nicht auftauchen. FF4
vergleicht die KI-Implementierungen mit dem Open-Source-Original; haette die
KI das Original gelesen, waere dieser Vergleich wertlos. Hier ist das
unproblematisch, weil der Schritt vor der Erhebung liegt und sein Ergebnis
alle Bedingungen identisch trifft.

Aufruf:
    python3 derive_catalog.py --repo https://github.com/adamlarner/angularbooking \\
                              --id A --bmad ./bmad-core-bmm --model claude-sonnet-5

Ergebnis:
    catalogs/A/
      katalog-entwurf.md     <- der Entwurf, von dir zu pruefen
      quelle.json            <- Repo-URL, Commit, Modell, Datum, BMAD-Stand
      lauf.jsonl             <- vollstaendiger Stream, fuer das Replikationspaket
      pruefliste.md          <- was du vor dem Einfrieren abarbeiten musst
"""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import subprocess
import sys
import time
from pathlib import Path

PROMPT = """headless: true
Dies ist ein nicht-interaktiver Lauf. Es gibt keinen Benutzer, der antworten
kann. Stelle keine Rueckfragen.

Im Unterverzeichnis `repo/` liegt ein bestehendes Open-Source-Projekt.

Analysiere es mit dem Skill bmad-document-project und leite daraus einen
**Feature-Katalog** ab: eine nummerierte Liste fachlicher Anforderungen, die
beschreiben, WAS die Anwendung fuer ihre Nutzer leistet.

Regeln fuer den Katalog:

1. Jedes Feature ist ein vollstaendiger vertikaler Schnitt - es geht durch
   Oberflaeche, Schnittstelle, Fachlogik und Persistenz. Keine technischen
   Teilschritte als eigene Features.
2. Jedes Feature ist in einem Satz aus Nutzersicht formuliert, in der
   Gegenwartsform, ohne Konjunktiv.
3. **Keine Implementierungsdetails.** Nenne keine Endpunktpfade, keine
   Klassen-, Datei- oder Ordnernamen, keine Frameworks, keine Datenbank- oder
   Schemadetails, keine HTTP-Methoden oder Statuscodes. Wer den Katalog liest,
   soll nicht rekonstruieren koennen, wie das Original gebaut ist.
4. **Keine Eigennamen.** Kein Projektname, kein Repository-Name, keine
   charakteristischen Bezeichner aus dem Original.
5. Mindestens ein Feature muss einen fachlichen Fehler- oder Konfliktfall
   beschreiben (z. B. eine abgelehnte Aktion wegen zwischenzeitlicher
   Zustandsaenderung).
6. Nenne getrennt davon, was du bewusst **nicht** aufgenommen hast, jeweils mit
   einem Satz Begruendung. Ausschlusskandidaten sind: Authentifizierung und
   Benutzerverwaltung, E-Mail- oder Benachrichtigungsversand, Datei-Upload,
   Mehrsprachigkeit, Administrationsoberflaechen, Echtzeitfunktionen und
   jeder zweite eigenstaendige Fachbereich.

Schreibe das Ergebnis nach `katalog-entwurf.md` mit dieser Struktur:

    # Feature-Katalog (Entwurf)
    ## Projektkontext
    (zwei bis drei Absaetze, was die Anwendung fachlich leistet, anonymisiert)
    ## Katalog
    | ID | Feature |
    ## Bewusst ausgeschlossen
    | Ausgeschlossen | Begruendung |
    ## Beobachtungen
    (was beim Ableiten unklar oder mehrdeutig war)

Beende deine Antwort mit einem JSON-Objekt:
{"status":"complete","feature_count":<n>,"excluded_count":<n>,
 "assumptions":[...],"open_questions":[...]}
"""

CHECKLIST = """# Prueiliste vor dem Einfrieren

Der Entwurf in `katalog-entwurf.md` ist ein Vorschlag, keine Festlegung. Erst
nach dieser Pruefung wird er zum Katalog.

## 1. Anonymisierung

- [ ] Kein Projekt- oder Repository-Name im Text
- [ ] Keine charakteristischen Eigennamen aus dem Original
- [ ] Kein Endpunktpfad, keine HTTP-Methode, kein Statuscode
- [ ] Kein Klassen-, Datei- oder Ordnername
- [ ] Kein Framework, keine Bibliothek, kein Datenbankschema

Diese Punkte sind kein Formalismus: Alles davon waere ein vorweggenommener
Entwurf. Der Schnittstellenentwurf ist ausdruecklich Untersuchungsgegenstand
und darf nicht in die Eingabe geraten (Analysekonzept, Abschnitt 9).

## 2. Zuschnitt

- [ ] Jedes Feature geht vollstaendig durch alle Schichten
- [ ] Kein Feature beschreibt einen technischen Teilschritt
- [ ] Mindestens ein Fehler- oder Konfliktfall enthalten
- [ ] Mindestens eine nichttriviale Datenstruktur gefordert
- [ ] Mindestens eine berechnende Fachregel enthalten

## 3. Umfang

- [ ] Featurezahl passt zum Zeitbudget (Richtwert: rund 16 Minuten und 6 USD
      je Story in der BMAD-Bedingung, etwa 1,2 Stories je Feature)
- [ ] Ausschlussliste vollstaendig und je Eintrag begruendet

## 4. Abgleich

- [ ] Entwurf gegen den bestehenden Katalog in
      `claude/Feature-Kataloge_Entwurf.md` gehalten
- [ ] Abweichungen notiert - sie sind selbst ein Befund ueber BMADs
      Anforderungsableitung und gehoeren ins Methodikkapitel
- [ ] Endgueltige Fassung ist deine Entscheidung, nicht die des Werkzeugs

## 5. Einfrieren

- [ ] Katalog in die Eingabedatei nach `claude/Eingabespezifikation.md` uebernommen
- [ ] SHA-256 der Eingabedatei in der Laufkonfiguration hinterlegt
- [ ] Ab hier keine Aenderung mehr: die Nummerierung ist Bezugsgroesse fuer
      A2, B1, B1b, B6 und B11
"""


def sh(*args, cwd=None, check=True):
    return subprocess.run(args, cwd=cwd, check=check, capture_output=True, text=True)


def main() -> int:
    ap = argparse.ArgumentParser(description="Katalogentwurf aus einem Repository ableiten")
    ap.add_argument("--repo", required=True, help="Repository-URL")
    ap.add_argument("--id", required=True, help="Projektkennung, z.B. A")
    ap.add_argument("--bmad", required=True, type=Path,
                    help="BMAD-Installation (core + bmm)")
    ap.add_argument("--model", required=True)
    ap.add_argument("--claude-bin", default="claude")
    ap.add_argument("--out", type=Path, default=Path("catalogs"))
    ap.add_argument("--force", action="store_true",
                    help="vorhandenes Zielverzeichnis loeschen und neu beginnen")
    args = ap.parse_args()

    out = (args.out / args.id).resolve()
    if out.exists() and any(out.iterdir()):
        if not args.force:
            return err(f"{out} existiert bereits und ist nicht leer.\n"
                       f"Mit --force wird es geloescht und neu begonnen.\n"
                       f"Achtung: ein abgebrochener Lauf laesst dort den Klon des "
                       f"Originals zurueck - der darf in keinen Messlauf geraten.")
        print(f"[0/4] Loesche {out} …")
        shutil.rmtree(out)
    out.mkdir(parents=True, exist_ok=True)

    print(f"[1/4] Klone {args.repo} …")
    repo = out / "repo"
    sh("git", "clone", "--depth", "1", args.repo, str(repo))
    commit = sh("git", "rev-parse", "HEAD", cwd=repo).stdout.strip()
    shutil.rmtree(repo / ".git", ignore_errors=True)

    print("[2/4] Spiele BMAD ein …")
    for item in ("_bmad", ".claude"):
        src = args.bmad / item
        if not src.exists():
            return err(f"{src} fehlt.")
        shutil.copytree(src, out / item, dirs_exist_ok=True)

    print("[3/4] Leite den Katalog ab (das dauert einige Minuten) …")
    log = out / "lauf.jsonl"
    cmd = [args.claude_bin, "-p", PROMPT, "--output-format", "stream-json",
           "--verbose", "--model", args.model,
           "--permission-mode", "bypassPermissions"]
    t0 = time.time()
    usage, cost, result = {}, 0.0, ""
    with log.open("w", encoding="utf-8") as fh:
        proc = subprocess.Popen(cmd, cwd=out, stdout=subprocess.PIPE,
                                stderr=subprocess.STDOUT, text=True, bufsize=1)
        for line in proc.stdout:                            # type: ignore[union-attr]
            fh.write(line)
            try:
                ev = json.loads(line)
            except json.JSONDecodeError:
                continue
            if ev.get("type") == "assistant":
                for c in (ev.get("message") or {}).get("content") or []:
                    if isinstance(c, dict) and c.get("type") == "tool_use":
                        print(f"      [{c.get('name')}]", flush=True)
            elif ev.get("type") == "result":
                result = ev.get("result") or result
                cost = float(ev.get("total_cost_usd") or cost)
                usage = ev.get("usage") or usage
        proc.wait()
    dur = time.time() - t0

    print("[4/4] Sichere Herkunftsangaben …")
    manifest = args.bmad / "_bmad" / "_config" / "manifest.yaml"
    draft = out / "katalog-entwurf.md"
    (out / "pruefliste.md").write_text(CHECKLIST, encoding="utf-8")
    (out / "quelle.json").write_text(json.dumps({
        "projekt": args.id,
        "repository": args.repo,
        "commit": commit,
        "modell": args.model,
        "abgeleitet_am": time.strftime("%Y-%m-%dT%H:%M:%S%z"),
        "dauer_min": round(dur / 60, 1),
        "kosten_usd": round(cost, 4),
        "usage": usage,
        "bmad_manifest_sha256": hashlib.sha256(manifest.read_bytes()).hexdigest()
        if manifest.exists() else None,
        "hinweis": "Einmaliger Vorbereitungsschritt ausserhalb der Messlaeufe. "
                   "Das Ergebnis ist ein Entwurf und vor dem Einfrieren manuell "
                   "zu pruefen (siehe pruefliste.md).",
    }, indent=2, ensure_ascii=False), encoding="utf-8")

    # Das geklonte Original wird entfernt: es darf in keinem spaeteren Schritt
    # versehentlich als Kontext auftauchen.
    shutil.rmtree(repo, ignore_errors=True)
    for item in ("_bmad", ".claude"):
        shutil.rmtree(out / item, ignore_errors=True)

    print(f"\nFertig in {dur/60:.1f} min, {cost:.2f} USD.")
    if draft.exists():
        print(f"Entwurf:    {draft}")
    else:
        print("WARNUNG: katalog-entwurf.md wurde nicht geschrieben. "
              f"Antwort steht in {log}.")
        print(result[:600])
    print(f"Pruefliste: {out / 'pruefliste.md'}")
    print("Das geklonte Repository wurde geloescht - es darf in keinen Messlauf geraten.")
    return 0


def err(msg: str) -> int:
    sys.stderr.write(msg + "\n")
    return 1


if __name__ == "__main__":
    sys.exit(main())
