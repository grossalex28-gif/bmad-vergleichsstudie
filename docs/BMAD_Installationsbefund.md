# Befund zur BMAD-Installation und Folgen für den Harness

*Auswertung von `ProjektGodSend/BMAD-Implementation/Implementation1` und des Vorstudien-Laufs `Projekt0/.../implementation1` · Stand 07.08.2026*

Dieses Dokument löst Abschnitt 1 („BMAD-Installation") aus `claude/Harness_Anforderungen.md` auf und trägt vier Befunde nach, die den geplanten Harness-Aufbau spürbar verändern.

---

## 1. Versions- und Modulstand (für `config.yaml`, H6)

**BMAD v6.10.0**, installiert am 07.08.2026, IDE-Ziel `claude-code`. Sieben Module, davon zwei eingebaut und fünf extern mit exaktem Commit-Stand:

| Modul | Version | Quelle | Commit / Paket |
|---|---|---|---|
| core | 6.10.0 | built-in | — |
| bmm | 6.10.0 | built-in | — |
| bmad-loop | v0.9.1 | extern | `67aa8028a22eb96962c7579a917b2fbea7f9475a` |
| tea | v1.21.6 | extern | `358f730b58298da7b985be00113e5813db623d8c` |
| bmb | v2.1.0 | extern | `d54981a8ba3b990148256455ccf4e6c1cb4918f0` |
| cis | v0.2.1 | extern | `07a8dd03d196a762f21c241d985293fe5b0f90e7` |
| wds | v0.4.3 | extern | `cc16f09fcfab26d35635af1491f36a38a8431c8d` |

Damit ist die Forderung „BMAD-Version als Commit-Hash" erfüllbar — allerdings sind es sieben Angaben, nicht eine. Ergänzend zu sichern: `_bmad/_config/files-manifest.csv` enthält SHA-256 je installierter Datei und taugt als vollständiger Integritätsnachweis fürs Replikationspaket.

**Konfiguration:** `document_output_language: German`, `communication_language: German`, `user_skill_level: intermediate`, Ausgabe nach `_bmad-output/{planning-artifacts, implementation-artifacts, test-artifacts}`, `project_knowledge = docs/`. Alle drei erstgenannten Werte sind Behandlungsparameter und müssen über alle zwölf Läufe identisch sein.

---

## 2. Der wichtigste Befund: BMAD v6 hat einen eingebauten Headless-Modus

Das ändert die Planung an einer zentralen Stelle. Mehrere Skills bringen eine eigene `references/headless.md` und ein `assets/headless-schemas.md` mit — belegt für `bmad-prd`, `bmad-architecture`, `bmad-ux`, `bmad-brainstorming` und `bmad-spec`.

Aus `bmad-prd/references/headless.md` wörtlich:

> **Detection.** Headless mode is in effect when any of the following is true: the invoking caller sets a `headless: true` flag …, the invocation is from another skill or a non-interactive runner (no TTY, no user message stream) …
>
> **General.** Do not ask. Complete the intent using what is provided … If intent remains ambiguous after inference, halt with `status: "blocked"` and a `reason` field — do not prompt. Do not greet.
>
> Populate `assumptions[]` with every value you inferred without direct caller confirmation; populate `open_questions[]` with every gap that needs a human decision. Use `status: "partial"` … `complete` = stands on its own; `partial` = caller should review before downstream use; `blocked` = no artifact produced.

**Vier Konsequenzen:**

**Erstens wird H4 weitgehend gegenstandslos.** Der geplante deterministische Antwortautomat sollte Rückfragen erkennen und nach R3/R4 beantworten. Im Headless-Modus stellt BMAD keine Rückfragen — es trifft die Annahme selbst und protokolliert sie in `assumptions[]`. Das ist inhaltlich sogar näher an R2 („Anforderungen erarbeitet das Framework selbst") als jedes Antwortskript es wäre. H4 schrumpft auf eine Rückfallregel für den Fall, dass ein Skill entgegen der Spezifikation doch fragt.

**Zweitens wird H7 robust statt fragil.** Blocker müssen nicht über Textmuster erkannt werden, sondern kommen als `status: "blocked"` mit `reason` zurück. Der Harness liest ein JSON-Feld. `partial` mit nicht-leeren `open_questions[]` ist die Zwischenstufe: Lauf geht weiter, Eintrag ins Defizitregister.

**Drittens entsteht eine unerwartet gute Datenquelle für Ebene B.** `assumptions[]` und `open_questions[]` sind maschinenlesbar und genau das, was FF3 qualitativ sucht: Wo hat das Framework Lücken selbst geschlossen, wo hat es sie offen stehen lassen? Das lässt sich ohne Zusatzaufwand je Phase auszählen.

**Viertens muss die Solo-Bedingung gleichgestellt werden.** Die Headless-Instruktion „do not ask, record assumptions" ist eine Prozessanweisung. Sie kommt in der BMAD-Bedingung aus dem Framework — also legitim, sie ist Teil der Behandlung. Die Solo-Bedingung läuft ebenfalls nicht-interaktiv, bekommt diese Anweisung aber nicht. Das ist konsistent mit dem Studiendesign, gehört aber ausdrücklich ins Methodikkapitel: Die Solo-Bedingung kann in einer nicht-interaktiven Session stehenbleiben, wo BMAD weiterläuft — und genau das wäre ein Befund über das Framework, kein Messfehler.

**Offen und zu testen:** wie der Flag `headless: true` konkret übergeben wird. Die Detection akzeptiert „no TTY, no user message stream", was bei `claude -p` erfüllt sein sollte; sicherer ist ein expliziter Präfix im Aufrufrahmen. Das ist der erste Punkt eines Probelaufs.

---

## 3. Aufrufmechanik: Skills, nicht Slash-Commands

BMAD v6.10 installiert **89 Claude-Code-Skills** nach `.claude/skills/<name>/SKILL.md`. Es gibt keine Slash-Commands und keine Agentendateien. Aufgerufen wird also entweder dadurch, dass das Modell aufgrund der Skill-Beschreibung selbst zugreift, oder dadurch, dass der Prompt den Skill beim Namen nennt.

Für den Harness heißt das: Jeder Phasenprompt nennt den Skill explizit beim Namen. Das ist deterministischer als auf Beschreibungs-Matching zu hoffen, und es ist dokumentierbar.

### Die vom Framework definierte Kette

`_bmad/bmm/module-help.csv` enthält die maßgebliche Sequenz mit Phasen, Vorgängern, Nachfolgern und — entscheidend — einer `required`-Spalte:

| Phase | Skill | Pflicht | Vorgänger |
|---|---|---|---|
| 1-analysis | `bmad-brainstorming`, `bmad-market-research`, `bmad-domain-research`, `bmad-technical-research`, `bmad-product-brief`, `bmad-prfaq` | nein | — |
| 2-planning | **`bmad-prd`** | **ja** | `bmad-product-brief` |
| 2-planning | `bmad-ux` | nein | `bmad-prd` |
| 3-solutioning | **`bmad-architecture`** | **ja** | — |
| 3-solutioning | **`bmad-create-epics-and-stories`** | **ja** | `bmad-architecture` |
| 3-solutioning | **`bmad-check-implementation-readiness`** | **ja** | `bmad-create-epics-and-stories` |
| 4-implementation | **`bmad-sprint-planning`** | **ja** | — |
| 4-implementation | **`bmad-create-story`** (`create` → `validate`) | **ja** | `bmad-sprint-planning` |
| 4-implementation | **`bmad-dev-story`** | **ja** | `bmad-create-story:validate` |
| 4-implementation | `bmad-code-review` | nein | `bmad-dev-story` |
| 4-implementation | `bmad-qa-generate-e2e-tests`, `bmad-retrospective`, `bmad-checkpoint-preview` | nein | — |

Der Story-Zyklus ist in den Beschreibungen als Schleife formuliert: *Create Story → Validate Story → Dev Story → Code Review → bei Befunden zurück zu Dev Story, sonst nächste Story, am Epic-Ende optional Retrospective.*

**Damit ist die Frage der Schrittgranularität beantwortet:** Eine Story ist der natürliche wiederaufnehmbare Schritt. Der Harness setzt seinen Checkpoint nach jedem abgeschlossenen Story-Zyklus. `sprint-status.yaml` hält den Fortschritt ohnehin persistent — der Vorstudien-Lauf hat sie erzeugt, sie ist also real und nicht nur dokumentiert.

Zusätzlich gibt es `bmad-dev-auto` — *„One iteration of an unattended development loop … Turn intent into a hardened, reviewable artifact, without human interaction"* — mit eigener HALT-Semantik und Statusdatei. Ob der Messlauf den regulären Story-Zyklus oder `bmad-dev-auto` fährt, ist eine Behandlungsentscheidung und muss begründet werden; der reguläre Zyklus ist die dokumentierte Hauptroute und damit der Standard.

### Zwei Wege, die Reihenfolge festzulegen

**Variante A – fest verdrahtet.** Der Harness ruft die Pflichtkette in der obigen Reihenfolge auf. Maximal reproduzierbar, aber die Auswahl der optionalen Schritte (Product Brief, UX, Code Review, QA-Tests) trifft dann der Forscher — und das ist eine Behandlungsentscheidung, die R2 aufweicht.

**Variante B – vom Framework geroutet.** Der Skill `bmad-help` liest den Katalog `_bmad/_config/bmad-help.csv`, erkennt aus vorhandenen Artefakten, was schon erledigt ist, und empfiehlt den nächsten Schritt. Der Harness fragt nach jedem Schritt „was ist der nächste Schritt?" und wendet R3 (Accept-Default) auf die Empfehlung an. Die Sequenz stammt dann vom Framework, nicht vom Forscher — methodisch deutlich sauberer und exakt das, wofür R3 gedacht war. Preis: ein zusätzlicher Modellaufruf je Phase (Tokenkosten, in C8 sichtbar) und eine Sequenz, die zwischen Wiederholungsläufen variieren kann — was allerdings selbst ein Ergebnis zu FF1 (Konsistenz) ist.

Empfehlung: **Variante B**, mit der Pflichtkette als Rückfallebene, falls `bmad-help` keine eindeutige Empfehlung liefert.

---

## 4. `memlog.py` — ein geschenktes Messinstrument

Jeder Skill-Lauf führt in seinem Arbeitsordner eine `.memlog.md`: append-only, typisiert, über ein gemeinsames Skript geschrieben. Typen im Vorstudien-Lauf: `decision`, `change`, `override`, `assumption`, `event`, `direction`, `question`, `version`, `constraint`.

Das deckt drei Bedarfe auf einmal:

- **C9 Defizitregister** — `question`-Einträge sind explizit als „Deferred" markierte, bewusst offen gelassene Punkte.
- **B11 Traceability** — `decision`-Einträge tragen im Vorstudien-Lauf durchgängig `Binds CAP-x, FR-y`, also bereits eine Rückbindung an Anforderungen. Für die Kette Katalog → PRD → Story → Commit ist das ein zusätzliches Zwischenglied, kein Ersatz, aber es macht das Auszählen erheblich leichter.
- **C4 Blocker-Eingriffe** — `override`-Einträge protokollieren ausdrücklich, wenn vom Standardverhalten abgewichen wurde, „including headless overrides".

Die Dateien sind mit Zeitstempel versehen. Sie gehören ins Replikationspaket und sollten in der Datensicherung neben den JSONL-Session-Logs stehen.

---

## 5. Vier Punkte, die vor dem ersten Messlauf zu klären sind

### 5.1 Der Modulumfang ist eine Behandlungsvariable

Installiert sind neben `core` und `bmm` auch `wds` (Web-Design-System-Methodik mit eigener neunstufiger Workflow-Kette), `cis` (Kreativitätstechniken), `bmb` (Skill-Builder), `tea` (Test-Architektur) und `bmad-loop`. Von 89 Skills gehören rund 40 nicht zur Kernkette.

Das ist für die Arbeit ein Problem der Konstruktdefinition: Wenn „BMAD" die Behandlung ist, muss feststehen, was BMAD in dieser Studie umfasst. Ein `wds`-Skill, der in Phase 2 die Routing-Empfehlung an sich zieht, verändert den Lauf — und in Variante B (Routing über `bmad-help`) kann genau das passieren.

Empfehlung: **Neuinstallation mit `core` + `bmm`** für die Messläufe, begründet als „Standardpfad der BMad-Method für Softwareentwicklung ohne Erweiterungsmodule". `tea` ist der einzige Grenzfall — es erzeugt Testarchitektur und damit potenziell einen echten Qualitätsbeitrag; wenn es mitläuft, muss es in beiden … nein, es kann nur in der BMAD-Bedingung laufen und ist dann als Teil der Behandlung zu benennen. Sauberer ist, es wegzulassen und in den Limitationen zu erwähnen.

### 5.2 BMAD recherchiert aktiv im Web — Datenleckage-Risiko

Aus dem Architektur-Memlog des Vorstudien-Laufs:

> `(direction)` Web-Recherche: SPEC-Capabilities … sind praktisch identisch zur bestehenden Open-Source-Referenzimplementierung todotxt/todo.txt-cli … **als reales Vorbild für Paradigma herangezogen, nicht kopiert**

Der Architektur-Skill hat also von sich aus nach einer vergleichbaren Open-Source-Implementierung gesucht und sie als Vorbild genutzt. Für die Hauptstudie ist das unmittelbar relevant: Die Eingabedateien sind zwar anonymisiert, aber ein Buchungssystem mit Sitzplan oder ein E-Commerce-Katalog sind leicht zu ähnlichen Projekten zurückzuführen. Die Limitation „Datenleckage aus Trainingsdaten" bekommt damit einen zweiten, aktiven Kanal.

Abschalten wäre keine gute Lösung — der Architektur-Skill verifiziert per Web-Suche auch aktuelle Versionsstände, was zu seiner Funktion gehört. Empfehlung: zulassen, aber alle Web-Zugriffe im JSONL-Log auswerten und je Lauf berichten, ob und welche Fremdimplementierung herangezogen wurde. Das ist ein eigener kleiner Ergebnisabschnitt und stärkt die Arbeit mehr, als ein Verbot es täte.

### 5.3 Die Vorstudie ist als Prozessquelle wertvoll — und zeigt genau die Eingriffe, die R1–R6 verbieten

Der Lauf `implementation1` (todo.sh) hat vollständige Artefakte erzeugt: PRD, Architektur-Spine, zwei Reconcile-Berichte, drei Review-Berichte, `epics.md`, Implementation-Readiness-Report, **15 Story-Dateien**, `sprint-status.yaml`, `deferred-work.md` und drei Memlogs mit Zeitstempeln.

Die Memlogs belegen zugleich mehrere Operator-Eingriffe:

> `(override)` Skipped Discovery brain-dump/stakes-calibration Q&A per explicit user instruction …
> `(decision)` User: proceed using only existing SPEC.md … draft directly, fast path
> `(direction)` User: Fast Path gewählt …
> `(event)` PRD durch User-Anweisung ('führe das genauso aus') nach Architecture-Session gepatcht

Das ist kein Mangel, sondern der Beleg dafür, warum die Vorstudie zu Recht nicht im Hauptdatensatz steht — und es ist zitierfähiges Material für den Abschnitt „Vorstudie" im Methodikkapitel: Es zeigt konkret, welche Eingriffsmöglichkeiten das Protokoll schließen musste. Zusätzlich ist erkennbar, dass der Lauf von einem `bmad-spec`-Dokument ausging, nicht von einem Feature-Katalog; der Eintrittspunkt der Hauptstudie ist also ein anderer.

### 5.4 Die Umgebungsprüfung steht noch aus

Ich kann deine Ordner lesen und beschreiben, aber die Shell, die mir hier zur Verfügung steht, läuft in einer abgeschotteten VM und **nicht** auf deinem Arch-Linux-Host. Versionsabfragen dort sind für den Harness bedeutungslos. Was ich von deinem Rechner brauche, ist die Ausgabe von:

```
claude --version && node -v && npm -v && uv --version && python3 -V && dotnet --version && git --version && ng version 2>/dev/null | head -5
```

`uv` ist keine Kür: `memlog.py` und `resolve_customization.py` werden von den Skills über `uv run` aufgerufen. Fehlt `uv`, fallen Audit-Trail und Customization-Auflösung aus.

Ebenfalls gebraucht: die Claude-Code-Session-Logs des Vorstudien-Laufs (üblicherweise unter `~/.claude/projects/<projektpfad>/*.jsonl`). Dort stehen die tatsächlichen Token- und Zeitwerte — die einzige belastbare Grundlage für die Kalibrierung von Token-Budget und Iterationsobergrenze.

---

## 6. Was sich am Harness-Entwurf dadurch ändert

| Bisher geplant | Neu |
|---|---|
| H4 als vollwertiger Antwortautomat mit eingefrorenem Antwortskript | Headless-Modus von BMAD nutzen; H4 nur noch als Rückfallregel |
| H7 Blockererkennung über Textmuster | Auswertung des JSON-Statusfelds `blocked` / `partial` / `complete` |
| Phasenliste vom Forscher festgelegt | Routing über `bmad-help`, Pflichtkette als Rückfallebene |
| Schrittgranularität offen | Ein Story-Zyklus = ein wiederaufnehmbarer Schritt; Zustand zusätzlich in `sprint-status.yaml` |
| Defizitregister als nachgelagerter Review | Primärquellen `assumptions[]`, `open_questions[]`, `.memlog.md` (`question`, `override`) |
| „BMAD-Commit-Hash" (Singular) in `config.yaml` | Sieben Modulversionen plus `files-manifest.csv` als Integritätsnachweis |

---

## 7. Nächste Schritte

1. Entscheidung über den Modulumfang (Abschnitt 5.1) und gegebenenfalls Neuinstallation.
2. Entscheidung Variante A oder B für die Sequenzsteuerung (Abschnitt 3).
3. Umgebungsausgabe und Vorstudien-Session-Logs bereitstellen (Abschnitt 5.4).
4. Probelauf zur Klärung, wie `headless: true` konkret übergeben wird und ob die Phase-4-Skills ohne eigene `headless.md` tatsächlich ohne Rückfrage durchlaufen.
5. Erst danach das Harness-Skript schreiben.
