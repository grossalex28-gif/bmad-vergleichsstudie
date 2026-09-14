# Startnachweis der Oberflächen – Prüfprotokoll

*Festgelegt am 14.09.2026, vor der ersten Ausführung. Ergänzende Prüfung zur Schließung der in Abschnitt 6.6 benannten Lücke, dass die Oberflächen der zwölf Implementierungen bislang nicht im Betrieb geprüft wurden. Kein Bestandteil des eingefrorenen Metrikkatalogs und keine nachträgliche Änderung an A1 oder A2.*

## 1. Zweck und Abgrenzung

Geprüft wird ausschließlich, ob die Oberfläche einer Implementierung startet, rendert und dabei ihr eigenes Backend erreicht. Nicht geprüft werden Bedienbarkeit, Gestaltung, einzelne Katalogmerkmale und das Verhalten nach einer Benutzereingabe. Die Prüfung verändert keinen Datenbestand, weil sie nur die Startseite lädt.

Die Ergebnisse werden neben A2 berichtet und führen nicht zu einer Neuberechnung von A1 oder A2. Weicht ein Befund von einer bereits berichteten A2-Bewertung ab, wird die Abweichung benannt und nicht aufgelöst.

## 2. Ablauf je Implementierung

1. Prüfen, dass der Container `sqlserver-bachelor` läuft und die Datenbank `<projekt>_<ansatz>_<wiederholung>` existiert.
2. Backend mit `dotnet run` gegen diese Datenbank starten, auf dem Port, den die Proxy-Konfiguration des jeweiligen Frontends erwartet.
3. Warten, bis das Backend auf der Wurzel-URL antwortet, höchstens 90 Sekunden.
4. Frontend mit `ng serve` auf Port 4200 starten und warten, bis es antwortet, höchstens 180 Sekunden.
5. Startseite `http://localhost:4200/` in einem headless betriebenen Chromium laden, nach dem Laden fünf Sekunden nachlaufen lassen, Screenshot in voller Seitenhöhe ablegen.
6. Beide Prozesse beenden.

Aufgezeichnet werden der HTTP-Status des Dokuments, die Textlänge der gerenderten Seite, alle Anfragen mit `/api/` im Pfad samt Status, alle Konsolenfehler und alle unbehandelten Skriptfehler.

## 3. Urteil

Das Urteil ist einstufig und wird aus den aufgezeichneten Werten abgeleitet, nicht durch Betrachtung vergeben.

| Urteil | Bedingung |
|---|---|
| bestanden | Dokument mit Status unter 400 geladen, Seite enthält Text, mindestens ein Aufruf mit `/api/`, kein solcher Aufruf mit Status 0 oder ab 400, kein unbehandelter Skriptfehler |
| startet nicht | Dokument nicht geladen oder Status ab 400 |
| rendert nicht | Dokument geladen, Seite bleibt ohne Text |
| kein Backend-Kontakt | Seite rendert, ruft beim Laden aber keinen `/api/`-Endpunkt auf |
| Backend-Kontakt fehlerhaft | mindestens ein `/api/`-Aufruf mit Status 0 oder ab 400 |
| unbehandelter Skriptfehler | sonst, aber mit unbehandeltem Fehler im Seitenkontext |

Konsolenfehler werden gezählt und aufgeführt, gehen aber nicht in das Urteil ein, weil sie in Entwicklungsservern regelmäßig ohne funktionale Bedeutung auftreten.

Ein Urteil `kein Backend-Kontakt` ist kein Fehlerbefund. Es bedeutet, dass die Startseite der jeweiligen Implementierung keine Daten nachlädt, und ist als solches zu berichten.

## 4. Grenzen dieser Prüfung

Sie findet nach Abschluss der Hauptauswertung statt und damit in Kenntnis der Ergebnisse. Sie erfasst nur die Startseite. Sie sagt nichts darüber aus, ob die weiteren Ansichten funktionieren. Die verwendeten Abhängigkeiten werden zum Prüfzeitpunkt installiert, sofern sie im Laufverzeichnis fehlen, weshalb ein Frontend, das bei der Erhebung noch baute, an einer inzwischen veränderten Paketversion scheitern kann. Ein solcher Fall ist als Befund über die Prüfung und nicht über die Implementierung zu werten.

## 5. Ablage

`smoke-ergebnisse/<lauf>.json` mit den Messwerten, `smoke-ergebnisse/<lauf>.png` mit dem Screenshot, `smoke-ergebnisse/uebersicht.csv` mit einer Zeile je Lauf. Die Protokolle der beiden Prozesse liegen daneben als `.backend.log` und `.frontend.log`.
