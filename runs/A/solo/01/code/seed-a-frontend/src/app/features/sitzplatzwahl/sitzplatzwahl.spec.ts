import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { Sitzplan, VeranstaltungDetail } from '../../core/models/veranstaltung.model';
import { SitzplatzwahlComponent } from './sitzplatzwahl';

const veranstaltung: VeranstaltungDetail = {
  id: 'E1',
  titel: 'Testkonzert',
  beschreibung: '',
  dauerMinuten: 90,
  altersfreigabe: 0,
  spielstaetteId: 'V1',
  spielstaetteName: 'Stadthalle',
  raumId: 'R1',
  raumName: 'Großer Saal',
  zeitpunkt: '2026-09-05T19:30:00',
  preiskategorien: [
    { id: 'E1-A', name: 'Kategorie A', preis: 30 },
    { id: 'E1-B', name: 'Kategorie B', preis: 20 }
  ]
};

const sitzplan: Sitzplan = {
  raumName: 'Großer Saal',
  reihen: ['A', 'B'],
  spalten: 3,
  gangSpalten: [2],
  gangHinweis: null,
  belegtePlaetze: []
};

describe('SitzplatzwahlComponent', () => {
  let httpMock: HttpTestingController;

  async function erstelleComponent() {
    await TestBed.configureTestingModule({
      imports: [SitzplatzwahlComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'E1' }) } }
        }
      ]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(SitzplatzwahlComponent);
    fixture.detectChanges();

    httpMock.expectOne('/api/veranstaltungen/E1').flush(veranstaltung);
    httpMock.expectOne('/api/veranstaltungen/E1/sitzplan').flush(sitzplan);
    fixture.detectChanges();

    return fixture;
  }

  it('weist einem neu ausgewählten Sitzplatz die erste Preiskategorie zu und berechnet den Gesamtpreis', async () => {
    const fixture = await erstelleComponent();
    const component = fixture.componentInstance;

    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 1 });
    fixture.detectChanges();

    expect(component['ausgewaehlteSitzplaetze']()).toEqual([
      { reihe: 'A', spalte: 1, preiskategorieId: 'E1-A' }
    ]);
    expect(component['gesamtpreis']()).toBe(30);

    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 3 });
    expect(component['gesamtpreis']()).toBe(60);
  });

  it('entfernt einen Sitzplatz aus der Auswahl, wenn er erneut umgeschaltet wird', async () => {
    const fixture = await erstelleComponent();
    const component = fixture.componentInstance;

    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 1 });
    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 1 });

    expect(component['ausgewaehlteSitzplaetze']()).toEqual([]);
  });

  it('navigiert nach erfolgreicher Buchung zur Buchungsbestätigung', async () => {
    const fixture = await erstelleComponent();
    const component = fixture.componentInstance;
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 1 });
    component['name'] = 'Erika Mustermann';
    component['email'] = 'erika@example.com';
    component['buchen']();

    httpMock
      .expectOne('/api/buchungen')
      .flush({ referenz: 'ABC12345', positionen: [], gesamtpreis: 30 });

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', 'ABC12345']);
  });

  it('zeigt bei einem Sitzplatzkonflikt eine Fehlermeldung, setzt die Auswahl zurück und lädt den Sitzplan neu', async () => {
    const fixture = await erstelleComponent();
    const component = fixture.componentInstance;

    component['sitzplatzUmschalten']({ reihe: 'A', spalte: 1 });
    component['name'] = 'Erika Mustermann';
    component['email'] = 'erika@example.com';
    component['buchen']();

    httpMock
      .expectOne('/api/buchungen')
      .flush({ message: 'Sitzplatz bereits belegt' }, { status: 409, statusText: 'Conflict' });

    httpMock.expectOne('/api/veranstaltungen/E1/sitzplan').flush({ ...sitzplan, belegtePlaetze: [{ reihe: 'A', spalte: 1 }] });
    fixture.detectChanges();

    expect(component['buchungFehler']()).toContain('Sitzplatz bereits belegt');
    expect(component['ausgewaehlteSitzplaetze']()).toEqual([]);
  });
});
