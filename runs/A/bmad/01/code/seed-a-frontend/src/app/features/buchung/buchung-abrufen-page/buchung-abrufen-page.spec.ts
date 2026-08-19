import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { BuchungAbrufenPage } from './buchung-abrufen-page';
import { Booking } from '../../../core/models/booking.model';

describe('BuchungAbrufenPage', () => {
  let httpMock: HttpTestingController;
  let navigateSpy: ReturnType<typeof vi.fn>;

  const mockBooking: Booking = {
    reference: 'ABCDEFGHJKMNPQRSTUVWXYZ23',
    eventId: 'e1',
    eventTitle: 'Kammerkonzert',
    venueName: 'Stadthalle Nordpark',
    startsAt: '2026-09-05T19:30:00',
    status: 'Active',
    seats: [{ seatId: 's-a1', row: 'A', column: 1, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 }],
    totalPrice: 32
  };

  function createFixture() {
    navigateSpy = vi.fn();
    TestBed.configureTestingModule({
      imports: [BuchungAbrufenPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Router, useValue: { navigate: navigateSpy } }]
    });
    httpMock = TestBed.inject(HttpTestingController);
    const fixture = TestBed.createComponent(BuchungAbrufenPage);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    httpMock.verify();
  });

  function submitReference(fixture: ReturnType<typeof createFixture>, reference: string): void {
    const compiled = fixture.nativeElement as HTMLElement;
    const input = compiled.querySelector('.buchung-abrufen-page__input') as HTMLInputElement;
    input.value = reference;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    (compiled.querySelector('.buchung-abrufen-page__form') as HTMLFormElement).dispatchEvent(new Event('submit'));
    fixture.detectChanges();
  }

  it('requests GET /api/bookings/{reference} on submit and navigates to the detail page on success', () => {
    const fixture = createFixture();
    submitReference(fixture, mockBooking.reference);

    const req = httpMock.expectOne(`/api/bookings/${mockBooking.reference}`);
    expect(req.request.method).toBe('GET');
    req.flush(mockBooking);

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', mockBooking.reference], { state: { booking: mockBooking } });
  });

  it('navigates using the canonical reference from the response, not the raw typed input', () => {
    const fixture = createFixture();
    submitReference(fixture, mockBooking.reference.toLowerCase());

    const req = httpMock.expectOne(`/api/bookings/${mockBooking.reference.toLowerCase()}`);
    req.flush(mockBooking);

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', mockBooking.reference], { state: { booking: mockBooking } });
  });

  it('shows an inline error and does not navigate when the reference is unknown (404)', () => {
    const fixture = createFixture();
    submitReference(fixture, 'UNBEKANNTEREFERENZ1234');

    httpMock.expectOne('/api/bookings/UNBEKANNTEREFERENZ1234').flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-abrufen-page__field-error')?.textContent?.trim()).toBe(
      'Keine Buchung mit dieser Referenz gefunden.'
    );
    expect(navigateSpy).not.toHaveBeenCalled();
  });

  it('shows a validation error and sends no request when the field is empty', () => {
    const fixture = createFixture();
    submitReference(fixture, '');

    httpMock.expectNone(() => true);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-abrufen-page__field-error')?.textContent?.trim()).toBe(
      'Bitte geben Sie eine Buchungsreferenz ein.'
    );
  });

  it('shows a generic error for other HTTP error statuses', () => {
    const fixture = createFixture();
    submitReference(fixture, mockBooking.reference);

    httpMock.expectOne(`/api/bookings/${mockBooking.reference}`).flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-abrufen-page__field-error')?.textContent?.trim()).toBe(
      'Buchung konnte nicht abgerufen werden. Bitte versuchen Sie es erneut.'
    );
  });
});
