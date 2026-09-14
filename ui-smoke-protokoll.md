# Startnachweis der Oberflächen – Prüfprotokoll

*Festgelegt am 14.09.2026, vor der ersten Ausführung. Kriterium am selben Tag auf Fassung 1.1 angepasst, siehe Abschnitt 6. Ergänzende Prüfung zur Schließung der in Abschnitt 6.6 benannten Lücke, dass die Oberflächen der zwölf Implementierungen bislang nicht im Betrieb geprüft wurden. Kein Bestandteil des eingefrorenen Metrikkatalogs und keine nachträgliche Änderung an A1 oder A2.*

## 1. Zweck und Abgrenzung

Geprüft wird ausschließlich, ob die Oberfläche einer Implementierung startet, rendert und dabei ihr eigenes Backend erreicht. Nicht geprüft werden Bedienbarkeit, Gestaltung, einzelne Katalogmerkmale und das Verhalten nach einer Benutzereingabe. Die Prüfung verändert keinen Datenbestand, weil sie nur die Startseite lädt.

Die Ergebnisse werden neben A2 berichtet und führen nicht zu einer Neuberechnung von A1 oder A2. Weicht ein Befund von einer bereits berichteten A2-Bewertung ab, wird die Abweichung benannt und nicht aufgelöst.

## 2. Ablauf je Implementierung

1. Prüfen, dass der Container `sqlserver-bachelor` läuft und die Datenbank `<projekt>_<ansatz>_<wiederholung>` existiert.
2. Backend mit `dotnet run` gegen diese Datenbank starten, auf dem Port, den die Proxy- oder Umgebungskonfiguration des jeweiligen Frontends erwartet.
3. Warten, bis das Backend auf der Wurzel-URL antwortet, höchstens 90 Sekunden.
4. Frontend mit `ng serve` auf Port 4200 starten und warten, bis es antwortet, höchstens 180 Sekunden.
5. Startseite `http://localhost:4200/` in einem headless betriebenen Chromium laden, nach dem Laden fünf Sekunden nachlaufen lassen, Screenshot in voller Seitenhöhe ablegen.
6. Beide Prozesse beenden.

Aufgezeichnet werden der HTTP-Status des Dokuments, die Textlänge der gerenderten Seite, alle Datenabrufe der Seite samt Status, alle Konsolenfehler und alle unbehandelten Skriptfehler. Als Datenabruf gilt jede Anfrage, die Chromium dem Ressourcentyp `xhr` oder `fetch` zuordnet, unabhängig vom Pfad und unabhängig davon, ob sie über den Entwicklungsserver oder direkt an den Backend-Port geht.

## 3. Urteil

Das Urteil ist einstufig und wird aus den aufgezeichneten Werten abgeleitet, nicht durch Betrachtung vergeben.

| Urteil | Bedingung |
|---|---|
| bestanden | Dokument mit Status unter 400 geladen, Seite enthält Text, mindestens ein Datenabruf, kein Datenabruf mit Status 0 oder ab 400, kein unbehandelter Skriptfehler |
| startet nicht | Dokument nicht geladen oder Status ab 400 |
| rendert nicht | Dokument geladen, Seite bleibt ohne Text |
| kein Backend-Kontakt | Seite rendert, löst beim Laden aber keinen Datenabruf aus |
| Backend-Kontakt fehlerhaft | mindestens ein Datenabruf mit Status 0 oder ab 400 |
| unbehandelter Skriptfehler | sonst, aber mit unbehandeltem Fehler im Seitenkontext |

Konsolenfehler werden gezählt und aufgeführt, gehen aber nicht in das Urteil ein, weil sie in Entwicklungsservern regelmäßig ohne funktionale Bedeutung auftreten.

Ein Urteil `kein Backend-Kontakt` ist kein Fehlerbefund. Es bedeutet, dass die Startseite der jeweiligen Implementierung keine Daten nachlädt, und ist als solches zu berichten.

Das Urteil `bestanden` sagt nichts darüber aus, ob die geladenen Daten auch angezeigt werden. Eine Seite, die ihre Endpunkte erfolgreich aufruft, die Antwort aber leer lässt, besteht diese Prüfung. Solche Fälle sind über die Textlänge und den Screenshot erkennbar und gesondert zu berichten.

## 4. Grenzen dieser Prüfung

Sie findet nach Abschluss der Hauptauswertung statt und damit in Kenntnis der Ergebnisse. Sie erfasst nur die Startseite und sagt nichts darüber aus, ob die weiteren Ansichten funktionieren. Die verwendeten Abhängigkeiten werden zum Prüfzeitpunkt installiert, sofern sie im Laufverzeichnis fehlen, weshalb ein Frontend, das bei der Erhebung noch baute, an einer inzwischen veränderten Paketversion scheitern kann. Ein solcher Fall ist als Befund über die Prüfung und nicht über die Implementierung zu werten.

Zwei Eigenschaften der Ablaufsteuerung sind bei der Auswertung mitzudenken. Das Skript wartet auf Port 4200, ohne den Prozess des gestarteten Entwicklungsservers zu überwachen, sodass ein fremder oder aus einem vorherigen Lauf übriggebliebener Server auf demselben Port unbemerkt geprüft würde. Für die dokumentierte Ausführung ist das ausgeschlossen, weil jedes `<lauf>.frontend.log` einen eigenen Build mit den Chunk-Namen der jeweiligen Implementierung ausweist. Außerdem bildet die Übersicht alle JSON-Dateien im Ergebnisverzeichnis ab, weshalb Ergebnisse unterschiedlicher Fassungen getrennt abzulegen sind.

## 5. Ablage

`smoke-ergebnisse/<lauf>.json` mit den Messwerten, `smoke-ergebnisse/<lauf>.png` mit dem Screenshot, `smoke-ergebnisse/uebersicht.csv` mit einer Zeile je Lauf und den Spalten Lauf, Urteil, Datenabrufe, fehlerhafte Datenabrufe und Konsolenfehler. Die Protokolle der beiden Prozesse liegen daneben als `.backend.log` und `.frontend.log`, ein etwaiges nachgeholtes `npm install` als `.npm.log`. Die Ergebnisse der verworfenen Fassung 1.0 liegen unverändert unter `smoke-ergebnisse-fassung-1.0/`.

## 6. Änderung des Kriteriums nach dem ersten Durchlauf

Fassung 1.0 erkannte einen Datenabruf daran, dass der Pfad der Anfrage die Zeichenfolge `/api/` enthielt. Diese Pfadkonvention ist nicht Teil der eingefrorenen Aufgabenstellung, sondern eine Annahme über die Benennung der Routen. Der erste Durchlauf zeigte, dass sie nicht für alle Implementierungen gilt. Fassung 1.1 stellt deshalb auf den von Chromium vergebenen Ressourcentyp um, der von der Routenbenennung unabhängig ist.

Die Änderung betrifft genau einen Lauf. A-BMAD-3 spricht sein Backend ohne Entwicklungsserver-Proxy direkt unter `http://localhost:5244/veranstaltungen` an, also ohne das Präfix `/api/`. Unter Fassung 1.0 erhielt der Lauf deshalb das Urteil `kein Backend-Kontakt`, obwohl beide Abrufe mit Status 200 beantwortet wurden. Unter Fassung 1.1 lautet das Urteil `bestanden`. Alle übrigen elf Urteile sind in beiden Fassungen gleich. Beide Datenstände sind erhalten und getrennt abgelegt, damit die Änderung nachvollziehbar bleibt.
