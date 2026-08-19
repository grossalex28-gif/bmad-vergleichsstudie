import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap } from '@angular/router';
import { Sitzplatzauswahl } from './sitzplatzauswahl';
import { Sitzplan, VeranstaltungDetail } from '../../core/models/api.models';

const testVeranstaltung: VeranstaltungDetail = {
  id: 'E1',
  titel: 'Kammerkonzert Frühling',
  beschreibung: 'Beschreibung',
  zeitpunkt: '2026-09-05T19:30:00',
  dauerMinuten: 90,
  altersfreigabe: 0,
  spielstaette: { id: 'V1', name: 'Stadthalle Nordpark' },
  raum: { id: 'R2', name: 'Kleiner Saal' },
  preiskategorien: [
    { id: 'E1-A', name: 'Kategorie A', preis: 32 },
    { id: 'E1-B', name: 'Kategorie B', preis: 22 }
  ]
};

const testSitzplan: Sitzplan = {
  veranstaltungId: 'E1',
  raum: { reihen: ['A', 'B'], spalten: 3, gangSpalten: [2], gangHinweis: 'Mittelgang' },
  sitzplaetze: [
    { reihe: 'A', spalte: 1, typ: 'Sitzplatz', status: 'Frei' },
    { reihe: 'A', spalte: 2, typ: 'Gang', status: null },
    { reihe: 'A', spalte: 3, typ: 'Sitzplatz', status: 'Frei' },
    { reihe: 'B', spalte: 1, typ: 'Sitzplatz', status: 'Belegt' },
    { reihe: 'B', spalte: 2, typ: 'Gang', status: null },
    { reihe: 'B', spalte: 3, typ: 'Sitzplatz', status: 'Frei' }
  ],
  preiskategorien: testVeranstaltung.preiskategorien
};

describe('Sitzplatzauswahl', () => {
  let component: Sitzplatzauswahl;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [Sitzplatzauswahl],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        { provide: Router, useValue: { navigate: () => Promise.resolve(true) } },
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'E1' }) } }
        }
      ]
    });

    const fixture = TestBed.createComponent(Sitzplatzauswahl);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);

    httpMock.expectOne('/api/veranstaltungen/E1').flush(testVeranstaltung);
    httpMock.expectOne('/api/veranstaltungen/E1/sitzplan').flush(testSitzplan);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lädt Veranstaltung und Sitzplan beim Start', () => {
    expect(component.veranstaltung()).toEqual(testVeranstaltung);
    expect(component.loading()).toBe(false);
  });

  it('wählt einen freien Sitzplatz mit der ersten Preiskategorie aus', () => {
    const sitzplatz = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 1)!;

    component.sitzplatzKlicken(sitzplatz);

    expect(component.ausgewaehlteListe()).toEqual([{ reihe: 'A', spalte: 1, preiskategorieId: 'E1-A' }]);
  });

  it('hebt die Auswahl bei erneutem Klick wieder auf', () => {
    const sitzplatz = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 1)!;

    component.sitzplatzKlicken(sitzplatz);
    component.sitzplatzKlicken(sitzplatz);

    expect(component.ausgewaehlteListe()).toEqual([]);
  });

  it('ignoriert Klicks auf belegte Sitzplätze', () => {
    const sitzplatz = testSitzplan.sitzplaetze.find((s) => s.reihe === 'B' && s.spalte === 1)!;

    component.sitzplatzKlicken(sitzplatz);

    expect(component.ausgewaehlteListe()).toEqual([]);
  });

  it('ignoriert Klicks auf Gang-Positionen', () => {
    const sitzplatz = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 2)!;

    component.sitzplatzKlicken(sitzplatz);

    expect(component.ausgewaehlteListe()).toEqual([]);
  });

  it('berechnet den Gesamtpreis aus den gewählten Preiskategorien', () => {
    const a1 = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 1)!;
    const a3 = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 3)!;

    component.sitzplatzKlicken(a1);
    component.sitzplatzKlicken(a3);
    component.kategorieAendern(component.ausgewaehlteListe()[1], 'E1-B');

    expect(component.gesamtpreis()).toBe(32 + 22);
  });

  it('entfernt einen Sitzplatz aus der Auswahl über entfernen()', () => {
    const a1 = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 1)!;
    component.sitzplatzKlicken(a1);

    component.entfernen(component.ausgewaehlteListe()[0]);

    expect(component.ausgewaehlteListe()).toEqual([]);
  });

  it('entfernt belegte Sitzplätze aus der Auswahl und lädt den Sitzplan neu, wenn die Buchung mit 409 abgelehnt wird', () => {
    const a1 = testSitzplan.sitzplaetze.find((s) => s.reihe === 'A' && s.spalte === 1)!;
    component.sitzplatzKlicken(a1);
    component.name = 'Erika Mustermann';
    component.email = 'erika@example.com';

    component.jetztBuchen();

    const createReq = httpMock.expectOne('/api/buchungen');
    createReq.flush(
      { title: 'Sitzplatzkonflikt', status: 409, detail: 'belegt', belegteSitzplaetze: [{ reihe: 'A', spalte: 1 }] },
      { status: 409, statusText: 'Conflict' }
    );

    expect(component.ausgewaehlteListe()).toEqual([]);
    expect(component.buchungsFehler()).toBeTruthy();

    httpMock.expectOne('/api/veranstaltungen/E1').flush(testVeranstaltung);
    httpMock.expectOne('/api/veranstaltungen/E1/sitzplan').flush(testSitzplan);
  });
});
