"""
Die exakten Texte, die an beide Bedingungen gehen.

Bewusst in einer eigenen Datei: Dies ist der "Aufrufrahmen" im Sinne des
Analysekonzepts - der einzige strukturelle Unterschied zwischen den Bedingungen und
damit der Teil des Harness, der im Methodikkapitel und im Anhang wiedergegeben
werden muss. Getrennt vom Steuerungscode ist er nachvollziehbar und zitierbar.

Grundsaetze:
- Der Auftragstext (die eingefrorene Eingabedatei) geht byteidentisch in beide
  Bedingungen. Unterschiedlich ist nur der Rahmen darum.
- Keine Prozessanweisung, kein Qualitaetsappell, keine Vorgabe zu Struktur
  oder Schnittstelle.
- Das Headless-Praefix ist keine Ausnahme davon: BMAD bringt denselben
  Mechanismus in seinen Skills selbst mit; die Solo-Bedingung erhaelt lediglich die
  Feststellung, dass niemand antworten kann.
"""

from __future__ import annotations

from typing import Optional

HEADLESS_PREFIX = (
    "headless: true\n"
    "Dies ist ein nicht-interaktiver Lauf. Es gibt keinen Benutzer, der antworten "
    "kann. Stelle keine Rueckfragen. Wenn eine Angabe fehlt, leite sie ab und "
    "protokolliere sie als Annahme. Beende deine Antwort mit dem vorgesehenen "
    "JSON-Status.\n\n"
)

ROUTER_PROMPT = (
    HEADLESS_PREFIX
    + "Verwende den Skill bmad-help. Ermittle anhand des Katalogs und der bereits "
    "vorhandenen Artefakte im Projektverzeichnis, welcher Skill als Naechstes "
    "auszufuehren ist. Fuehre ihn NICHT aus.\n\n"
    "Antworte ausschliesslich mit einem JSON-Objekt in genau dieser Form:\n"
    '{"status":"complete","next_skill":"<skill-name>","action":"<action oder null>",'
    '"phase":"<phase>","required":true|false,"done":true|false,'
    '"reason":"<ein Satz>"}\n\n'
    "Setze \"done\": true genau dann, wenn kein weiterer Pflichtschritt aussteht "
    "und alle Stories des Sprint-Plans den Status abgeschlossen tragen."
)


def bmad_first_prompt(input_text: str) -> str:
    """Eintrittspunkt BMAD-Bedingung: Aufrufrahmen so knapp wie moeglich (H2)."""
    return (
        HEADLESS_PREFIX
        + "Verwende den Skill bmad-help, um den ersten auszufuehrenden Schritt zu "
        "bestimmen, und fuehre ihn anschliessend mit dem folgenden Auftrag als "
        "Eingangsinformation aus.\n\n"
        "--- AUFTRAG ---\n" + input_text + "\n--- ENDE AUFTRAG ---"
    )


def bmad_step_prompt(skill: str, action: Optional[str], input_text: str) -> str:
    act = f" mit der Aktion '{action}'" if action else ""
    return (
        HEADLESS_PREFIX
        + f"Fuehre den Skill {skill}{act} aus.\n\n"
        "Der urspruengliche Auftrag und alle bisher erzeugten Artefakte liegen im "
        "Projektverzeichnis. Lies die fuer diesen Schritt benoetigten Artefakte "
        "selbst ein; dieser Kontext ist leer.\n\n"
        "--- AUFTRAG ---\n" + input_text + "\n--- ENDE AUFTRAG ---"
    )


def solo_first_prompt(input_text: str) -> str:
    """Eintrittspunkt Solo-Bedingung: derselbe Auftrag, kein Framework, keine
    Prozessanweisung und kein Qualitaetsappell."""
    return (
        "headless: true\n"
        "Dies ist ein nicht-interaktiver Lauf. Es gibt keinen Benutzer, der "
        "antworten kann.\n\n"
        "--- AUFTRAG ---\n" + input_text + "\n--- ENDE AUFTRAG ---"
    )


def solo_continue_prompt(input_text: str) -> str:
    return (
        "headless: true\n"
        "Dies ist ein nicht-interaktiver Lauf. Es gibt keinen Benutzer, der "
        "antworten kann.\n\n"
        "Im Projektverzeichnis liegt der bisherige Stand. Setze die Arbeit an dem "
        "folgenden Auftrag fort.\n\n"
        "--- AUFTRAG ---\n" + input_text + "\n--- ENDE AUFTRAG ---\n\n"
        "Beende deine Antwort mit genau einer Zeile:\n"
        'FERTIG: JA   oder   FERTIG: NEIN'
    )


