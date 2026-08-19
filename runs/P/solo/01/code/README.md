# Werkzeugausgabe

Kleine Webanwendung zur Verwaltung von Ausleihvorgängen in einer
Werkzeugausgabe: Werkzeuge auflisten, nach Verfügbarkeit filtern,
ausleihen und zurückgeben.

## Technik

- Backend und Frontend in TypeScript, ausgeführt auf Node.js (nativ ohne
  Build-Schritt für das Backend; Node führt `.ts`-Dateien direkt aus).
- Persistenz als JSON-Datei unter `data/tools.json` — keine Datenbank.
- Kein Framework: Node-eigenes `http`-Modul im Backend, Vanilla-TypeScript
  im Browser.
- Tests mit dem in Node.js eingebauten Testrunner (`node --test`).

## Setup

```
npm install
```

## Starten

```
npm start
```

Kompiliert das Frontend nach `dist/public` und startet den Server
anschließend auf `http://localhost:3000` (Port über `PORT` änderbar,
Datendatei über `TOOLS_DATA_FILE`).

Für die Entwicklung ohne erneuten Frontend-Build:

```
npm run dev
```

## Tests

```
npm test
```

## Typprüfung & Build

```
npm run build
```

Führt die Typprüfung für Backend/Shared-Code aus und kompiliert das
Frontend-TypeScript nach `dist/public`.

## API

- `GET /api/tools` — alle Werkzeuge
- `GET /api/tools?status=available` — nur verfügbare Werkzeuge
- `GET /api/tools?status=borrowed` — nur ausgeliehene Werkzeuge
- `POST /api/tools/:id/borrow` mit `{ "name": "..." }` — ausleihen
  (409, falls bereits ausgeliehen)
- `POST /api/tools/:id/return` — zurückgeben

## Abgrenzung

Kein Login, keine Benutzerkonten/Rollen, kein E-Mail-Versand, keine
Mehrsprachigkeit, keine Vorab-Reservierungen, keine Berichte/Auswertungen
und kein Dateiupload.
