// ui-smoke.mjs  (Fassung 1.1)
//
// Startnachweis fuer eine einzelne Implementierung der BMAD-Vergleichsstudie.
// Laedt ausschliesslich die Startseite des bereits laufenden Frontends und
// protokolliert, ob sie rendert und ob sie dabei das Backend erreicht.
//
// Es findet KEINE Bedienung und KEINE Bewertung einzelner Merkmale statt.
// Das Kriterium steht in ui-smoke-protokoll.md.
//
// Aenderung gegenueber Fassung 1.0: Datenabrufe werden ueber den Ressourcentyp
// der Anfrage erkannt (xhr, fetch) statt ueber die Zeichenfolge "/api/" im
// Pfad. Fassung 1.0 setzte eine Pfadkonvention voraus, die nicht Teil der
// eingefrorenen Vorgabe ist, und zaehlte bei Implementierungen mit abweichender
// Routenbenennung keinen Abruf. Der Ressourcentyp ist davon unabhaengig.
//
// Aufruf:
//   node ui-smoke.mjs --lauf a_bmad_1 --url http://localhost:4200/ \
//                     --out smoke-ergebnisse
//
// Ausgabe:
//   <out>/<lauf>.json   Messwerte und Urteil
//   <out>/<lauf>.png    Screenshot der Startseite

import { chromium } from 'playwright';
import { mkdirSync, writeFileSync } from 'node:fs';
import { join } from 'node:path';

const FASSUNG = '1.1';

function arg(name, fallback = null) {
  const i = process.argv.indexOf('--' + name);
  return i !== -1 && process.argv[i + 1] ? process.argv[i + 1] : fallback;
}

const lauf = arg('lauf');
const url = arg('url', 'http://localhost:4200/');
const out = arg('out', 'smoke-ergebnisse');
const wartezeitMs = Number(arg('wartezeit', '5000'));
const timeoutMs = Number(arg('timeout', '60000'));

if (!lauf) {
  console.error('Fehlt: --lauf <name>');
  process.exit(2);
}

mkdirSync(out, { recursive: true });

const konsolenfehler = [];
const seitenfehler = [];
const datenAufrufe = [];
let dokumentStatus = null;

const istDatenAufruf = (req) => {
  const typ = req.resourceType();
  return typ === 'xhr' || typ === 'fetch';
};

const browser = await chromium.launch();
const kontext = await browser.newContext({
  viewport: { width: 1440, height: 900 },
  ignoreHTTPSErrors: true,
});
const seite = await kontext.newPage();

seite.on('console', (msg) => {
  if (msg.type() === 'error') konsolenfehler.push(msg.text().slice(0, 300));
});
seite.on('pageerror', (err) => {
  seitenfehler.push(String(err).slice(0, 300));
});
seite.on('response', (res) => {
  const req = res.request();
  if (istDatenAufruf(req)) {
    datenAufrufe.push({ url: res.url().slice(0, 200), typ: req.resourceType(), status: res.status() });
  }
});
seite.on('requestfailed', (req) => {
  if (istDatenAufruf(req)) {
    datenAufrufe.push({
      url: req.url().slice(0, 200),
      typ: req.resourceType(),
      status: 0,
      fehler: req.failure()?.errorText ?? 'unbekannt',
    });
  }
});

let ladefehler = null;
try {
  const antwort = await seite.goto(url, { waitUntil: 'domcontentloaded', timeout: timeoutMs });
  dokumentStatus = antwort ? antwort.status() : null;
  // Angular-Anwendungen holen ihre Daten erst nach dem Bootstrapping.
  // networkidle ist bei Dev-Servern mit offenem WebSocket unzuverlaessig,
  // deshalb eine feste Nachlaufzeit statt eines Wartens auf Netzruhe.
  await seite.waitForTimeout(wartezeitMs);
} catch (e) {
  ladefehler = String(e).slice(0, 300);
}

let textlaenge = 0;
try {
  const text = await seite.evaluate(() => (document.body ? document.body.innerText : ''));
  textlaenge = text.trim().length;
  await seite.screenshot({ path: join(out, lauf + '.png'), fullPage: true });
} catch (e) {
  if (!ladefehler) ladefehler = String(e).slice(0, 300);
}

await browser.close();

const anzahlDatenAufrufe = datenAufrufe.length;
const fehlerhafteAufrufe = datenAufrufe.filter((a) => a.status === 0 || a.status >= 400);

let urteil;
if (ladefehler || dokumentStatus === null || dokumentStatus >= 400) {
  urteil = 'startet nicht';
} else if (textlaenge === 0) {
  urteil = 'rendert nicht';
} else if (anzahlDatenAufrufe === 0) {
  urteil = 'kein Backend-Kontakt';
} else if (fehlerhafteAufrufe.length > 0) {
  urteil = 'Backend-Kontakt fehlerhaft';
} else if (seitenfehler.length > 0) {
  urteil = 'unbehandelter Skriptfehler';
} else {
  urteil = 'bestanden';
}

const ergebnis = {
  lauf,
  fassung: FASSUNG,
  url,
  zeitpunkt: new Date().toISOString(),
  urteil,
  dokumentStatus,
  textlaengeStartseite: textlaenge,
  datenAufrufe: anzahlDatenAufrufe,
  fehlerhafteAufrufe,
  anzahlKonsolenfehler: konsolenfehler.length,
  konsolenfehler: konsolenfehler.slice(0, 10),
  seitenfehler: seitenfehler.slice(0, 10),
  ladefehler,
  alleDatenAufrufe: datenAufrufe.slice(0, 40),
};

writeFileSync(join(out, lauf + '.json'), JSON.stringify(ergebnis, null, 2), 'utf-8');
console.log(
  lauf + ': ' + urteil +
  ' (Datenabrufe: ' + anzahlDatenAufrufe +
  ', davon fehlerhaft: ' + fehlerhafteAufrufe.length +
  ', Konsolenfehler: ' + konsolenfehler.length + ')'
);

// Rueckgabewert 0 auch bei negativem Urteil, damit die Kette weiterlaeuft.
// Das Urteil steht in der JSON-Datei.
process.exit(0);
