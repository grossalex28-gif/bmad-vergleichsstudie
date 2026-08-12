# Harness und Probelauf – Entscheidungen, Befunde, offene Punkte

*Stand 11.08.2026 · konsolidiert aus der Entwicklungs- und Probelaufsitzung*
*Ergänzt `claude/Harness_Anforderungen.md`, `claude/Zeitregime_Laufsteuerung.md`, `claude/BMAD_Installationsbefund.md` und `claude/Feature-Kataloge_final.md`*

> **Achtung:** `claude/Feature-Kataloge_Entwurf.md` ist überholt. Maßgeblich ist `claude/Feature-Kataloge_final.md` (11.08.2026).

---

## 1. Getroffene Festlegungen

| Gegenstand | Festlegung | Begründung in Kürze |
|---|---|---|
| BMAD-Version | v6.10.0, **nur `core` + `bmm`** (46 Skills) | Sonst ist unklar, was „BMAD" als Behandlung umfasst; `wds` könnte das Routing an sich ziehen |
| Modell | `claude-sonnet-5`, Zugriffsdatum **07.08.2026** | Die CLI liefert keine datierte Kennung, nur den kanonischen Alias — das Datum trägt die Beweislast |
| Zugang | Claude-Abo über die CLI | Kosten kommen aus `costUSD` der CLI, sind Schätzung des Anbieters, keine Abrechnung |
| Kontext | **Frische Session je Schritt**, kein `--resume` | Macht nachweisbar, dass BMAD-Artefakte tatsächlich gelesen werden; verhindert Auto-Compact |
| Sequenz | **Variante B**: `bmad-help` routet, Story-Zyklus aus `module-help.csv` abgelesen | Reihenfolge stammt vom Framework (R2/R3), nicht vom Forscher |
| Zeitregime | T1 8 h aktiv · T2 36 h Kalender · T3 180 min je Schritt | Drei Zwecke: Budget, Planschutz, Stall-Erkennung |
| Rahmung | **Umfangsfixiert**, Deckel als Notbremse | Vorab fixierte Umschaltregel: greift T1 in mehr als einem von drei BMAD-Läufen, gilt das Design als zeitfixiert |
| `bmad-code-review` | **Beibehalten**, trotz 42 % Kostenanteil | Ohne Qualitätsgates zu messen und dann über Codequalität zu urteilen wäre ein anderes Experiment |
| Feature-Kataloge | **Einfrierbereit: A 13, B 14 Features** (`claude/Feature-Kataloge_final.md`) | Manueller Entwurf durch BMAD-Ableitung validiert; fünf Präzisierungen übernommen, davon zwei fachlich neu |
| Katalogableitung | Durch BMAD, aber **einmalig vor der Erhebung** (`derive_catalog.py`) | Das Original darf in keinen Messlauf, sonst ist FF4 wertlos |
| Web-Werkzeuge | `WebSearch`/`WebFetch` gesperrt, Bash-Kommandos klassifiziert | Datenleckage wird zur Messgröße statt zur Hoffnung |
| Hooks | Für die Messläufe **abgeschaltet** | Vier `bmad-loop`-Hooks lagen global; ein Stop-Hook hätte die Schrittgrenzen ausgehebelt |

---

## 2. Kennzahlen aus dem Probelauf

Miniprojekt „Werkzeugausgabe", 5 Features, TypeScript auf Node. Beide Bedingungen, derselbe Auftrag.

| | BMAD | Solo | Faktor |
|---|---|---|---|
| Schritte | 25 | 2 | |
| Aktivzeit | 146 min | 8,1 min | **18×** |
| Kosten | 48,55 USD | 1,81 USD | **27×** |
| Turns | 1.172 | 78 | 15× |
| Produktivcode | ~650 Zeilen | ~500 Zeilen | 1,3× |
| Testcode | 717 Zeilen | 253 Zeilen | 2,8× |
| Prozessdokumente | 2.100 Zeilen | 66 Zeilen | 32× |

### Aufwandsverteilung in der BMAD-Bedingung

| Skill | Aufrufe | Zeit | Kosten | Anteil |
|---|---|---|---|---|
| `bmad-code-review` | 6 | 52,8 min | 20,49 USD | **42 %** |
| `bmad-create-story` | 8 | 29,8 min | 9,29 USD | 19 % |
| `bmad-dev-story` | 6 | 23,5 min | 8,88 USD | **18 %** |
| `bmad-architecture` | 1 | 14,8 min | 4,86 USD | 10 % |
| PRD, Epics, Readiness, Sprint | 4 | 13,4 min | 4,11 USD | 8 % |
| Router | 25 | 12,0 min | 0,92 USD | 2 % |

**Nur 18 % des Aufwands fließen ins Schreiben von Code, 42 % ins Überprüfen.** Das ist ein Ergebnis für FF2, keine Fehlkonfiguration.

**Richtwerte für die Hochrechnung:** Planung rund 29 min und 9 USD je Lauf, danach 15,8 min und 5,85 USD je Story, etwa 1,2 Stories je Feature. Für 13 Features also grob 7 h und 110–170 USD je BMAD-Lauf.

---

## 3. Technische Befunde

**Tokenzahlen sind ohne Aufschlüsselung irreführend.** Von 63,8 Mio. Tokens waren 60,0 Mio. Cache-Reads (94 %) — derselbe Kontext, bei jedem Turn erneut gelesen. Ohne sie bleiben 3,8 Mio. C8 ist stets nach Input, Output, Cache-Creation und Cache-Read getrennt zu berichten; die Rohsumme misst im Wesentlichen die Zahl der Turns und würde die BMAD-Bedingung systematisch schlechter aussehen lassen.

**Subagenten sind erfasst.** `bmad-code-review` startet drei Subagenten. Die Logdatei enthält mehrere `result`-Ereignisse mit aufsteigenden Kosten (0,91 → 2,19 → 2,32 → 3,59 USD); der letzte Wert stimmt mit `modelUsage` überein und ist der vollständige. Die `assistant`-Ereignisse tragen `parent_tool_use_id`, `subagent_type` und `task_description` — damit lässt sich nachträglich auswerten, welcher Anteil von BMADs Verbrauch auf Subagenten entfällt.

**`bmad-code-review` skaliert nicht mit der Codebasis.** Er arbeitet mit `git diff HEAD~1 HEAD`, prüft also die Änderung der jeweiligen Story. Die Kosten blieben zwischen 2,86 und 3,95 USD flach, während die Codebasis von 684 auf 1.330 Zeilen wuchs. Eine Bündelung aller Reviews ans Laufende spart daher nichts und würde zusätzlich die Rückkopplung zerstören: In `deferred-work.md` steht ein Befund aus dem Review von Story 1-2, der in Story 2-2 behoben wurde.

**Der Headless-Modus greift.** Keine Begrüßung in 25 Schritten, JSON-Status in jedem Schritt, 23 × `complete` und 2 × `partial`, dazu **54 protokollierte Annahmen und 5 offene Fragen**. Auch die Phase-4-Skills ohne eigene `headless.md` laufen rückfragefrei.

**Kein einziger Web-Zugriff im gesamten Lauf** — trotzdem existiert ein Artefakt `review-tech-currency.md`, und `epics.md` legt konkrete Versionen fest (Node.js ^26, TypeScript ^7.0.2, Express ^5.2.1, Vite ^8.2.1). Der Architektur-Skill schreibt vor, Versionen im Web zu verifizieren; das ist nicht geschehen. **Ein Prozessartefakt behauptet eine Prüfung, die nicht stattgefunden hat** — handfestes Material für das Defizitregister C9. In der Vorstudie hatte derselbe Skill noch aktiv recherchiert und dabei die Referenzimplementierung `todo.txt-cli` gefunden.

**Der Seed ist eine Bitte, keine Vorgabe.** Die Solo-Bedingung hat `package.json` und `tests/smoke.test.js` des Seeds ersetzt. Die nachgelagerte Stack-Prüfung aus `package-lock.json` und den `*.csproj`-Dateien ist damit keine Formalie, und die Ausschlussliste muss damit rechnen, dass Seed-Dateien verschwinden.

**Die Solo-Bedingung terminiert nach einem einzigen Arbeitsschritt.** Er packt alles in eine Session (67 Turns) und meldet im zweiten Schritt `FERTIG: JA`. Verifiziert: 16 von 16 Tests grün, alle fünf Features vorhanden, Abgrenzung eingehalten. Daraus folgt die T3-Anhebung auf 180 min — bei 13 Features würde ein solcher Einzelschritt sonst als Blocker abgeschossen. Zugleich greift in der Solo-Bedingung die Entscheidung „frische Session je Schritt" faktisch nicht; bei größeren Katalogen droht dort Auto-Compact.

**Die Bedingungen terminieren nach verschiedenen Mechanismen.** BMAD endet, wenn sein Workflow durch ist; Solo endet mit einer Selbstauskunft. Das ist konstruktionsbedingt und gehört ins Methodikkapitel.

---

## 4. Der Harness

Liegt in `~/dev/Bachelor/bmad-vergleichsstudie`. Python-Standardbibliothek, keine Abhängigkeiten.

| Datei | Zeilen | Inhalt |
|---|---|---|
| `harness.py` | 991 | Steuerung: `init`, `run`, `status`, `stop`, `reopen` |
| `audit.py` | 122 | Hook-Prüfung, Netzwerkklassifikation, Hashes |
| `prompts.py` | 91 | Die exakten Texte an beide Bedingungen — der Aufrufrahmen |
| `selftest.py` | — | **69 Prüfungen** gegen eine CLI-Attrappe, ohne Tokenverbrauch |
| `tools/fake_claude.py` | — | Die Attrappe |
| `derive_catalog.py` | — | Einmalige Katalogableitung, außerhalb der Messläufe |
| `TESTANLEITUNG.md`, `README.md` | — | Beide Teststufen, Entwurfsentscheidungen |

**Umsetzung von H1–H7:** frisches Arbeitsverzeichnis aus Seed-Commit · SHA-256-Abgleich der Eingabedatei vor jedem Lauf · `claude -p --output-format stream-json` · Headless-Präfix und JSON-Statusauswertung statt Textmuster · Git-Commit und Tag je Schritt · JSONL je Schritt, `protokoll.csv`, `config.json` · `blocked` hält den Lauf an.

**Zusätzlich abgesichert:** Hook-Sperre beim `init` · Modelldrift-Warnung je Schritt · Werkzeugzensus getrennt nach Haupt- und Subagent · Netzwerkklassifikation jedes Bash-Kommandos (`fetch` = Befund, `package` = erwartet) · Tokenbudget ohne Cache-Reads · `reopen` als protokollierter Operator-Eingriff.

**Pausieren:** `stop` oder Strg-C setzt ein Flag, das *zwischen* zwei Schritten geprüft wird; der laufende Prompt läuft aus, `state.json` wird atomar geschrieben, Fortsetzen ist derselbe `run`-Befehl. Rate-Limit-Pausen wartet der Harness selbst ab und rechnet sie **nicht** auf T1 an.

**Abo-Verbrauch:** 2,1 h Aktivzeit haben 50 % des 5-Stunden-Fensters verbraucht, also etwa 4 h nutzbare Aktivzeit je Fenster. Ein 8-Stunden-Lauf braucht zwei Fenster.

---

## 4a. Katalogableitung durch BMAD (11.08.2026, abgeschlossen)

Einmaliger Vorbereitungsschritt außerhalb der Erhebung, über `derive_catalog.py`.

| Projekt | Repository | Commit | Dauer | Kosten | Ergebnis |
|---|---|---|---|---|---|
| A | `adamlarner/angularbooking` | `51f5283…` | 3,7 min | 1,11 USD | 19 Features |
| B | `0legKot/Godsend` | `dc97055…` | 5,1 min | 1,78 USD | 23 Features |

**Befunde.** Beide Entwürfe hielten die Anonymisierungsregel vollständig ein — kein Endpunktpfad, kein Klassen- oder Dateiname, kein Framework, kein Schema, kein Projektname. BMAD extrahiert jedoch **vollständig statt zugeschnitten**: Es schloss nur aus, was der Prompt namentlich nannte, und hat keinen Begriff von einem Messbudget. Der Zuschnitt bleibt Entscheidung des Untersuchenden.

Wertvoll waren die *Beobachtungen*: In Projekt B identifizierte BMAD toten Code (leere Platzhalter im Frontend, serverseitige Funktionen, die absichtlich einen Laufzeitfehler werfen, eine auskommentierte gewichtsbasierte Bestellvariante, eine Berechtigungsstufe ohne Ablauf); in Projekt A offene Fragen zur Nebenläufigkeit beim Sitzplatzverkauf.

**Übernommen:** Lieferant–Produkt–Preis als n:m-Beziehung samt Lieferant je Warenkorbposition (B), Aufrufzähler (B), Ersetzungsregel bei Bewertungen (B), Dauer und Altersfreigabe (A), Atomizität der Buchungsablehnung (A-F13).
**Verworfen:** Verwaltungsbereiche, Artikelmodul, Lieferantenverwaltung, Kommentarbäume, Trennung Veranstaltung/Vorstellungstermin.

Der Ableitungsschritt ist damit zugleich ein Konstruktvaliditätsnachweis für die Kataloge und ein deskriptiver Befund über BMADs Anforderungsableitung. Die Artefakte liegen unter `bmad-vergleichsstudie/catalogs/{A,B}/` mit `quelle.json`, `lauf.jsonl` und den Analysedokumenten — **sie dürfen in keinen Messlauf geraten**.

---

## 5. Offene Punkte

1. **Anfangsdatenbestand festlegen.** Beide Kataloge setzen Daten voraus, die kein Feature anlegt (die Verwaltungsbereiche sind ausgeschlossen). Ohne Festlegung ist A-F1 gegen eine leere Anwendung nicht prüfbar und die Akzeptanztests haben keinen definierten Ausgangszustand. Vorschlag: Datei im Seed-Repository, in den technischen Rahmenbedingungen der Eingabedatei verlangt — Inhalt fachlich vorgegeben, Dateiformat nicht.
2. **Treiberschnittstelle B anpassen** (`claude/Akzeptanztests_Architektur.md`): `LegeInWarenkorb` braucht `lieferantId`, `HoleProdukt` liefert Lieferanten mit Preisen sowie Bewertungs- und Aufrufkennzahlen, Warenkorbpositionen tragen den Lieferanten, `sortierung` akzeptiert den Aufrufzähler. Schnittstelle A bleibt unverändert.
3. **Eingabedateien** für A und B nach `claude/Eingabespezifikation.md` schreiben: anonymisiert, ohne Prozessanweisung, ohne API-Vertrag.
4. **Seed-Repository** mit leerer Angular-Anwendung und leerer ASP.NET-Core-Projektmappe, gepinnten Versionen, lauffähigem Build- und Testbefehl. Größter verbleibender Bauaufwand.
5. **Datenbankfrage für Projekt B** — gehört in die technischen Rahmenbedingungen und muss vor dem Einfrieren feststehen.
6. **`config.messlauf.json`** anlegen und einfrieren; Tokenbudget und `solo_max_iterations` aus den Probelaufdaten kalibrieren.
7. **`project_name`** in der BMAD-Konfiguration auf einen neutralen, konstanten Wert setzen — er taucht in Artefaktnamen auf (aktuell `bmad-core-bmm`).
8. Klären, ob die Websuche in der CLI-Konfiguration überhaupt verfügbar ist: verfügbar und ungenutzt ist ein Befund über BMAD, nicht verfügbar ist eine Eigenschaft des Aufbaus und gehört in die Limitationen.
9. **SonarQube** einrichten und einfrieren (nicht zeitkritisch, läuft auf archiviertem Code).

**Laufreihenfolge:** abwechselnd, Projekt A vollständig vor Projekt B —
`A-BMAD-1 · A-SOLO-1 · A-BMAD-2 · A-SOLO-2 · A-BMAD-3 · A-SOLO-3`, dann B.
Entscheidungspunkt nach Lauf 6: mehr als zwölf Kalendertage verbraucht → Projekt B entfällt.

---

## 6. Für Methodikkapitel und Limitationen vorzumerken

- Die Läufe fanden **ohne aktive Claude-Code-Hooks** statt; Nachweis mit Hash der Einstellungsdatei in jeder `config.json`.
- Der Zeitdeckel wird **dem Modell nicht mitgeteilt** — er ist ausschließlich harnessseitig durchgesetzt, weil ein Hinweis darauf eine Prozessanweisung wäre.
- **T3 bindet konstruktionsbedingt nur die Solo-Bedingung**, weil BMAD die Arbeit selbst zerlegt. Der Deckel ist in beiden Bedingungen identisch gesetzt.
- Die Terminierungsregel der Solo-Bedingung hängt an der Zeile `FERTIG: JA` — eine Prozessanweisung, die offenzulegen ist.
- **C8 ist eine Anbieterschätzung**, keine Abrechnung. Vier Tokenkomponenten getrennt berichten.
- Rate-Limit-Wartezeiten sind aus C1c herauszurechnen; sie stehen getrennt in `protokoll.csv`.
- Der Modellbezeichner ist ein Alias ohne Datumsangabe; die Erhebung ist über den **Zeitraum** zu datieren, nicht über einen Tag.
- **`bmad-code-review` ist optional** und wurde bewusst beibehalten; die Kostenverteilung 18 % Implementierung zu 42 % Review ist als Ergebnis zu berichten.
- Web-Werkzeuge waren gesperrt; BMADs Versionsprüfung arbeitet damit auf Modellwissen.
