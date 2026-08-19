import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { VeranstaltungenListe } from './veranstaltungen-liste';

describe('VeranstaltungenListe', () => {
  let component: VeranstaltungenListe;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [VeranstaltungenListe],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });

    const fixture = TestBed.createComponent(VeranstaltungenListe);
    component = fixture.componentInstance;
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lädt Spielstätten und Veranstaltungen ohne Filter beim Start', () => {
    httpMock.expectOne('/api/spielstaetten').flush([{ id: 'V1', name: 'Stadthalle Nordpark' }]);
    const req = httpMock.expectOne('/api/veranstaltungen');
    req.flush([
      { id: 'E1', titel: 'Kammerkonzert Frühling', spielstaetteId: 'V1', spielstaetteName: 'Stadthalle Nordpark', zeitpunkt: '2026-09-05T19:30:00' }
    ]);

    expect(component.spielstaetten().length).toBe(1);
    expect(component.veranstaltungen().length).toBe(1);
    expect(component.loading()).toBe(false);
  });

  it('filtert nach Datumsbereich und Spielstätte', () => {
    httpMock.expectOne('/api/spielstaetten').flush([]);
    httpMock.expectOne('/api/veranstaltungen').flush([]);

    component.von = '2026-09-01';
    component.bis = '2026-09-30';
    component.spielstaetteId = 'V1';
    component.filtern();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/veranstaltungen' && r.params.get('von') === '2026-09-01' && r.params.get('bis') === '2026-09-30' && r.params.get('spielstaetteId') === 'V1'
    );
    req.flush([]);
  });

  it('setzt den Filter zurück und lädt erneut ohne Query-Parameter', () => {
    httpMock.expectOne('/api/spielstaetten').flush([]);
    httpMock.expectOne('/api/veranstaltungen').flush([]);

    component.von = '2026-09-01';
    component.spielstaetteId = 'V1';
    component.zuruecksetzen();

    expect(component.von).toBe('');
    expect(component.spielstaetteId).toBe('');
    const req = httpMock.expectOne('/api/veranstaltungen');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([]);
  });

  it('zeigt eine Fehlermeldung, wenn das Laden fehlschlägt', () => {
    httpMock.expectOne('/api/spielstaetten').flush([]);
    httpMock.expectOne('/api/veranstaltungen').flush('Fehler', { status: 500, statusText: 'Server Error' });

    expect(component.error()).toBeTruthy();
    expect(component.loading()).toBe(false);
  });
});
