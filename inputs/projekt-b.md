# Projektauftrag

## 1. Kontext

Es soll eine Webanwendung für den Online-Handel entstehen, über die Kunden ein
Produktsortiment durchsuchen, Artikel in einen Warenkorb legen und daraus eine
Bestellung auslösen können. Der Einstieg ist eine Produktliste, die sich über
Kategorien, eine Suche, Sortierungen und Filter eingrenzen lässt.

Das Sortiment ist in zwei Ebenen gegliedert, und jede Unterkategorie bringt eigene
Produkteigenschaften mit, nach denen sich die Liste einschränken lässt. Ein Produkt
wird von einem oder mehreren Lieferanten zu jeweils eigenen Preisen angeboten,
weshalb die Wahl des Lieferanten Teil der Kaufentscheidung ist und von der
Warenkorbposition bis zur abgeschlossenen Bestellung erhalten bleiben muss. Der
Preis einer Bestellposition wird zum Bestellzeitpunkt festgeschrieben und ändert
sich danach nicht mehr, auch wenn der Lieferant seinen Preis anpasst.

Kunden können Produkte bewerten, und die Anwendung hält fest, wie oft ein Produkt
aufgerufen wurde, sodass sich die Liste nach Beliebtheit ordnen lässt. Genutzt wird
die Anwendung von Kunden ohne Anmeldung, die einen Kauf in einem Durchgang
abschließen. Die Pflege des Sortiments findet außerhalb der Anwendung statt,
weshalb kein Verwaltungsbereich benötigt wird.

## 2. Funktionsumfang

B-F1   Produkte werden als paginierte Liste mit fester Seitengröße angezeigt.
B-F2   Kategorien sind zweistufig und werden navigierbar dargestellt.
B-F3   Die Produktliste lässt sich auf eine Kategorie oder Unterkategorie einschränken.
B-F4   Produkte lassen sich nach Preis und Name auf- und absteigend sortieren.
B-F5   Eine Volltextsuche über Name und Beschreibung liefert passende Produkte.
B-F6   Produkte besitzen kategoriespezifische Eigenschaften; die Liste lässt sich darüber filtern.
B-F7   Eine Detailansicht zeigt Name, Beschreibung, Kategorie, Eigenschaften, Durchschnittsbewertung, Anzahl der Bewertungen sowie alle anbietenden Lieferanten mit ihrem jeweiligen Preis.
B-F8   Ein Produkt wird mit einer Menge und einem ausgewählten Lieferanten in den Warenkorb gelegt; dasselbe Produkt bei verschiedenen Lieferanten bildet getrennte Positionen.
B-F9   Der Warenkorb zeigt Positionen mit Lieferant, Einzelpreis, Menge und Gesamtsumme; Mengen sind änderbar, Positionen entfernbar.
B-F10  Aus dem Warenkorb wird eine Bestellung mit Liefer- und Kontaktdaten angelegt; je Position werden Lieferant und der zum Bestellzeitpunkt gültige Preis festgeschrieben, der Warenkorb wird geleert.
B-F11  Bestellungen werden mit Status, Positionen und Gesamtsumme angezeigt.
B-F12  Zu einem Produkt kann eine Bewertung von 1 bis 5 mit Autorennamen abgegeben werden; eine erneute Bewertung desselben Autors ersetzt die vorherige.
B-F13  Jeder Aufruf einer Produktdetailansicht erhöht einen dauerhaft gespeicherten Aufrufzähler; die Produktliste lässt sich nach diesem Zähler sortieren.
B-F14  Eine Bestellung wird vollständig abgelehnt, wenn der Warenkorb leer ist, eine Position eine unzulässige Menge enthält oder ein Produkt beim gewählten Lieferanten nicht mehr angeboten wird; es entsteht keine Teilbestellung.

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

- Benutzerkonten, Registrierung, Anmeldung, Profile und Rollen
- Anlegen, Bearbeiten oder Auflisten von Lieferanten
- Ein Verwaltungsbereich zur Pflege des Sortiments oder des Bestellstatus
- Ein Artikel- oder Redaktionsmodul mit Schlagworten und Autoren
- Verschachtelte Kommentare zu Produkten
- Ein Produktvergleich
- Kategorien mit mehr als zwei Ebenen
- Zahlungs- und Versandabwicklung
- Echtzeitbenachrichtigungen, Mehrsprachigkeit, Bildergalerien und Dateiupload
- Deployment, Containerisierung und Betrieb
