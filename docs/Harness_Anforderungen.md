# Automatisierter Harness – benötigte Informationen und Daten

*Vorbereitungsdokument zu Abschnitt 6 „Automatisierte Durchführung" (H1–H7) · Stand 07.08.2026*

**Festgelegter Rahmen aus der Vorabklärung:** BMAD v6 (Modulstruktur `bmad/bmm`) · Ausführung lokal auf Arch Linux · Modellzugang über Claude-Abo (Pro/Max) via CLI · Kontextstrategie: frische Session je Phase mit explizitem Einlesen der zuvor erzeugten Artefakte.

---

## 0. Was diese Entscheidungen bereits erzwingen

Drei Konsequenzen, die den Skriptaufbau vorwegnehmen und deshalb hier vorangestellt sind:

**Abo statt API-Key.** Es gibt keinen Preis pro Token aus der Abrechnung. C8 muss vollständig aus den `usage`-Feldern der Session-Logs rekonstruiert und zum dokumentierten Listenpreisstand *rechnerisch* bewertet werden; das ist im Methodikkapitel als Ersatzgröße auszuweisen. Wichtiger noch: das Abo hat rollierende 5-Stunden-Limits. Ein Lauf über mehrere Stunden **wird** darauf treffen. Der Harness braucht damit zwingend eine Limit-Erkennung, die den Lauf sauber an einer Phasengrenze anhält und später an derselben Stelle fortsetzt — das ist dieselbe Mechanik wie das gewünschte manuelle Pausieren und wird gemeinsam gebaut.

**Frische Session je Phase.** Das ist die einzige Variante, die die Kernforderung erfüllt, dass BMAD-Artefakte tatsächlich genutzt werden: Wenn der Kontext leer startet, kann eine Phase nur das wissen, was sie aus den Dateien liest. Nutzung wird dadurch nachweisbar statt nur wahrscheinlich. Nebeneffekt: Tokenverbrauch je Phase ist sauber zurechenbar (nötig für C8) und Auto-Compact — das C7 und C8 sonst verfälscht — tritt nicht auf.

**Beide Bedingungen, ein Harness.** Die Solo-Bedingung braucht dieselbe Phasensteuerung, dieselbe Zustandsdatei, dasselbe Logging. Unterschied ist ausschließlich, dass in der Solo-Bedingung keine BMAD-Definitionen im Arbeitsverzeichnis liegen und die Phasen keine Agentenaufrufe, sondern feste Fortsetzungsintervalle sind.

---

## 1. BMAD-Installation — der größte Block

Ohne diese Angaben lässt sich die Aufrufkette nicht schreiben. Das meiste kann ich selbst aus der Installation auslesen, wenn ich Zugriff auf den Ordner bekomme.

**Was ich selbst ermitteln kann (bei verbundenem Ordner):**

- Exakter Commit-Hash und Versionsstring der BMAD-Installation (für `config.yaml`, H6).
- Verzeichnisbaum: Modulordner (`bmad/bmm`, `bmad/core`, ggf. weitere), Agentendefinitionen, Workflow-Ordner mit `workflow.yaml`, `instructions.md`, `template.md`, `checklist.md`.
- Die in `.claude/commands/` installierten Slash-Commands — sie sind der eigentliche Aufrufkanal aus der CLI heraus.
- Die konfigurierten Ausgabepfade je Workflow (in v6 typischerweise über eine Modulkonfiguration gesetzt, z. B. Ziel-Ordner für PRD, Architektur, Stories). Diese Pfade sind der Angelpunkt: Sie bestimmen, was die Folgephase einliest.
- Welche Workflows aufeinander verweisen, also die vom Framework selbst vorgesehene Reihenfolge.

**Was nur du entscheiden kannst:**

1. **Welcher Workflow-Pfad gefahren wird.** BMAD v6 kennt unterschiedliche Projektskalen (kleines Feature bis Full-Stack-Neubau) mit unterschiedlich vielen Phasen. Für 13 bzw. 14 Features mit Frontend, Backend und Persistenz ist die volle Kette plausibel, aber die Wahl ist eine methodische Festlegung und muss begründet in der Arbeit stehen. Falls der Workflow diese Skala selbst bestimmt, ist genau das der bessere Weg — dann entscheidet das Framework, nicht du (R2), und wir protokollieren nur, wofür es sich entschieden hat.
2. **Ob eine BMAD-Konfiguration vom Standard abweicht.** Sprache der Ausgaben, Detailtiefe, aktivierte optionale Schritte. Jede Abweichung vom Auslieferungszustand ist eine Behandlungsvariable und gehört ins Replikationspaket.
3. **Erfahrungswerte aus deinen zwei Vorstudien-Läufen** (siehe Abschnitt 5).

**Offene technische Frage, die ich an der Installation prüfen muss:** ob die v6-Workflows in einer nicht-interaktiven Session überhaupt vollständig durchlaufen oder an Punkten stehenbleiben, die eine Eingabe erwarten. Davon hängt ab, ob der Antwortautomat (H4) ein einfacher Vorab-Anhang an den Prompt sein kann oder eine echte Stream-Auswertung mit Wiedereintritt braucht.

---

## 2. Laufzeitumgebung

- **Claude-Code-CLI-Version** (`claude --version`) — gehört als Skriptversion in jede `config.yaml`.
- **Modellbezeichner**, der in allen zwölf Läufen erzwungen wird, plus Zugriffsdatum. Muss explizit gesetzt werden, nicht dem Default überlassen, sonst wechselt er dir mitten in der Erhebung.
- **Toolchain-Versionen:** Node/npm, .NET SDK, Angular CLI. Diese Versionen sind identisch mit denen, die im Seed-Repository gepinnt werden.
- **Datenbank für Projekt B.** Der Katalog verlangt Katalog, Warenkorb und Bestellungen. Zu klären: lokal installierter SQL Server, Container, oder eine leichtere Engine, die im Seed vorkonfiguriert ist. Das ist keine Nebensache — wenn das Modell selbst eine Datenbank aufsetzen muss, misst du auch Infrastrukturarbeit, und wenn es keine bekommt, scheitern die Akzeptanztests aus einem Grund, der nichts mit dem Framework zu tun hat.
- **Berechtigungsprofil.** Welche Werkzeuge vorab freigegeben sind (Dateisystem, Shell, Netzwerk/Paketinstallation, Git). Muss für beide Bedingungen byteidentisch sein und wird in `config.yaml` festgehalten. Netzwerkzugriff ist unvermeidbar, weil `npm install` und `dotnet restore` ihn brauchen.
- **Ablageort** der Laufdaten nach dem Schema `runs/<Projekt>/<Ansatz>/<RunNr>/{logs,artifacts,code,metrics}` und der voraussichtliche Platzbedarf. Zwölf Läufe mit je einem vollständigen `node_modules` sind mehrere Gigabyte; sinnvoll ist, den Code nach Laufende zu committen und den Arbeitsbaum ohne Abhängigkeiten zu archivieren.

---

## 3. Eingefrorene Studienartefakte

Diese Dateien sind Voraussetzung, nicht Ergebnis des Skripts. Nach Abschnitt 16 des Gesamtstands sind sie noch offen.

| Artefakt | Status | Wird gebraucht für |
|---|---|---|
| Eingabedatei Projekt A (nach Vorlage Eingabespezifikation §7) | offen | H2, Input-Hash |
| Eingabedatei Projekt B | offen | H2, Input-Hash |
| Seed-Repository mit gepinnten Versionen, Testrunner, lauffähigem Build- und Testbefehl | offen | H1, Seed-Hash |
| Feature-Kataloge, verifiziert und eingefroren | Entwurf vorhanden | A2, B1, B1b, B6, B11 |
| Aufrufrahmen je Bedingung (der minimale Text um die Eingabedatei herum) | offen | einziger struktureller Unterschied, dokumentationspflichtig |

Der Aufrufrahmen verdient besondere Aufmerksamkeit: Er ist in der BMAD-Bedingung die Übergabe an den ersten Agenten, in der Solo-Bedingung der komplette Auftrag. Beide Fassungen müssen so knapp wie möglich sein und dürfen keine Prozessanweisung enthalten — sonst behandelst du die Solo-Bedingung unbeabsichtigt mit.

---

## 4. Steuerungsparameter

Zahlen, die vor dem ersten Messlauf feststehen müssen, weil sie sonst nachträglich wie eine Anpassung an Ergebnisse aussehen:

- **Token-Budget je Lauf**, identisch für beide Bedingungen. Kalibriert aus den Vorstudien-Läufen.
- **Zeitbudget je Lauf** (Wall-Clock), identisch für beide Bedingungen.
- **Iterationsobergrenze** für die Solo-Bedingung (C7).
- **Phasenintervall in der Solo-Bedingung** — nach wie vielen Turns oder Minuten committet und taggt der Harness, damit H5 in beiden Bedingungen vergleichbar greift.
- **Abbruchkriterium „erklärte Fertigstellung" in der Solo-Bedingung.** Das ist die heikelste Definition im ganzen Skript: Woran genau erkennt der Automat, dass das Modell sich für fertig hält? Ein Textmuster ist fragil. Robuster ist ein am Phasenende gestellter, immer gleicher Fragesatz mit erzwungenem Ja/Nein-Format. Diese Frage ist selbst eine Prozessanweisung und muss als solche im Methodikkapitel offengelegt und in beiden Bedingungen identisch angewandt werden.
- **Blocker-Definition (H7).** Wann hält der Lauf an und verlangt einen Eingriff? Kandidaten: Prozess bricht ab, Build schlägt in Folge N-mal fehl, keine Dateiänderung über N Turns, Werkzeug fordert eine nicht vorab erteilte Berechtigung, Antwortautomat findet keine passende Regel. Jede Schwelle braucht einen Zahlenwert.
- **Verhalten bei Rate-Limit.** Warten und automatisch fortsetzen, oder anhalten und auf deinen Start warten? Die Wartezeit zählt in C1c (Wall-Clock) und würde diese Kennzahl verzerren — daher der Vorschlag, Limit-Pausen als eigene Zeitspanne zu protokollieren und aus C1c herauszurechnen. Das gehört in die Limitationen.

---

## 5. Antwortautomat H4 und Vorstudiendaten

Der deterministische Antwortautomat braucht empirisches Material. Aus deinen beiden manuellen Vorstudien-Läufen benötige ich:

- Die tatsächlich aufgetretenen **Rückfragen im Wortlaut**, mit der jeweiligen Phase. Daraus entsteht das eingefrorene Standardantworten-Skript.
- Die **Form** der Rückfragen: freie Frage, nummerierte Optionsliste, Optionsliste mit markierter Empfehlung, Bestätigungsabfrage. Die Erkennungsregeln des Automaten hängen daran.
- Die aufgetretenen **Blockertypen** — Grundlage der Taxonomie und der Schwellenwerte oben.
- **Grobe Verbrauchswerte**: Dauer und, falls verfügbar, Tokenverbrauch je Phase. Ohne diese Kalibrierung sind Budget und Iterationsobergrenze geraten.
- Die **erzeugten Artefakte** und deren Dateinamen, damit ich die Handoff-Kette gegen die tatsächliche Ausgabe verifizieren kann statt gegen die Dokumentation.

Falls die Chatverläufe nicht mehr vollständig vorliegen: sag mir, was noch da ist. Ein retrospektives Protokoll steht ohnehin als offener Punkt in Abschnitt 16, und beides lässt sich in einem Zug erledigen.

---

## 6. Was ich zum Pausieren und Fortsetzen wissen muss

Das gewünschte Verhalten — laufenden Prompt zu Ende führen, dann anhalten, später an derselben Stelle weiter — ist umsetzbar, verlangt aber drei Festlegungen von dir:

1. **Granularität der Wiederaufnahme.** Der kleinste wiederaufnehmbare Schritt ist ein abgeschlossener Prompt. Bei sehr langen Phasen (eine Implementierungsphase kann Stunden laufen) bedeutet ein Abbruch mittendrin, dass diese Phase verloren ist. Frage: Soll der Harness die Implementierung in kleinere, einzeln wiederaufnehmbare Schritte zerlegen — etwa eine Story pro Prompt? Für BMAD ist das ohnehin die natürliche Granularität; in der Solo-Bedingung müsste ein entsprechendes Intervall gesetzt werden, damit beide Bedingungen gleich behandelt sind.
2. **Was bei einem harten Absturz gelten soll.** Wenn der Rechner ausgeht oder der Prozess stirbt: Phase als unvollständig verwerfen und neu starten, oder aus dem letzten Git-Tag heraus fortsetzen? Beides ist vertretbar, aber es muss vorher feststehen und für alle Läufe gleich gelten.
3. **Ob mehrere Läufe parallel laufen dürfen.** Sequenziell ist methodisch sauberer und beim Abo ohnehin fast erzwungen, weil parallele Läufe sich dasselbe Rate-Limit teilen und sich gegenseitig in Wartezeiten treiben — was C1c verfälscht. Ich empfehle strikt sequenziell.

Der Mechanismus selbst braucht keine Entscheidung von dir: eine Zustandsdatei je Lauf, die nach jedem abgeschlossenen Schritt fortgeschrieben wird, plus eine Stopp-Markierung, die der Harness zwischen den Schritten prüft. Fortsetzen heißt dann, dasselbe Skript mit derselben Lauf-ID erneut zu starten.

---

## 7. Messerfassung

- **Welche Felder der JSON-Stream-Ausgabe** tatsächlich Token, Dauer und Turn-Grenzen enthalten — prüfe ich am Livesystem, weil sich das Format zwischen CLI-Versionen ändert. Falls dort etwas fehlt, muss der Harness die Lücke clientseitig schließen (Zeitstempel vor und nach jedem Aufruf).
- **Preisstand** für die rechnerische Kostenbewertung: Datum und Quelle, einmal festgehalten, für alle Läufe gleich.
- **Referenzrahmen für „handhabbar"** (FF2): welches Abonnement oder welcher Stundensatz als Vergleichsmaßstab dient. Muss vorab im Methodikkapitel stehen.
- Ob der Harness die **Metrikerhebung** (SonarQube, Semgrep, Akzeptanztests) direkt anschließen soll oder ob das ein getrenntes Skript wird. Empfehlung: getrennt. Die Erhebung ist wiederholbar auf archiviertem Code, der Lauf nicht; eine Vermischung macht beide Teile fragiler.

---

## 8. Nächster Schritt

Am schnellsten kommen wir voran, wenn ich mir die BMAD-Installation ansehen kann. Verbinde dazu im Desktop-App den Ordner, in dem BMAD liegt (bzw. das Projektverzeichnis, in dem du die Vorstudien-Läufe gemacht hast) — dann klärt sich Abschnitt 1 fast vollständig von selbst, und ich kann die Phasenliste, die Artefaktpfade und die Handoff-Kette direkt aus den Workflow-Definitionen ableiten statt aus der Dokumentation.

Parallel dazu sind die drei Angaben aus Abschnitt 5 (Rückfragen, Blocker, Verbrauchswerte der Vorstudie) das, was ich von dir nicht selbst herausfinden kann.
