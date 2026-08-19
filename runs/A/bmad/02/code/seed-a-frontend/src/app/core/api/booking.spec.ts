import { parseSeatCode } from './booking';

describe('parseSeatCode', () => {
  it('parst einen einstelligen Sitzplatz-Code in RowLabel und ColumnNumber', () => {
    expect(parseSeatCode('B3')).toEqual({ rowLabel: 'B', columnNumber: 3 });
  });

  it('parst eine zweistellige Spaltennummer korrekt', () => {
    expect(parseSeatCode('C12')).toEqual({ rowLabel: 'C', columnNumber: 12 });
  });

  it('wirft bei einem Code mit vertauschter Reihenfolge (Ziffer vor Buchstabe)', () => {
    expect(() => parseSeatCode('3B')).toThrow();
  });

  it('wirft bei einem Code ohne Spaltennummer', () => {
    expect(() => parseSeatCode('B')).toThrow();
  });

  it('wirft bei einem leeren Code', () => {
    expect(() => parseSeatCode('')).toThrow();
  });
});
