import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookingDetailComponent } from './booking-detail.component';

const booking = {
  reference: 'AB3D9F2K',
  eventId: 'E1',
  eventTitle: 'Kammerkonzert',
  eventStartsAt: '2026-09-05T19:30:00',
  customerName: 'Ada Lovelace',
  customerEmail: 'ada@example.com',
  createdAt: '2026-08-15T10:00:00',
  isCancelled: false,
  totalPrice: 30,
  seats: [{ row: 'A', column: 1, priceCategoryName: 'Kategorie A', price: 30 }]
};

describe('BookingDetailComponent', () => {
  let fixture: ComponentFixture<BookingDetailComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookingDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ reference: 'AB3D9F2K' }) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(BookingDetailComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads the booking by reference', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/bookings/AB3D9F2K').flush(booking);

    expect(fixture.componentInstance['booking']()?.customerName).toBe('Ada Lovelace');
  });

  it('cancels the booking and updates the view with the cancelled state', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/bookings/AB3D9F2K').flush(booking);

    (fixture.componentInstance as unknown as { cancelBooking: () => void }).cancelBooking();

    const req = httpMock.expectOne('/api/bookings/AB3D9F2K/cancel');
    req.flush({ ...booking, isCancelled: true });

    expect(fixture.componentInstance['booking']()?.isCancelled).toBe(true);
  });

  it('shows an error when the reference does not exist', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/bookings/AB3D9F2K').flush('not found', { status: 404, statusText: 'Not Found' });

    expect(fixture.componentInstance['error']()).toBeTruthy();
  });
});
