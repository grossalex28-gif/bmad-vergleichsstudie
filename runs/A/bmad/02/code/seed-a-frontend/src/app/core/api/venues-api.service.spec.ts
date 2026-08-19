import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { VenuesApiService } from './venues-api.service';
import { Venue } from './venue';

describe('VenuesApiService', () => {
  let service: VenuesApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(VenuesApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('ruft GET /api/venues auf und mapped die Antwort', () => {
    const erwarteteVenues: Venue[] = [{ id: 1, name: 'Spielstätte Nord' }];

    let ergebnis: Venue[] | undefined;
    service.getVenues().subscribe(venues => (ergebnis = venues));

    const req = httpMock.expectOne('/api/venues');
    expect(req.request.method).toBe('GET');
    req.flush(erwarteteVenues);

    expect(ergebnis).toEqual(erwarteteVenues);
  });
});
