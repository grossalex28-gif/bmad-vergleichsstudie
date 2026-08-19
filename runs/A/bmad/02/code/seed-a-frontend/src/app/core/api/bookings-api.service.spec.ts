import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { BookingsApiService } from './bookings-api.service';
import { Booking, CreateBookingRequest } from './booking';

describe('BookingsApiService', () => {
  let service: BookingsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BookingsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('ruft POST /api/bookings mit dem Request-Body auf und mapped die Antwort', () => {
    const request: CreateBookingRequest = {
      eventId: 1,
      name: 'Erika Musterfrau',
      email: 'erika@example.com',
      positionen: [{ rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 }]
    };
    const erwarteteBooking: Booking = {
      reference: 'ABCD2345',
      name: 'Erika Musterfrau',
      status: 'aktiv',
      positionen: [{ rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 32 } }],
      gesamtpreis: 32
    };

    let ergebnis: Booking | undefined;
    service.createBooking(request).subscribe(booking => (ergebnis = booking));

    const req = httpMock.expectOne('/api/bookings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(erwarteteBooking);

    expect(ergebnis).toEqual(erwarteteBooking);
  });

  it('ruft GET /api/bookings/:reference auf und mapped die Antwort', () => {
    const erwarteteBooking: Booking = {
      reference: 'ABCD2345',
      name: 'Erika Musterfrau',
      status: 'aktiv',
      positionen: [{ rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 32 } }],
      gesamtpreis: 32
    };

    let ergebnis: Booking | undefined;
    service.getBooking('ABCD2345').subscribe(booking => (ergebnis = booking));

    const req = httpMock.expectOne('/api/bookings/ABCD2345');
    expect(req.request.method).toBe('GET');
    req.flush(erwarteteBooking);

    expect(ergebnis).toEqual(erwarteteBooking);
  });

  it('ruft POST /api/bookings/:reference/cancel ohne Body auf und mapped die Antwort', () => {
    const erwarteteBooking: Booking = {
      reference: 'ABCD2345',
      name: 'Erika Musterfrau',
      status: 'storniert',
      positionen: [{ rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 32 } }],
      gesamtpreis: 32
    };

    let ergebnis: Booking | undefined;
    service.cancelBooking('ABCD2345').subscribe(booking => (ergebnis = booking));

    const req = httpMock.expectOne('/api/bookings/ABCD2345/cancel');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toBeNull();
    req.flush(erwarteteBooking);

    expect(ergebnis).toEqual(erwarteteBooking);
  });
});
