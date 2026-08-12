# Projektauftrag

## 1. Kontext

Es soll eine Webanwendung entstehen, über die Besucher Karten für Veranstaltungen
an mehreren Spielstätten buchen können. Die Anwendung zeigt das laufende Programm,
führt von dort zur einzelnen Veranstaltung und erlaubt die Auswahl konkreter
Sitzplätze auf einem grafisch dargestellten Sitzplan.

Eine Spielstätte verfügt über mehrere Räume, deren Sitzpläne sich in Größe und
Gangführung unterscheiden. Zu jeder Veranstaltung gehören Preiskategorien, aus
denen sich der Preis einer Auswahl ergibt. Eine abgeschlossene Buchung belegt die
gewählten Plätze, lässt sich über eine Buchungsreferenz wieder aufrufen und kann
storniert werden, wodurch die Plätze erneut frei werden. Da mehrere Besucher
gleichzeitig auf denselben Sitzplan zugreifen, muss die Anwendung damit umgehen,
dass ein ausgewählter Platz zwischen Auswahl und Abschluss von jemand anderem
belegt wird.

Genutzt wird die Anwendung von Besuchern ohne Anmeldung, die eine Buchung in einem
Durchgang abschließen. Die Pflege der Stammdaten findet außerhalb der Anwendung
statt, weshalb kein Verwaltungsbereich benötigt wird.

## 2. Funktionsumfang

A-F1   Veranstaltungen werden als Liste angezeigt, jede mit Titel, Spielstätte, Datum und Uhrzeit.
A-F2   Die Veranstaltungsliste lässt sich nach einem Datumsbereich filtern.
A-F3   Die Veranstaltungsliste lässt sich nach Spielstätte filtern.
A-F4   Eine Detailansicht zeigt Beschreibung, Dauer, Altersfreigabe, Spielstätte, Raum und Zeitpunkt.
A-F5   Spielstätten enthalten Räume; ein Raum besitzt einen Sitzplan aus Reihen und Spalten, einzelne Positionen können Gang statt Sitzplatz sein.
A-F6   Der Sitzplan wird grafisch dargestellt und unterscheidet freie, belegte und als Gang definierte Positionen.
A-F7   Ein oder mehrere freie Sitzplätze können ausgewählt werden; belegte sind nicht auswählbar.
A-F8   Jeder Veranstaltung sind Preiskategorien zugeordnet; bei der Auswahl wird je Sitzplatz eine Kategorie gewählt.
A-F9   Der Gesamtpreis wird aus den Preiskategorien der gewählten Sitzplätze berechnet und angezeigt.
A-F10  Eine Buchung wird mit Name und E-Mail angelegt; die gewählten Sitzplätze gelten danach als belegt.
A-F11  Eine Buchung kann über eine Buchungsreferenz abgerufen und mit allen Positionen und dem Gesamtpreis angezeigt werden.
A-F12  Eine Buchung kann storniert werden; die zugehörigen Sitzplätze werden wieder frei.
A-F13  Eine Buchung wird vollständig abgelehnt, wenn mindestens ein gewählter Sitzplatz zwischenzeitlich belegt wurde; es entsteht keine Teilbuchung.

## 3. Technische Rahmenbedingungen

- Frontend: Angular 22.1 mit TypeScript 6.0 auf Node.js 24.19 LTS
- Backend: ASP.NET Core 10.0 mit C# 14 auf .NET 10.0
- Persistenz: Entity Framework Core 10.0 mit Microsoft SQL Server
- Tests: xUnit v3 im Backend, Vitest im Frontend
- Paketverwaltung: npm im Frontend, NuGet im Backend

Ein vorbereitetes Projektgerüst mit diesen Festlegungen liegt im Arbeitsverzeichnis
bereit. Die Verbindungszeichenfolge zur Datenbank steht zur Laufzeit in der
Umgebungsvariablen `ConnectionStrings__Default`. Im Projektverzeichnis liegt eine
Datei mit dem Anfangsdatenbestand. Die Anwendung lädt diesen Bestand beim Start,
sofern noch keine Daten vorhanden sind.

## 4. Nicht Teil des Auftrags

- Benutzerkonten, Registrierung, Anmeldung und Rollen
- Versand von E-Mails jeder Art
- Ein Verwaltungsbereich zur Pflege von Spielstätten, Räumen, Veranstaltungen oder Preisen
- Frei konfigurierbare Preisstrategien
- Mehrere Vorstellungstermine je Veranstaltung, da eine Veranstaltung genau einem Termin entspricht
- Zahlungsabwicklung
- Mehrsprachigkeit, Bildergalerien und Dateiupload
- Deployment, Containerisierung und Betrieb
