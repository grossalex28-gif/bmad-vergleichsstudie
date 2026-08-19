import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { BuchungenService } from './buchungen.service';

describe('BuchungenService', () => {
  let service: BuchungenService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BuchungenService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('sendet eine POST-Anfrage zum Anlegen einer Buchung', () => {
    const request = {
      veranstaltungId: 'E1',
      name: 'Erika Mustermann',
      email: 'erika@example.com',
      sitzplaetze: [{ reihe: 'A', spalte: 1, preiskategorieId: 'E1-A' }]
    };

    service.create(request).subscribe();

    const req = httpMock.expectOne('/api/buchungen');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush({});
  });

  it('lädt eine Buchung anhand der Referenz', () => {
    service.getByReferenz('AB3XK9QZ').subscribe();

    const req = httpMock.expectOne('/api/buchungen/AB3XK9QZ');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('storniert eine Buchung per POST', () => {
    service.stornieren('AB3XK9QZ').subscribe();

    const req = httpMock.expectOne('/api/buchungen/AB3XK9QZ/stornieren');
    expect(req.request.method).toBe('POST');
    req.flush({});
  });
});
