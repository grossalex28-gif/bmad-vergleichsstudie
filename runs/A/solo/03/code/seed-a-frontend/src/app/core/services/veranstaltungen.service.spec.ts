import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { VeranstaltungenService } from './veranstaltungen.service';

describe('VeranstaltungenService', () => {
  let service: VeranstaltungenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(VeranstaltungenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('lädt Spielstätten von /api/spielstaetten', () => {
    service.getSpielstaetten().subscribe();

    const req = httpMock.expectOne('/api/spielstaetten');
    expect(req.request.method).toBe('GET');
    req.flush([{ id: 'V1', name: 'Stadthalle Nordpark' }]);
  });

  it('hängt gesetzte Filter als Query-Parameter an', () => {
    service.getVeranstaltungen({ von: '2026-09-01', bis: '2026-09-30', spielstaetteId: 'V1' }).subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/veranstaltungen' && r.params.get('von') === '2026-09-01' && r.params.get('bis') === '2026-09-30' && r.params.get('spielstaetteId') === 'V1'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('lässt leere Filter aus den Query-Parametern weg', () => {
    service.getVeranstaltungen({}).subscribe();

    const req = httpMock.expectOne('/api/veranstaltungen');
    expect(req.request.params.keys().length).toBe(0);
    req.flush([]);
  });

  it('lädt den Sitzplan einer Veranstaltung', () => {
    service.getSitzplan('E1').subscribe();

    const req = httpMock.expectOne('/api/veranstaltungen/E1/sitzplan');
    expect(req.request.method).toBe('GET');
    req.flush({ veranstaltungId: 'E1', raum: { reihen: [], spalten: 0, gangSpalten: [], gangHinweis: null }, sitzplaetze: [], preiskategorien: [] });
  });
});
