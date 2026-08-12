# Testanleitung

Zwei Stufen. Die erste kostet nichts und dauert eine Minute; die zweite verbraucht
echte Tokens. Erst wenn Stufe 1 vollständig grün ist, lohnt Stufe 2.

---

## Stufe 1 — Selbsttest ohne Tokenverbrauch

Der Selbsttest ersetzt die Claude-Code-CLI durch eine Attrappe
(`tools/fake_claude.py`), die dasselbe JSON-Stream-Format ausgibt. Kein
Netzwerkzugriff, kein Tokenverbrauch. Er prüft die gesamte Steuerungslogik.

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
python3 selftest.py
```

Erwartete Ausgabe am Ende:

```
bestanden: 69   fehlgeschlagen: 0
```

Voraussetzungen: Python 3.10 oder neuer und `git`. Sonst nichts.

### Was dabei geprüft wird

| # | Szenario | Nachweis |
|---|---|---|
| 1 | BMAD-Bedingung läuft regulär durch | Seed kopiert, BMAD-Definitionen vorhanden, Input-Hash und Seed-Commit in `config.json`, Tokens gezählt, `assumptions[]` und `open_questions[]` übernommen, `protokoll.csv` und ein JSONL je Schritt geschrieben, ein Git-Tag je Schritt, Router arbeitet die Kette ab |
| 2 | H2 Hash-Abgleich | Eingabedatei wird nach `init` verändert → `run` bricht mit Verweis auf H2 ab |
| 3 | Stoppen und Fortsetzen | Stoppmarkierung wird *während* eines laufenden Schritts gesetzt → der Schritt läuft aus, der Lauf pausiert statt zu terminieren, `state.json` bleibt konsistent, Fortsetzung führt die Schrittzählung lückenlos weiter |
| 4 | Rate-Limit | 429 wird erkannt, Pause getrennt in `protokoll.csv` protokolliert, Wartezeit **nicht** in T1 enthalten, Lauf fährt danach fort |
| 5 | H7 Blocker | `status: blocked` terminiert den Lauf mit Grund `blocker` |
| 6 | T1 Aktivzeitdeckel | Deckel greift, Abbruch erfolgt an einer Schrittgrenze, nicht mitten im Prompt |
| 7 | T3 hängender Schritt | Ein Aufruf, der gar nichts ausgibt, wird vom Wachhund abgebrochen |
| 8 | Solo-Bedingung | Keine BMAD-Definitionen im Arbeitsverzeichnis, kein Router-Aufruf, Lauf endet bei `FERTIG: JA`, Iterationsobergrenze greift |
| 9 | Hook-Absicherung | Aktive Hooks verhindern den `init`, die Meldung nennt die betroffenen Ereignisse, es bleibt kein halb angelegtes Laufverzeichnis zurück; mit `require_no_hooks: false` läuft es mit Warnung durch und die Hooks landen in `config.json` |

### Einzelne Szenarien von Hand nachstellen

Die Attrappe lässt sich auch direkt steuern, wenn du etwas Bestimmtes sehen willst:

```bash
export FAKE_RATE_LIMIT_AT=3      # Aufruf 3 endet mit 429
export FAKE_BLOCK_AT=2           # Aufruf 2 meldet status blocked
export FAKE_SLEEP_AT=1           # Aufruf 1 hängt
export FAKE_STEP_SECONDS=5       # jeder Aufruf dauert 5 Sekunden
export FAKE_SOLO_DONE_AT=4       # Solo-Bedingung meldet ab Aufruf 4 fertig
```

Dann eine eigene Konfiguration mit `claude_bin` auf einen Wrapper um
`tools/fake_claude.py` zeigen lassen und `init` / `run` normal aufrufen.

### Wenn etwas fehlschlägt

Die Ausgabe nennt Szenario, Prüfpunkt und den beobachteten Wert. Die häufigste
Ursache ist eine zu alte Python-Version — `dataclasses` mit `X | None` verlangt
3.10. Prüfen mit `python3 -V`.

---

## Stufe 2 — Probelauf gegen die echte CLI

Erst durchführen, wenn Stufe 1 grün ist.

### Vorbereitung

**0. Hooks abschalten.** In `~/.claude/settings.json` sind vier `bmad-loop`-Hooks
eingetragen (`SessionStart`, `Stop`, `SessionEnd`, `PreCompact`). Sie feuern bei
**jedem** Sessionstart — der Harness startet je Schritt eine neue Session, also
mehrere hundert Mal über zwölf Läufe. Der `Stop`-Hook eines Loop-Moduls ist
zudem genau dafür gebaut, eine beendete Session automatisch fortzusetzen; er
würde die Schrittgrenzen des Harness aushebeln.

```bash
cp ~/.claude/settings.json ~/.claude/settings.json.bak-messlauf
python3 - <<'EOF'
import json, pathlib
p = pathlib.Path.home() / ".claude" / "settings.json"
d = json.loads(p.read_text())
d.pop("hooks", None)
p.write_text(json.dumps(d, indent=2))
print("Hooks entfernt. Sicherung: settings.json.bak-messlauf")
EOF
```

Der Harness prüft das selbst und **verweigert den `init`**, solange Hooks aktiv
sind. Willst du sie bewusst mitlaufen lassen, setze
`"require_no_hooks": false` — dann werden sie mit Inhalt und Hash in jede
`config.json` geschrieben und sind Teil des dokumentierten Berechtigungsprofils.

**1. BMAD neu installieren, nur `core` + `bmm`.**

```bash
mkdir -p ~/dev/Bachelor/bmad-vergleichsstudie/bmad-core-bmm
cd ~/dev/Bachelor/bmad-vergleichsstudie/bmad-core-bmm
npx bmad-method@latest install
```

Im Installer ausschließlich `core` und `bmm` auswählen, keine Erweiterungsmodule.
Danach prüfen:

```bash
grep -A1 "name:" _bmad/_config/manifest.yaml | grep -c "name:"   # erwartet: 2
ls .claude/skills | wc -l                                        # deutlich unter 89
```

Die Einstellungen für Sprache und Skill-Level müssen mit denen der späteren
Messläufe identisch sein — sie sind Behandlungsparameter.

**2. Modellbezeichner.** Bereits eingetragen: `claude-sonnet-5`, ermittelt am
07.08.2026. Die CLI liefert keine datierte Kennung, nur den kanonischen Alias —
deshalb trägt hier das Zugriffsdatum die Beweislast. Der Harness protokolliert
je Schritt das tatsächlich verwendete Modell und meldet jede Abweichung als
`model_mismatch` in `protokoll.csv`.

**3. Voraussetzungen prüfen.**

```bash
node -v && npm -v && uv --version && python3 -V && git --version
```

`uv` ist nicht optional: die BMAD-Skills rufen `memlog.py` und
`resolve_customization.py` über `uv run` auf. Fehlt es, entfallen Audit-Trail und
Customization-Auflösung stillschweigend.

### Durchführung

```bash
cd ~/dev/Bachelor/bmad-vergleichsstudie
python3 harness.py init --config config.probe.json --run P-BMAD-1
python3 harness.py run  --config config.probe.json --run P-BMAD-1
```

In einem zweiten Terminal jederzeit möglich:

```bash
python3 harness.py status --config config.probe.json --run P-BMAD-1
python3 harness.py stop   --config config.probe.json --run P-BMAD-1
```

Anschließend derselbe Auftrag in der Solo-Bedingung:

```bash
python3 harness.py init --config config.probe.json --run P-SOLO-1
python3 harness.py run  --config config.probe.json --run P-SOLO-1
```

### Sechs Fragen, die der Probelauf beantworten muss

**1. Wird `headless: true` erkannt?**

```bash
head -40 runs/P/bmad/01/logs/step-000-*.jsonl | grep -i -E "hallo|hi alex|guten"
```

Findet sich eine namentliche Begrüßung, greift der Headless-Modus **nicht**.
Dann muss der Übergabeweg geändert werden — vermutlich über
`_bmad/custom/<skill>.toml` und `workflow.activation_steps_prepend`, das laut
`references/headless.md` als Erkennungsmerkmal gilt.

**2. Kommt der JSON-Status zurück?**

```bash
cut -d, -f6,11 runs/P/bmad/01/logs/protokoll.csv
```

Die Spalte `status` muss `complete` oder `partial` zeigen. Steht dort
durchgängig `complete`, obwohl keine JSON-Antwort kam, greift der Rückfallwert —
erkennbar daran, dass `assumptions` in `state.json` leer bleiben.

**3. Laufen die Phase-4-Skills rückfragefrei?**

`bmad-create-story`, `bmad-dev-story` und `bmad-code-review` haben keine eigene
`headless.md`. Hier zeigt sich, ob das Präfix genügt. Ein Schritt, der nach
wenigen Sekunden mit einer Rückfrage endet, ist das Warnsignal.

**4. Schaltet `bmad-help` korrekt weiter?**

```bash
python3 -c "import json;s=json.load(open('runs/P/bmad/01/state.json'));print([x['skill'] for x in s['completed_steps']])"
```

Wiederholt sich derselbe Skill, erkennt der Router den Fortschritt nicht. Dann
muss der Routing-Prompt die bereits erledigten Schritte explizit mitgeben.

**5. Werden Subagenten-Tokens mitgezählt?**

Das ist der wichtigste Punkt. `bmad-create-story` und `bmad-code-review` starten
parallele Subagenten. Wenn deren Verbrauch nicht in der Hauptzählung auftaucht,
unterschätzt C8 systematisch — und zwar nur in der BMAD-Bedingung, was den Vergleich
verzerren würde.

Der Harness wertet inzwischen zusätzlich das `modelUsage`-Objekt der CLI aus und
nimmt die größere der beiden Summen; dort tauchen Subagenten auf. Zu prüfen ist,
ob das tatsächlich greift:

```bash
python3 -c "import json;s=json.load(open('runs/P/bmad/01/state.json'));print(s['tokens'], s['cost_usd'])"
grep -o '"modelUsage":{[^}]*}' runs/P/bmad/01/logs/step-*create-story*.jsonl | head
```

Gegenprüfen gegen `/cost` in einer interaktiven Sitzung mit demselben Auftrag
oder gegen das Nutzungs-Dashboard. Weicht es deutlich ab, muss der Parser vor
den Messläufen nachgebessert werden.

**Nebenbefund:** Die CLI liefert `costUSD` auch im Abo-Betrieb. Damit ist C8
nicht mehr nur rechnerisch aus Tokenzahlen und Listenpreis abzuleiten, sondern
kommt direkt aus dem Werkzeug — der Harness summiert es in `state.json` unter
`cost_usd` und schreibt es je Schritt in `protokoll.csv`. Das ist trotzdem eine
Schätzung des Anbieters und keine Abrechnung; im Methodikkapitel entsprechend zu
kennzeichnen.

**6. Wie sieht die Tokenkurve je Schritt aus?**

`protokoll.csv` ist die Kalibriergrundlage für das Tokenbudget und die
Iterationsobergrenze der Messläufe. Beide sind bisher offen.

### Bewusst provozieren

Damit die Mechanik einmal unter realen Bedingungen greift, ist `t3_step_minutes`
im Probelauf auf 45 gesetzt — knapp genug, dass ein längerer Implementierungs-
schritt anschlägt. Zusätzlich lohnt es, während eines laufenden Schritts einmal
`stop` aufzurufen und danach fortzusetzen: Das ist der Ablauf, den du später bei
jedem Rate-Limit brauchst.

---

## Danach

Der Probelauf ist ein Wegwerflauf. Sein Ergebnis sind nicht Artefakte, sondern
sechs beantwortete Fragen und zwei kalibrierte Zahlen. Danach werden
`config.messlauf.json` mit den endgültigen Werten angelegt, die Eingabedateien
für Projekt A und B eingefroren und das eigentliche Seed-Repository gebaut.
