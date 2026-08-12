# Feature-Kataloge – einfrierbereite Fassung

*Stand 11.08.2026 · ersetzt `claude/Feature-Kataloge_Entwurf.md` (v1.6, Entwurfsstatus)*
*Status: **einfrierbereit** nach Klärung der zwei offenen Punkte in Abschnitt 5*

---

## 1. Herkunft und Vorgehen

Die Kataloge entstanden aus zwei unabhängigen Quellen und wurden gegeneinander gehalten:

**Quelle 1 – manueller Entwurf.** Vertikaler Schnitt durch den Funktionsumfang der Originale, mit begründeter Ausschlussliste (`claude/Feature-Kataloge_Entwurf.md`).

**Quelle 2 – werkzeuggestützte Ableitung.** BMAD v6.10.0 (`bmad-document-project`) hat beide Repositories analysiert und je einen Katalogentwurf erzeugt. Einmaliger Vorbereitungsschritt außerhalb der Erhebung; die geklonten Originale wurden anschließend gelöscht.

| Projekt | Repository | Commit | Dauer | Kosten | Ergebnis |
|---|---|---|---|---|---|
| A | `adamlarner/angularbooking` | `51f5283…` | 3,7 min | 1,11 USD | 19 Features |
| B | `0legKot/Godsend` | `dc97055…` | 5,1 min | 1,78 USD | 23 Features |

**Befund zur Ableitung selbst.** Beide Entwürfe hielten die Anonymisierungsregel vollständig ein — kein Endpunktpfad, kein Klassen- oder Dateiname, kein Framework, kein Datenbankdetail, kein Projektname. BMAD extrahiert jedoch **vollständig statt zugeschnitten**: Es schloss nur aus, was der Prompt namentlich als Ausschlusskandidat nannte, und hat keinen Begriff von einem Messbudget. Der Zuschnitt bleibt damit eine Entscheidung des Untersuchenden.

Wertvoll waren die *Beobachtungen* beider Läufe: BMAD identifizierte in Projekt B toten Code (leere Platzhalter im Frontend, serverseitige Funktionen, die absichtlich einen Laufzeitfehler werfen, eine auskommentierte gewichtsbasierte Bestellvariante) und in Projekt A offene Fragen zur Nebenläufigkeit beim Sitzplatzverkauf. Beides floss in die Schärfung der Fehlerfälle ein.

---

## 2. Projekt A – Buchungssystem (13 Features)

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

**Gegenüber dem Entwurf geändert:** A-F4 um Dauer und Altersfreigabe ergänzt (beides in beiden Quellen belegt, kostet keine zusätzliche Story). A-F13 um die Atomizitätsforderung geschärft — BMADs Beobachtung, dass im Original unklar bleibt, ob der Konflikt aktiv verhindert oder nur angezeigt wird, macht die explizite Formulierung nötig. A-F11 nennt den Gesamtpreis ausdrücklich, damit A-F9 auch nach Abschluss prüfbar ist.

### Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
|---|---|
| Authentifizierung, Registrierung, Kundenkonten, Passwortverwaltung | Querschnittsmechanismus ohne fachspezifische Aussage; zieht erhebliche Infrastruktur nach. Sicherheitsaspekte bleiben über Eingabevalidierung und Buchungslogik messbar. |
| E-Mail-Versand (Verifikation, Buchungsbestätigung) | Externe Abhängigkeit, im automatisierten Lauf nicht prüfbar. |
| Vollständiger Verwaltungsbereich (Stammdatenpflege für Spielstätten, Räume, Veranstaltungen, Termine, Preisstrategien, Kundendaten) | Dupliziert die CRUD-Logik der Fachfeatures ohne neuen Erkenntnisgewinn; in der BMAD-Ableitung acht der 19 Features. |
| Frei konfigurierbare Preisstrategien | Ersetzt durch die einfachere, fachlich nichttriviale Regel A-F8/A-F9. |
| Konflikterkennung bei gleichzeitiger Bearbeitung durch Verwaltende | An den entfallenen Verwaltungsbereich gebunden; der fachlich relevante Nebenläufigkeitsfall bleibt über A-F13 erhalten. |
| Trennung Veranstaltung / Vorstellungstermin (eine Veranstaltung mit mehreren Terminen in verschiedenen Räumen) | Vom Original belegt, hier bewusst vereinfacht: eine Veranstaltung entspricht einem Termin. Der Katalog ist Spezifikation, nicht Nachbaupflicht. |
| XSRF-Interceptor, Marketinginhalte der Startseite | An entfallene Bereiche gebunden bzw. eigenständiger Fachbereich Inhaltspflege. |

---

## 3. Projekt B – E-Commerce-Plattform (14 Features)

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

**Gegenüber dem Entwurf geändert.** Drei Eingriffe, jeder aus der BMAD-Ableitung begründet:

*Lieferantenbeziehung aufgenommen* (B-F7, B-F8, B-F9, B-F10, B-F14). BMAD hat als zentrale Fachstruktur herausgearbeitet, dass ein Produkt von mehreren Lieferanten zu unterschiedlichen Preisen angeboten wird und die Warenkorbposition den Lieferanten trägt. Das ist die architektonisch aufschlussreichste Anforderung des Projekts: Sie erzwingt eine n:m-Beziehung mit Attribut, macht die Warenkorbposition zusammengesetzt und liefert mit dem Festschreiben des Preises zum Bestellzeitpunkt eine echte Konsistenzregel. Aufgenommen wird ausdrücklich **nur die Beziehung**, nicht die Lieferantenverwaltung — kein Anlegen, kein Bearbeiten, keine Lieferantenübersicht.

*Bewertungsanzeige zusammengeführt.* Die getrennten Features „Bewertung abgeben" und „Durchschnitt anzeigen" waren zwei Katalogplätze für einen fachlichen Vorgang. Zusammengelegt in B-F7 und B-F12, wobei B-F12 um die von BMAD belegte Ersetzungsregel ergänzt wurde.

*Aufrufzähler aufgenommen* (B-F13). In beiden Quellen des Originals belegt, im manuellen Entwurf übersehen. Fachlich klein, architektonisch interessant: eine Schreiboperation auf dem Lesepfad, die Konsistenz- und Nebenläufigkeitsfragen aufwirft.

### Bewusst ausgeschlossen

| Ausgeschlossen | Begründung |
|---|---|
| Authentifizierung, Registrierung, Profile, Rollen | Wie in Projekt A. |
| Lieferantenverwaltung (Anlegen, Bearbeiten, Übersicht, Detailseite) | Eigenständiger Fachbereich. Die fachlich tragende **Beziehung** Produkt–Lieferant–Preis ist über B-F7 bis B-F10 erhalten. |
| Artikel-/Publikationsmodul mit Schlagworten und Autoren | Zweiter eigenständiger Fachbereich, verdoppelt den Umfang. |
| Verschachtelte Kommentarbäume samt Bearbeitungs- und Löschrechten | Ersetzt durch die flache Bewertung B-F12. Die Berechtigungsprüfung hängt an der entfallenen Benutzerverwaltung. |
| Verwaltung von Sortiment und Bestellstatus durch Fachpersonal | Wie in Projekt A: dupliziert CRUD ohne neuen Qualitätsaspekt. |
| Produktvergleich | Sonderfall der Listendarstellung, geringer Zusatznutzen. |
| Unbegrenzte Kategorietiefe | Auf zwei Stufen begrenzt; Hierarchielogik bleibt erhalten, Umfang sinkt deutlich. |
| SignalR-Echtzeitbenachrichtigungen, Mehrsprachigkeit, Bildergalerie und Upload | Infrastruktur- statt Logikanteil; Mehrsprachigkeit verzerrt zudem LOC-basierte Dichtemaße. |

---

## 4. Was die Ableitung gebracht hat

| BMAD-Befund | Entscheidung |
|---|---|
| B: Produkt–Lieferant–Preis als n:m-Beziehung, Lieferant je Warenkorbposition | **übernommen**, ohne Lieferantenverwaltung |
| B: Aufrufzähler für Beliebtheitssortierung | **übernommen** |
| B: Bewertung ersetzt vorherige Bewertung desselben Autors | **übernommen** |
| A: Dauer und Altersfreigabe an der Veranstaltung | **übernommen** |
| A: Unklarheit bei gleichzeitiger Sitzplatzbuchung | **als Schärfung übernommen** (A-F13 Atomizität) |
| A: Trennung Veranstaltung / Vorstellungstermin | verworfen — bewusste Vereinfachung, dokumentiert |
| A: Optimistische Sperre bei Verwaltungsänderungen | verworfen — an entfallenen Verwaltungsbereich gebunden |
| A/B: Verwaltungsbereiche, Artikelmodul, Lieferantenverwaltung, Kommentarbäume | verworfen — Ausschlussbegründungen des Entwurfs tragen weiterhin |

**Für die Arbeit:** Der manuelle Entwurf wurde durch eine unabhängige, werkzeuggestützte Ableitung validiert. Von 19 bzw. 23 abgeleiteten Anforderungen deckten sich die Kernbereiche vollständig; fünf Präzisierungen wurden übernommen, davon zwei fachlich neue. Das ist ein belastbarer Konstruktvaliditätsnachweis für die Kataloge und zugleich ein deskriptiver Befund über BMADs Anforderungsableitung.

---

## 5. Zwei Punkte, die vor dem Einfrieren zu entscheiden sind

### 5.1 Anfangsdatenbestand — bisher ungeklärt

Beide Kataloge setzen voraus, dass Daten vorhanden sind: A-F1 zeigt Veranstaltungen an, B-F1 zeigt Produkte an. **Keiner der Kataloge enthält ein Feature, das diese Daten anlegt** — die Verwaltungsbereiche sind ausgeschlossen. Damit ist A-F1 gegen eine leere Anwendung nicht prüfbar, und die Akzeptanztests haben keinen definierten Ausgangszustand.

Das ist keine Kleinigkeit: Ohne Festlegung erfindet jede der zwölf Implementierungen ihren eigenen Weg, an Daten zu kommen, und A1 wird unmessbar.

**Vorschlag:** Der Anfangsdatenbestand kommt als Datei im **Seed-Repository** und wird in den technischen Rahmenbedingungen der Eingabedatei verlangt — nicht als Feature, weil er nicht nutzersichtbar ist. Formulierung etwa:

> Im Projektverzeichnis liegt eine Datei mit dem Anfangsdatenbestand. Die Anwendung lädt diesen Bestand beim Start, sofern noch keine Daten vorhanden sind.

Der Bestand ist in allen zwölf Läufen identisch, geht über die Ausschlussliste nicht in die Metriken ein und gibt den Akzeptanztests einen definierten Ausgangszustand. Zu erstellen: je Projekt eine Datendatei mit genug Substanz für alle Testfälle (Projekt A: mindestens zwei Spielstätten, je zwei Räume mit unterschiedlichen Sitzplänen, mehrere Veranstaltungen über einen Datumsbereich, Preiskategorien; Projekt B: zwei Oberkategorien mit je zwei Unterkategorien, kategoriespezifische Eigenschaften, mehrere Produkte, mindestens zwei Lieferanten mit unterschiedlichen Preisen für dasselbe Produkt).

**Wichtig:** Das Dateiformat darf nicht vorgegeben werden — es wäre eine Entwurfsvorgabe. Vorgegeben wird der *Inhalt* als fachliche Aufzählung, nicht das Schema.

### 5.2 Treiberschnittstelle B muss angepasst werden

Die in `claude/Akzeptanztests_Architektur.md` entworfene Treiberschnittstelle für Projekt B passt nicht mehr zur Lieferantenbeziehung. Zu ändern:

| Operation | Änderung |
|---|---|
| `LegeInWarenkorb(sitzung, produktId, menge)` | → zusätzlicher Parameter `lieferantId` |
| `HoleProdukt(id)` | liefert zusätzlich Lieferanten mit Preisen, Durchschnittsbewertung, Bewertungsanzahl, Aufrufzähler |
| `HoleWarenkorb(sitzung)` | Positionen tragen zusätzlich den Lieferanten |
| `ListeProdukte(seite, kategorieId?, sortierung?, eigenschaftsfilter?)` | `sortierung` akzeptiert zusätzlich den Aufrufzähler |
| `ÄndereMenge(...)`, `EntferneAusWarenkorb(...)` | adressieren die Position, nicht das Produkt |

Die Treiberschnittstelle für Projekt A bleibt unverändert; nur `HoleVeranstaltung(id)` liefert zwei Felder mehr.

---

## 6. Einfrier-Checkliste

- [ ] Anfangsdatenbestand je Projekt festgelegt und als Datei im Seed-Repository abgelegt (5.1)
- [ ] Treiberschnittstelle B in `claude/Akzeptanztests_Architektur.md` angepasst (5.2)
- [ ] Eingabedateien A und B nach `claude/Eingabespezifikation.md` erstellt: Projektkontext ohne Eigennamen, Katalog als nummerierte Liste, technische Rahmenbedingungen mit konkreten Versionen, Abgrenzung
- [ ] Eingabedateien auf Implementierungsdetails geprüft (keine Endpunkte, keine Klassennamen, kein Schema)
- [ ] SHA-256 beider Eingabedateien gebildet und in `config.messlauf.json` hinterlegt
- [ ] Testfälle je Feature ausformuliert, für A-F13 und B-F14 zusätzlich der negative Fall
- [ ] `catalogs/` aus dem Arbeitsbereich der Messläufe fernhalten — dort liegen Analysedokumente des Originals

**Ab dem Einfrieren keine Änderung mehr.** Die Nummerierung A-F1…A-F13 und B-F1…B-F14 ist Bezugsgröße für A2, B1, B1b, B6 und B11.
