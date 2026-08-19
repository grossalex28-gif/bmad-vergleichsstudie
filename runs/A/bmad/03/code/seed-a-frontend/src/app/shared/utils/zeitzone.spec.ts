import { spielstaettenOffset } from './zeitzone';

describe('spielstaettenOffset', () => {
  it('extrahiert einen positiven Offset aus dem Zeitstempel', () => {
    expect(spielstaettenOffset('2026-09-05T19:30:00+02:00')).toBe('+02:00');
  });

  it('extrahiert einen negativen Offset aus dem Zeitstempel', () => {
    expect(spielstaettenOffset('2026-01-15T19:30:00-05:00')).toBe('-05:00');
  });

  it('behandelt das Z-Suffix als UTC-Offset', () => {
    expect(spielstaettenOffset('2026-09-05T19:30:00Z')).toBe('+00:00');
  });

  it('faellt bei fehlendem Offset auf UTC zurueck', () => {
    expect(spielstaettenOffset('2026-09-05T19:30:00')).toBe('+00:00');
  });
});
