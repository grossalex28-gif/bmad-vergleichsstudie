# Harness für die automatisierten KI-Läufe

Version `0.1.0-probe`. Setzt H1–H7 aus dem Analysekonzept und das Zeitregime
T1–T3 in ausführbaren Code um. Nur Python-Standardbibliothek, keine
Abhängigkeiten.

**Zum Testen: `TESTANLEITUNG.md`.** Erst der Selbsttest ohne Tokenverbrauch,
dann der Probelauf gegen die echte CLI.

## Ordnerinhalt

```
bmad-vergleichsstudie/
├── harness.py            Der Harness. init / run / status / stop / reopen
├── selftest.py           69 Prüfungen gegen eine CLI-Attrappe, ohne Tokenverbrauch
├── tools/fake_claude.py  Die Attrappe
├── config.probe.json     Konfiguration für den Probelauf
├── inputs/probe.md       Auftrag für das Wegwerf-Miniprojekt
├── seed-probe/           Leeres Projektgerüst für den Probelauf
├── audit.py              Hook-Prüfung, Netzwerkklassifikation, Hashes
├── prompts.py            Die exakten Texte an beide Bedingungen — der Aufrufrahmen
├── derive_catalog.py     Einmalige Katalogableitung, außerhalb der Messläufe
├── TESTANLEITUNG.md      Ablauf beider Teststufen
├── docs/                 Die Festlegungs- und Befunddokumente des Projekts
└── runs/                 Entsteht automatisch
```

## Begriffe

Die beiden untersuchten Konfigurationen heißen durchgängig **Bedingungen**
(`bmad`, `solo`), nicht „Arme". Der Begriff *Arm* stammt aus der klinischen
Studienmethodik und setzt dort Teilnehmer und Randomisierung voraus — beides
gibt es hier nicht. In der empirischen Softwaretechnik ist *Bedingung*
beziehungsweise *Behandlung* der zutreffende Begriff (Wohlin et al.,
*Experimentation in Software Engineering*). Im Code heißt das Feld
`condition`; alte Zustandsdateien mit dem Feld `arm` werden beim Laden
migriert.

Noch zu ergänzen: `bmad-core-bmm/` — eine BMAD-Neuinstallation mit
ausschließlich `core` und `bmm`. Siehe Testanleitung.

## Was das Skript umsetzt

| Regel | Umsetzung |
|---|---|
| **H1** isolierter Laufkontext | `init` kopiert das Seed-Repo nach `runs/<Projekt>/<Bedingung>/<Nr>/code`, legt ein frisches Git-Repo an und verweigert die Arbeit in einem nicht-leeren Verzeichnis |
| **H2** eingefrorene Eingabe | SHA-256 der Eingabedatei wird bei `init` in `config.json` festgehalten und vor **jedem** `run` gegengeprüft; Abweichung → Abbruch |
| **H3** nicht-interaktiver Betrieb | `claude -p --output-format stream-json --verbose --model … --permission-mode …` |
| **H4** Antwortautomat | Headless-Präfix vor jedem Prompt; ausgewertet wird der JSON-Status, nicht ein Textmuster |
| **H5** Phasensteuerung | Nach jedem Schritt `git commit` und `git tag step-NNN-<skill>` |
| **H6** Instrumentierung | JSONL je Schritt unter `logs/`, `protokoll.csv`, `config.json`, Tokenzählung getrennt nach Input, Output, Cache-Creation und Cache-Read, dazu `costUSD` und der je Schritt tatsächlich verwendete Modellbezeichner |
| **H7** Blockerbehandlung | `status: blocked` oder Fehler hält den Lauf an; Terminierungsgrund wird protokolliert |
| **T1** Aktivzeit, 8 h | Summe der Schrittdauern. Wartezeiten zählen **nicht** mit |
| **T2** Kalenderdeckel, 36 h | Ab dem ersten `run` |
| **T3** Schrittdeckel, 180 min | Wachhund bricht auch einen Prozess ab, der gar nichts ausgibt |

Zusätzlich abgesichert:

- **Hooks.** `init` erfasst die wirksame Claude-Code-Konfiguration mit Inhalt und Hash und **verweigert den Lauf**, solange Hooks aktiv sind. Hooks feuern bei jedem Sessionstart, und ein `Stop`-Hook kann eine beendete Session automatisch fortsetzen — beides würde die Messung unbemerkt verändern. Mit `"require_no_hooks": false` laufen sie bewusst mit und werden dokumentiert.
- **Modelldrift.** Weicht das je Schritt tatsächlich verwendete Modell vom konfigurierten ab, wird das als `model_mismatch` protokolliert. Bei einem Alias wie `claude-sonnet-5`, hinter dem der Anbieter jederzeit umhängen kann, ist das der einzige Weg, einen Wechsel mitten in der Erhebung zu bemerken.
- **Subagenten.** Neben den `assistant`-Ereignissen wird das `modelUsage`-Objekt ausgewertet; die größere der beiden Summen zählt. Damit sollten die Subagenten von `bmad-create-story` und `bmad-code-review` erfasst sein — im Probelauf zu verifizieren.

## Vier Entwurfsentscheidungen

**Kontext.** Jeder Schritt ist eine frische Session ohne `--resume`. Der Prompt
sagt ausdrücklich, dass der Kontext leer ist und die benötigten Artefakte selbst
einzulesen sind. Damit ist nachweisbar, dass die BMAD-Artefakte tatsächlich
genutzt werden — ein Schritt kann nur wissen, was er aus den Dateien liest.

**Sequenz (Variante B).** Vor jedem Schritt fragt der Harness `bmad-help`,
welcher Skill als Nächstes dran ist, und führt genau diesen aus. Die Reihenfolge
stammt damit vom Framework, nicht vom Forscher — das ist die Umsetzung von R2
und R3. Liefert der Router keine Empfehlung, greift `fallback_chain` aus der
Konfiguration. Mit `"routing_mode": "fixed"` lässt sich auf die feste Pflichtkette
umschalten.

**Pausieren und Fortsetzen.** `stop` legt eine Markierungsdatei an, alternativ
`Strg-C`. Beides beendet nicht den laufenden Prompt, sondern setzt ein Flag, das
*zwischen* zwei Schritten geprüft wird. `state.json` wird nach jedem Schritt
atomar geschrieben. Fortsetzen ist derselbe `run`-Befehl mit derselben Lauf-ID.

**Rate-Limit.** Wird an Ausgabe und Exit-Code erkannt. Der Harness wartet,
protokolliert Beginn und Ende der Pause getrennt und rechnet sie nicht auf T1 an.
Sonst würde die Auslastung des Abos als Entwicklungsaufwand gemessen und C1c
wäre nicht auswertbar.

## Datenablage je Lauf

```
runs/<Projekt>/<Bedingung>/<Nr>/
├── config.json     Modell, Berechtigungen, Input-Hash, Seed-Commit, Limits, BMAD-Manifest
├── state.json      Fortschritt, Zeiten, Tokens, alle abgeschlossenen Schritte
├── logs/
│   ├── protokoll.csv          eine Zeile je Schritt und je Pause
│   └── step-NNN-<skill>.jsonl vollständiger JSON-Stream
├── code/           Arbeitsverzeichnis, Git-Repo mit Tag je Schritt
├── artifacts/
└── metrics/
```

`state.json` hält je Schritt zusätzlich `assumptions[]` und `open_questions[]`
aus dem JSON-Status — die Rohdaten für FF3 und das Defizitregister C9.

## Bekannte Provisorien

- Die Feldnamen der Stream-JSON-Ausgabe können sich zwischen CLI-Versionen
  ändern. Der Parser ist defensiv geschrieben, aber die Tokenzahlen sind nach
  dem Probelauf gegen einen bekannten Wert zu prüfen.
- Tokens von Subagenten (`bmad-create-story` und `bmad-code-review` arbeiten mit
  parallelen Subagenten) erscheinen möglicherweise nicht in der Hauptzählung.
  Das ist im Probelauf zu verifizieren, sonst wird C8 systematisch und
  einseitig unterschätzt.
- Die Terminierungsregel der Solo-Bedingung hängt an der Zeile `FERTIG: JA`. Das ist
  eine Prozessanweisung und im Methodikkapitel offenzulegen.
- `bmad-loop` ist bewusst nicht eingebunden; die Steuerung liegt vollständig im
  Harness, damit beide Bedingungen identisch behandelt werden.
- `token_budget` und `solo_max_iterations` sind inzwischen aus den
  Probelaufdaten kalibriert und stehen in `config.messlauf.json`, samt
  Begründung in den `_hinweis`-Feldern derselben Datei.


## Messläufe: was gegenüber dem Probelauf dazugekommen ist

Der Probelauf arbeitete dateibasiert und mit einem einzigen Seed. Die zwölf
Messläufe brauchen beides anders, weshalb `config.messlauf.json` drei
Abschnitte trägt, die `config.probe.json` nicht kennt.

**`seed_repos`** statt `seed_repo`. Je Projekt ein eigenes Seed-Repository,
weil Projekt A und Projekt B zwei verschiedene Anwendungen sind. Das alte
Einzelfeld bleibt gültig, damit die Probelauf-Konfiguration unverändert
lauffähig ist.

**`freeze`** je Projekt. Vor jedem `init` prüft der Harness den HEAD des
Seed-Repositorys und den Hash der Anfangsdatenbestand-Datei gegen die
eingefrorenen Sollwerte aus `seed/{A,B}/seed.meta.json` und bricht bei
Abweichung ab. Ohne diese Prüfung fiele ein nachträglich verändertes
Seed-Repository erst bei der Auswertung auf, also zu spät. Der Hash der
Eingabedatei wurde schon vorher geprüft und bleibt unverändert in `config.json`
je Lauf.

**`database`**. Der Harness legt beim `init` eine leere Datenbank je Lauf an
(`<projekt>_<bedingung>_<wiederholung>`, etwa `a_bmad_1`) und reicht die
Verbindungszeichenfolge beim `run` über die Umgebungsvariable
`ConnectionStrings__Default` an die CLI weiter. Das Schema entsteht durch die
Migrationen, die der jeweilige Arm selbst schreibt, und bleibt damit Teil
dessen, was gemessen wird. Das Kennwort steht nicht in der Konfiguration,
sondern in der Umgebungsvariablen aus `database.password_env`, damit
`config.messlauf.json` unverändert ins Replikationspaket kann. In der `config.json`
je Lauf steht die Verbindungszeichenfolge nur mit geschwärztem Kennwort.

Erreichbarkeit vorab prüfen, bevor das erste Laufverzeichnis entsteht:

```bash
export MSSQL_SA_PASSWORD='<lokales_passwort>'
python3 harness.py dbcheck --config config.messlauf.json
```

`dbcheck` fragt die Version der Instanz ab, legt eine Wegwerf-Datenbank an,
verwirft sie wieder und listet die zwölf geplanten Datenbanknamen auf. Ein
fehlendes Kennwort oder ein gestoppter Container fällt damit auf, bevor ein
Lauf angelegt ist.
