# Feature-Katalog Projekt A – Buchungssystem

**Status: EINGEFROREN am 2026-08-11.** Ab hier keine Änderung mehr an ID,
Reihenfolge oder Wortlaut. Die Nummerierung A-F1 … A-F13 ist Bezugsgröße für
A2, B1, B1b, B6 und B11 (siehe Projektdokumentation
„Bachelorarbeit_Gesamtstand.md", Abschnitt 4).

**Herkunft.** Manueller Entwurf (vertikaler Schnitt durch den Funktionsumfang
des Originals, `github.com/adamlarner/angularbooking`) und werkzeuggestützte
Ableitung durch BMAD v6.10.0 (`bmad-document-project`, siehe `quelle.json`
und `katalog-entwurf.md` in diesem Ordner) wurden gegeneinander gehalten.
Fünf Präzisierungen aus der BMAD-Ableitung wurden übernommen (Details in der
Projektdokumentation „Feature-Kataloge_final.md", Abschnitt 4).

## Katalog (13 Features)

| ID | Feature |
|---|---|
| A-F1 | Veranstaltungen werden als Liste angezeigt, jede mit Titel, Spielstätte, Datum und Uhrzeit. |
| A-F2 | Die Veranstaltungsliste lässt sich nach einem Datumsbereich filtern. |
| A-F3 | Die Veranstaltungsliste lässt sich nach Spielstätte filtern. |
| A-F4 | Eine Detailansicht zeigt Beschreibung, Dauer, Altersfreigabe, Spielstätte, Raum und Zeitpunkt. |
| A-F5 | Spielstätten enthalten Räume; ein Raum besitzt einen Sitzplan aus Reihen und Spalten, einzelne Positionen können Gang statt Sitzplatz sein. |
| A-F6 | Der Sitzplan wird grafisch dargestellt und unterscheidet freie, belegte und als Gang definierte Positionen. |
| A-F7 | Ein oder mehrere freie Sitzplätze können ausgewählt werden; belegte sind nicht auswählbar. |
| A-F8 | Jeder Veranstaltung sind Preiskategorien zugeordnet; bei der Auswahl wird je Sitzplatz eine Kategorie gewählt. |
| A-F9 | Der Gesamtpreis wird aus den Preiskategorien der gewählten Sitzplätze berechnet und angezeigt. |
| A-F10 | Eine Buchung wird mit Name und E-Mail angelegt; die gewählten Sitzplätze gelten danach als belegt. |
| A-F11 | Eine Buchung kann über eine Buchungsreferenz abgerufen und mit allen Positionen und dem Gesamtpreis angezeigt werden. |
| A-F12 | Eine Buchung kann storniert werden; die zugehörigen Sitzplätze werden wieder frei. |
| A-F13 | Eine Buchung wird vollständig abgelehnt, wenn mindestens ein gewählter Sitzplatz zwischenzeitlich belegt wurde; es entsteht keine Teilbuchung. |

## Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
|---|---|
| Authentifizierung, Registrierung, Kundenkonten, Passwortverwaltung | Querschnittsmechanismus ohne fachspezifische Aussage; zieht erhebliche Infrastruktur nach. Sicherheitsaspekte bleiben über Eingabevalidierung und Buchungslogik messbar. |
| E-Mail-Versand (Verifikation, Buchungsbestätigung) | Externe Abhängigkeit, im automatisierten Lauf nicht prüfbar. |
| Vollständiger Verwaltungsbereich (Stammdatenpflege für Spielstätten, Räume, Veranstaltungen, Termine, Preisstrategien, Kundendaten) | Dupliziert die CRUD-Logik der Fachfeatures ohne neuen Erkenntnisgewinn; in der BMAD-Ableitung acht der 19 Features. |
| Frei konfigurierbare Preisstrategien | Ersetzt durch die einfachere, fachlich nichttriviale Regel A-F8/A-F9. |
| Konflikterkennung bei gleichzeitiger Bearbeitung durch Verwaltende | An den entfallenen Verwaltungsbereich gebunden; der fachlich relevante Nebenläufigkeitsfall bleibt über A-F13 erhalten. |
| Trennung Veranstaltung / Vorstellungstermin (eine Veranstaltung mit mehreren Terminen in verschiedenen Räumen) | Vom Original belegt, hier bewusst vereinfacht: eine Veranstaltung entspricht einem Termin. Der Katalog ist Spezifikation, nicht Nachbaupflicht. |
| XSRF-Interceptor, Marketinginhalte der Startseite | An entfallene Bereiche gebunden bzw. eigenständiger Fachbereich Inhaltspflege. |

## Hinweis für die Eingabedatei

Dieser Katalog wird unverändert in Abschnitt 2 „Funktionsumfang" der
Eingabedatei Projekt A übernommen (`claude/Eingabespezifikation.md`). Der
Anfangsdatenbestand (technische Rahmenbedingungen der Eingabedatei) ist eine
gesonderte, noch offene Aufgabe und berührt diese Nummerierung nicht.
