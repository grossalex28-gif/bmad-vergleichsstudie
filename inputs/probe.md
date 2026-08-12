# Projektauftrag

## 1. Kontext

Es soll eine kleine Webanwendung zur Verwaltung von Ausleihvorgängen in einer
Werkzeugausgabe entstehen. Mitarbeitende entnehmen Werkzeuge, geben sie später
zurück, und der Bestand soll jederzeit erkennbar sein. Die Anwendung wird von
wenigen Personen an einem Standort genutzt.

## 2. Funktionsumfang

1. Werkzeuge werden als Liste mit Bezeichnung, Inventarnummer und Zustand angezeigt.
2. Die Liste lässt sich auf verfügbare beziehungsweise ausgeliehene Werkzeuge einschränken.
3. Ein Werkzeug kann unter Angabe eines Namens ausgeliehen werden; es gilt danach als ausgeliehen.
4. Eine Ausleihe kann zurückgegeben werden; das Werkzeug gilt danach wieder als verfügbar.
5. Eine Ausleihe wird abgelehnt, wenn das Werkzeug bereits ausgeliehen ist.

## 3. Technische Rahmenbedingungen

Frontend und Backend werden in TypeScript auf Node.js umgesetzt. Die Daten werden
in einer Datei im Projektverzeichnis gehalten; eine Datenbank wird nicht verwendet.
Für Tests steht der in Node.js eingebaute Testrunner zur Verfügung.

## 4. Abgrenzung

Nicht Teil des Auftrags sind Benutzerkonten, Authentifizierung, Rollen,
E-Mail-Versand, Mehrsprachigkeit, Reservierungen im Voraus, Berichte und
Auswertungen sowie jede Form von Dateiupload.
