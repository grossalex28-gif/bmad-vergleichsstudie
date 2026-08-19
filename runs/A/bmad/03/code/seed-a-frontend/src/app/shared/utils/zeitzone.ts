/**
 * Extrahiert den UTC-Offset (z. B. "+02:00") aus einem ISO-8601-Zeitstempel, damit Angulars
 * DatePipe die Spielstätten-lokale Zeit rendert statt der Zeitzone des Besucher-Browsers.
 * Angulars DatePipe-timezone-Parameter versteht nur feste Offsets, keine IANA-Zonennamen
 * (z. B. "Europe/Berlin") — der Offset kommt vom Backend bereits korrekt (DST-bewusst) berechnet.
 */
export function spielstaettenOffset(zeitpunktIso: string): string {
  const treffer = /(Z|[+-]\d{2}:\d{2})$/.exec(zeitpunktIso);
  if (!treffer) {
    return '+00:00';
  }
  return treffer[1] === 'Z' ? '+00:00' : treffer[1];
}
