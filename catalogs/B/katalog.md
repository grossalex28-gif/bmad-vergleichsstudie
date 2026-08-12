# Feature-Katalog Projekt B – E-Commerce-Plattform

**Status: EINGEFROREN am 2026-08-11.** Ab hier keine Änderung mehr an ID,
Reihenfolge oder Wortlaut. Die Nummerierung B-F1 … B-F14 ist Bezugsgröße für
A2, B1, B1b, B6 und B11 (siehe Projektdokumentation
„Bachelorarbeit_Gesamtstand.md", Abschnitt 4).

**Herkunft.** Manueller Entwurf (vertikaler Schnitt durch den Funktionsumfang
des Originals, `github.com/0legKot/Godsend`) und werkzeuggestützte Ableitung
durch BMAD v6.10.0 (`bmad-document-project`, siehe `quelle.json` und
`katalog-entwurf.md` in diesem Ordner) wurden gegeneinander gehalten. Drei
Präzisierungen aus der BMAD-Ableitung wurden übernommen (Details in der
Projektdokumentation „Feature-Kataloge_final.md", Abschnitt 4).

## Katalog (14 Features)

| ID | Feature |
|---|---|
| B-F1 | Produkte werden als paginierte Liste mit fester Seitengröße angezeigt. |
| B-F2 | Kategorien sind zweistufig und werden navigierbar dargestellt. |
| B-F3 | Die Produktliste lässt sich auf eine Kategorie oder Unterkategorie einschränken. |
| B-F4 | Produkte lassen sich nach Preis und Name auf- und absteigend sortieren. |
| B-F5 | Eine Volltextsuche über Name und Beschreibung liefert passende Produkte. |
| B-F6 | Produkte besitzen kategoriespezifische Eigenschaften; die Liste lässt sich darüber filtern. |
| B-F7 | Eine Detailansicht zeigt Name, Beschreibung, Kategorie, Eigenschaften, Durchschnittsbewertung, Anzahl der Bewertungen sowie alle anbietenden Lieferanten mit ihrem jeweiligen Preis. |
| B-F8 | Ein Produkt wird mit einer Menge und einem ausgewählten Lieferanten in den Warenkorb gelegt; dasselbe Produkt bei verschiedenen Lieferanten bildet getrennte Positionen. |
| B-F9 | Der Warenkorb zeigt Positionen mit Lieferant, Einzelpreis, Menge und Gesamtsumme; Mengen sind änderbar, Positionen entfernbar. |
| B-F10 | Aus dem Warenkorb wird eine Bestellung mit Liefer- und Kontaktdaten angelegt; je Position werden Lieferant und der zum Bestellzeitpunkt gültige Preis festgeschrieben, der Warenkorb wird geleert. |
| B-F11 | Bestellungen werden mit Status, Positionen und Gesamtsumme angezeigt. |
| B-F12 | Zu einem Produkt kann eine Bewertung von 1 bis 5 mit Autorennamen abgegeben werden; eine erneute Bewertung desselben Autors ersetzt die vorherige. |
| B-F13 | Jeder Aufruf einer Produktdetailansicht erhöht einen dauerhaft gespeicherten Aufrufzähler; die Produktliste lässt sich nach diesem Zähler sortieren. |
| B-F14 | Eine Bestellung wird vollständig abgelehnt, wenn der Warenkorb leer ist, eine Position eine unzulässige Menge enthält oder ein Produkt beim gewählten Lieferanten nicht mehr angeboten wird; es entsteht keine Teilbestellung. |

## Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
|---|---|
| Authentifizierung, Registrierung, Profile, Rollen | Wie in Projekt A. |
| Lieferantenverwaltung (Anlegen, Bearbeiten, Übersicht, Detailseite) | Eigenständiger Fachbereich. Die fachlich tragende Beziehung Produkt–Lieferant–Preis ist über B-F7 bis B-F10 erhalten. |
| Artikel-/Publikationsmodul mit Schlagworten und Autoren | Zweiter eigenständiger Fachbereich, verdoppelt den Umfang. |
| Verschachtelte Kommentarbäume samt Bearbeitungs- und Löschrechten | Ersetzt durch die flache Bewertung B-F12. Die Berechtigungsprüfung hängt an der entfallenen Benutzerverwaltung. |
| Verwaltung von Sortiment und Bestellstatus durch Fachpersonal | Wie in Projekt A: dupliziert CRUD ohne neuen Qualitätsaspekt. |
| Produktvergleich | Sonderfall der Listendarstellung, geringer Zusatznutzen. |
| Unbegrenzte Kategorietiefe | Auf zwei Stufen begrenzt; Hierarchielogik bleibt erhalten, Umfang sinkt deutlich. |
| SignalR-Echtzeitbenachrichtigungen, Mehrsprachigkeit, Bildergalerie und Upload | Infrastruktur- statt Logikanteil; Mehrsprachigkeit verzerrt zudem LOC-basierte Dichtemaße. |

## Hinweis für die Eingabedatei

Dieser Katalog wird unverändert in Abschnitt 2 „Funktionsumfang" der
Eingabedatei Projekt B übernommen (`claude/Eingabespezifikation.md`). Zwei
gesonderte, noch offene Aufgaben berühren diese Nummerierung nicht, müssen
aber vor dem ersten Lauf erledigt sein: der Anfangsdatenbestand (technische
Rahmenbedingungen der Eingabedatei) und die Anpassung der
Treiberschnittstelle B in `claude/Akzeptanztests_Architektur.md` an die
Lieferantenbeziehung (B-F7 bis B-F10).
