import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { BookingService } from './booking.service';
import { Booking, CreateBookingRequest } from '../models/booking.model';

describe('BookingService', () => {
  let service: BookingService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(BookingService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests POST /api/bookings with the request body and passes the response through', () => {
    const request: CreateBookingRequest = {
      eventId: 'e1',
      name: 'Max Mustermann',
      email: 'max@example.com',
      seats: [{ seatId: 's1', priceCategoryId: 'cat-a' }]
    };
    const mockBooking: Booking = {
      reference: 'ABCDEFGHJKMNPQRSTUVWXYZ23',
      eventId: 'e1',
      eventTitle: 'Kammerkonzert',
      venueName: 'Stadthalle Nordpark',
      startsAt: '2026-09-05T19:30:00',
      status: 'Active',
      seats: [{ seatId: 's1', row: 'A', column: 1, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 }],
      totalPrice: 32
    };

    let result: Booking | undefined;
    service.createBooking(request).subscribe((booking) => (result = booking));

    const req = httpMock.expectOne('/api/bookings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockBooking);

    expect(result).toEqual(mockBooking);
  });

  it('requests GET /api/bookings/{reference} and passes the response through', () => {
    const mockBooking: Booking = {
      reference: 'ABCDEFGHJKMNPQRSTUVWXYZ23',
      eventId: 'e1',
      eventTitle: 'Kammerkonzert',
      venueName: 'Stadthalle Nordpark',
      startsAt: '2026-09-05T19:30:00',
      status: 'Active',
      seats: [{ seatId: 's1', row: 'A', column: 1, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 }],
      totalPrice: 32
    };

    let result: Booking | undefined;
    service.getBooking('ABCDEFGHJKMNPQRSTUVWXYZ23').subscribe((booking) => (result = booking));

    const req = httpMock.expectOne('/api/bookings/ABCDEFGHJKMNPQRSTUVWXYZ23');
    expect(req.request.method).toBe('GET');
    req.flush(mockBooking);

    expect(result).toEqual(mockBooking);
  });
});
