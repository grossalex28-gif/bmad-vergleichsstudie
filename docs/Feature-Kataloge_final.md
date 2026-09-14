# Feature-Kataloge – eingefrorene Fassung

*Stand 11.08.2026 · ersetzt `claude/Feature-Kataloge_Entwurf.md` (v1.6, Entwurfsstatus)*
*Status: **EINGEFROREN am 11.08.2026.** Ab hier keine Änderung mehr an ID, Reihenfolge oder Wortlaut. Die beiden Punkte aus Abschnitt 5 betreffen die Eingabedatei bzw. die Akzeptanztest-Architektur, nicht die Katalog-Nummerierung; beide sind inzwischen erledigt (siehe unten) — die tatsächliche Seed-Datei aus 5.1 entsteht erst beim Bau des Seed-Repositorys.*

**Frei­gegebene Katalogdateien.** Die maßgebliche, maschinenlesbare Fassung liegt im Harness-Arbeitsverzeichnis unter `catalogs/A/katalog.md` und `catalogs/B/katalog.md` (inhaltlich identisch mit Abschnitt 2/3 dieses Dokuments), mit Freeze-Metadaten in `katalog.meta.json`:

| Projekt | Datei | SHA-256 |
|---|---|---|
| A | `catalogs/A/katalog.md` | `4b52e7212916d0a54fd0b575ad075fb93c73968c55f6e9b7c9497292b4f85097` |
| B | `catalogs/B/katalog.md` | `b2fa337b9c10ebee465053c7c95626f0aa3852a3e21b1d32e5cfbcb9ed5c5cbe` |

Diese Hashes sind Kontrollwerte für die Katalogdateien selbst, nicht für die spätere Eingabedatei (H2) — deren Hash entsteht erst, wenn der Katalog nach `claude/Eingabespezifikation.md` in Abschnitt 2 der Eingabedatei eingebettet wird (offener Punkt in Abschnitt 6).

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

## 2. Projekt A – Buchungssystem (13 Features) — EINGEFROREN

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

## 3. Projekt B – E-Commerce-Plattform (14 Features) — EINGEFROREN

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

*Bewertungsanzeige zusammengeführt.* Die getrennten Features „Bewertung abgeben“ und „Durchschnitt anzeigen“ waren zwei Katalogplätze für einen fachlichen Vorgang. Zusammengelegt in B-F7 und B-F12, wobei B-F12 um die von BMAD belegte Ersetzungsregel ergänzt wurde.

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

**Für die Arbeit:** Der manuelle Entwurf wurde durch eine unabhängige, werkzeuggestützte Ableitung validiert. Von 19 bzw. 23 abgeleiteten Anforderungen deckten sich die Kernbereiche vollständig; fünf Präzisierungen wurden übernommen, davon zwei fachlich neu. Das ist ein belastbarer Konstruktvaliditätsnachweis für die Kataloge und zugleich ein deskriptiver Befund über BMADs Anforderungsableitung.

---

## 5. Zwei nachgelagerte Punkte — berühren die Nummerierung nicht, beide erledigt

Diese Kataloge (Abschnitt 2/3) sind mit der Nummerierung A-F1…A-F13 und B-F1…B-F14 eingefroren. Die beiden folgenden Punkte waren eigenständige Aufgaben auf dem Weg zur Eingabedatei (H2) und zum Seed-Repository — sie ändern weder Feature-Wortlaut noch -Nummerierung und waren deshalb kein Freeze-Hindernis. Beide sind inzwischen erledigt.

### 5.1 Anfangsdatenbestand — Inhalt bestätigt (11.08.2026)

Beide Kataloge setzen voraus, dass Daten vorhanden sind: A-F1 zeigt Veranstaltungen an, B-F1 zeigt Produkte an. **Keiner der Kataloge enthält ein Feature, das diese Daten anlegt** — die Verwaltungsbereiche sind ausgeschlossen. Damit ist A-F1 gegen eine leere Anwendung nicht prüfbar, und die Akzeptanztests haben keinen definierten Ausgangszustand.

Der vollständige Inhalt steht in `claude/Anfangsdatenbestand_Spezifikation.md` — für Projekt A zwei Spielstätten mit je zwei Räumen unterschiedlicher Sitzplan-Geometrie, sechs Veranstaltungen über fünf Wochen, zwei Preiskategorien je Veranstaltung, drei bereits belegte Sitzplätze bei einer Veranstaltung; für Projekt B ein zweistufiger Kategoriebaum, kategoriespezifische Eigenschaften, 14 Produkte, vier Lieferanten, drei Produkte mit zwei Lieferanten zu unterschiedlichen Preisen. Nach Selbstprüfung (Konsistenz, vollständige Feature-Abdeckung, kein Änderungsbedarf) am 11.08.2026 bestätigt. Arbeitsentwürfe als JSON liegen unter `HarnessTesting/anfangsdatenbestand/{A,B}/daten.json` (Format nicht bindend, siehe Hinweis in den Dateien).

**Noch offen:** Übernahme des bestätigten Inhalts in die tatsächliche Seed-Datei im vom Backend erwarteten Format (Anleitung dazu in `claude/Seed-Repository_Anleitung.md`) und Aufnahme des Hashes in `config.messlauf.json`. Das Dateiformat wird bewusst nicht in der Eingabedatei vorgegeben — nur der Hinweis, dass eine solche Datei existiert und beim Start geladen wird.

### 5.2 Treiberschnittstelle B — erledigt (11.08.2026)

Die Treiberschnittstelle für Projekt B in `claude/Akzeptanztests_Architektur.md` wurde an die Lieferantenbeziehung angepasst: `LegeInWarenkorb` erhält `lieferantId`, `HoleProdukt` liefert Lieferanten mit Preisen sowie Bewertungs- und Aufrufkennzahlen, Warenkorbpositionen tragen den Lieferanten, `ÄndereMenge`/`EntferneAusWarenkorb` adressieren jetzt die Position statt des Produkts, `sortierung` akzeptiert den Aufrufzähler. Die Treiberschnittstelle für Projekt A blieb unverändert bis auf zwei zusätzliche Felder bei `HoleVeranstaltung`.

---

## 6. Einfrier-Checkliste

- [x] **Katalog-Nummerierung und -Wortlaut eingefroren** (11.08.2026) — `catalogs/A/katalog.md` und `catalogs/B/katalog.md` im Harness-Arbeitsverzeichnis, SHA-256 in `katalog.meta.json` je Projekt, Prüflisten (`pruefliste.md`) mit Anonymisierung, Zuschnitt, Umfang und Abgleich bestätigt.
- [x] Anfangsdatenbestand-**Inhalt** je Projekt festgelegt und bestätigt (5.1) — `claude/Anfangsdatenbestand_Spezifikation.md`
- [ ] Anfangsdatenbestand als tatsächliche **Datei im Seed-Repository** abgelegt, Format je Backend gewählt, Hash in `config.messlauf.json` — steht noch aus, siehe `claude/Seed-Repository_Anleitung.md`
- [x] Treiberschnittstelle B in `claude/Akzeptanztests_Architektur.md` angepasst (5.2)
- [ ] Eingabedateien A und B nach `claude/Eingabespezifikation.md` erstellt: Projektkontext ohne Eigennamen, den eingefrorenen Katalog als nummerierte Liste übernehmen, technische Rahmenbedingungen mit konkreten Versionen, Abgrenzung
- [ ] Eingabedateien auf Implementierungsdetails geprüft (keine Endpunkte, keine Klassennamen, kein Schema)
- [ ] SHA-256 beider Eingabedateien gebildet und in `config.messlauf.json` hinterlegt
- [x] Testfälle je Feature ausformuliert, für A-F13 und B-F14 zusätzlich der negative Fall, für B-F8–B-F10 zusätzlich der Mehr-Lieferanten-Fall — **beide Projekte als Entwurf erledigt** (`claude/Akzeptanztestfaelle_ProjektA.md`, `claude/Akzeptanztestfaelle_ProjektB.md`, Stand 19.08.2026). Noch offen: Umsetzung als xUnit-Code und Einfrieren gegen die Treiberschnittstelle, plus die in beiden Dokumenten benannten Klärungsbedarfe (Fehlerformat, Sortierschlüssel bei Mehr-Lieferanten-Produkten, Struktur von Liefer-/Kontaktdaten)
- [ ] `catalogs/` aus dem Arbeitsbereich der Messläufe fernhalten — dort liegen Analysedokumente des Originals sowie die Entwürfe (`katalog-entwurf.md`, `quelle.json`, `lauf.jsonl`)

**Ab dem Einfrieren keine Änderung mehr.** Die Nummerierung A-F1…A-F13 und B-F1…B-F14 ist Bezugsgröße für A2.
