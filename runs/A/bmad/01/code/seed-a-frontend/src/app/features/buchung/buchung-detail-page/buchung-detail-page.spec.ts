import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router } from '@angular/router';

import { BuchungDetailPage } from './buchung-detail-page';
import { Booking } from '../../../core/models/booking.model';

describe('BuchungDetailPage', () => {
  const mockBooking: Booking = {
    reference: 'ABCDEFGHJKMNPQRSTUVWXYZ23',
    eventId: 'e1',
    eventTitle: 'Kammerkonzert',
    venueName: 'Stadthalle Nordpark',
    startsAt: '2026-09-05T19:30:00',
    status: 'Active',
    seats: [
      { seatId: 's-a1', row: 'A', column: 1, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 },
      { seatId: 's-a2', row: 'A', column: 2, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 }
    ],
    totalPrice: 64
  };

  const mockCancelledBooking: Booking = { ...mockBooking, status: 'Cancelled' };

  function createFixture(routerStub: Partial<Router>) {
    TestBed.configureTestingModule({
      imports: [BuchungDetailPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), { provide: Router, useValue: routerStub }]
    });
    const fixture = TestBed.createComponent(BuchungDetailPage);
    fixture.componentRef.setInput('referenz', mockBooking.reference);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
  });

  it('shows "Buchung bestätigt.", the reference, all seats and the total price when navigation state carries a matching booking with justBooked', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () =>
        ({ extras: { state: { booking: mockBooking, justBooked: true } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-detail-page__confirmation')?.textContent?.trim()).toBe('Buchung bestätigt.');
    expect(compiled.querySelector('.buchung-detail-page__reference')?.textContent).toContain(mockBooking.reference);

    const seatItems = compiled.querySelectorAll('.buchung-detail-page__seats li');
    expect(seatItems.length).toBe(2);
    expect(seatItems[0].textContent).toContain('Reihe A, Platz 1');
    expect(seatItems[0].textContent).toContain('Kategorie A');
    expect(seatItems[0].textContent).toContain('32,00 €');

    expect(compiled.querySelector('.buchung-detail-page__total')?.textContent).toContain('64,00 €');
  });

  it('does not show "Buchung bestätigt." when navigation state carries a booking without justBooked', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-detail-page__confirmation')).toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__reference')?.textContent).toContain(mockBooking.reference);
    TestBed.inject(HttpTestingController).expectNone(`/api/bookings/${mockBooking.reference}`);
  });

  it('shows "Kopiert." after clicking "Kopieren" with a mocked clipboard', async () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () =>
        ({ extras: { state: { booking: mockBooking, justBooked: true } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    Object.assign(navigator, { clipboard: { writeText: () => Promise.resolve() } });
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('.buchung-detail-page__reference button') as HTMLButtonElement;
    expect(button.textContent?.trim()).toBe('Kopieren');

    button.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(button.textContent?.trim()).toBe('Kopiert.');
  });

  it('shows a skeleton and then the booking loaded via GET when no navigation state is set', () => {
    const routerStub: Partial<Router> = { getCurrentNavigation: () => null };
    const fixture = createFixture(routerStub);

    let compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-detail-page__skeleton')).not.toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__reference')).toBeNull();

    TestBed.inject(HttpTestingController).expectOne(`/api/bookings/${mockBooking.reference}`).flush(mockBooking);
    fixture.detectChanges();

    compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.buchung-detail-page__confirmation')).toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__reference')?.textContent).toContain(mockBooking.reference);
    expect(compiled.querySelector('.buchung-detail-page__total')?.textContent).toContain('64,00 €');
  });

  it('shows "Keine Buchung mit dieser Referenz gefunden." when the GET request responds with 404', () => {
    const routerStub: Partial<Router> = { getCurrentNavigation: () => null };
    const fixture = createFixture(routerStub);

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}`)
      .flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const alert = compiled.querySelector('[role="alert"]');
    expect(alert?.textContent?.trim()).toBe('Keine Buchung mit dieser Referenz gefunden.');
  });

  it('shows a generic load-error message (not "not found") when the GET request fails with a server error', () => {
    const routerStub: Partial<Router> = { getCurrentNavigation: () => null };
    const fixture = createFixture(routerStub);

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}`)
      .flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const alert = compiled.querySelector('[role="alert"]');
    expect(alert?.textContent?.trim()).toBe('Buchung konnte nicht geladen werden. Bitte versuchen Sie es erneut.');
  });

  it('shows a "Stornieren" button for an active booking', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);
    expect((fixture.nativeElement as HTMLElement).querySelector('.buchung-detail-page__cancel-trigger')).not.toBeNull();
  });

  it('does not show a "Stornieren" button for an already cancelled booking', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () =>
        ({ extras: { state: { booking: mockCancelledBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);
    expect((fixture.nativeElement as HTMLElement).querySelector('.buchung-detail-page__cancel-trigger')).toBeNull();
  });

  it('opens the cancel dialog on click of "Stornieren" without triggering an HTTP request', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(compiled.querySelector('.cancel-dialog')).not.toBeNull();
    TestBed.inject(HttpTestingController).expectNone(`/api/bookings/${mockBooking.reference}/cancel`);
  });

  it('cancels the booking on confirmation in the dialog and shows the cancelled state', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();
    (compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement).click();
    fixture.detectChanges();

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}/cancel`)
      .flush(mockCancelledBooking);
    fixture.detectChanges();

    expect(compiled.querySelector('.status-badge--cancelled')).not.toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__confirmation')?.textContent?.trim()).toBe(
      'Buchung storniert. Die Plätze sind wieder frei.'
    );
    expect(compiled.querySelector('.buchung-detail-page__cancel-trigger')).toBeNull();
    expect(compiled.querySelector('.cancel-dialog')).toBeNull();
  });

  it('closes the dialog without an HTTP request on click of "Abbrechen"', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();
    (compiled.querySelector('.cancel-dialog__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(compiled.querySelector('.cancel-dialog')).toBeNull();
    expect(compiled.querySelector('.status-badge--cancelled')).toBeNull();
    TestBed.inject(HttpTestingController).expectNone(`/api/bookings/${mockBooking.reference}/cancel`);
  });

  it('re-fetches and shows the cancelled state (instead of a generic error) when the cancel request responds with 409 already-cancelled', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();
    (compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement).click();
    fixture.detectChanges();

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}/cancel`)
      .flush(null, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}`)
      .flush(mockCancelledBooking);
    fixture.detectChanges();

    expect(compiled.querySelector('.status-badge--cancelled')).not.toBeNull();
    expect(compiled.querySelector('app-error-banner')).toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__cancel-trigger')).toBeNull();
  });

  it('resets stornierung state when the referenz input changes to a different booking', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();
    (compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement).click();
    fixture.detectChanges();

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}/cancel`)
      .flush(mockCancelledBooking);
    fixture.detectChanges();
    expect(compiled.querySelector('.status-badge--cancelled')).not.toBeNull();

    const otherReference = 'ZZYYXXWWVVUUTTSSRRQQPPOO1';
    fixture.componentRef.setInput('referenz', otherReference);
    fixture.detectChanges();

    const otherBooking: Booking = { ...mockBooking, reference: otherReference, status: 'Active' };
    TestBed.inject(HttpTestingController).expectOne(`/api/bookings/${otherReference}`).flush(otherBooking);
    fixture.detectChanges();

    expect(compiled.querySelector('.status-badge--cancelled')).toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__confirmation')).toBeNull();
    expect(compiled.querySelector('.buchung-detail-page__cancel-trigger')).not.toBeNull();
  });

  it('shows an error banner and keeps the booking active when cancellation fails', () => {
    const routerStub: Partial<Router> = {
      getCurrentNavigation: () => ({ extras: { state: { booking: mockBooking } } }) as unknown as ReturnType<Router['getCurrentNavigation']>
    };
    const fixture = createFixture(routerStub);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.buchung-detail-page__cancel-trigger') as HTMLButtonElement).click();
    fixture.detectChanges();
    (compiled.querySelector('.cancel-dialog__confirm') as HTMLButtonElement).click();
    fixture.detectChanges();

    TestBed.inject(HttpTestingController)
      .expectOne(`/api/bookings/${mockBooking.reference}/cancel`)
      .flush(null, { status: 500, statusText: 'Server Error' });
    fixture.detectChanges();

    expect(compiled.querySelector('app-error-banner')?.textContent?.trim()).toBe(
      'Stornierung fehlgeschlagen. Bitte versuchen Sie es erneut.'
    );
    expect(compiled.querySelector('.cancel-dialog')).toBeNull();
    expect(compiled.querySelector('.status-badge--cancelled')).toBeNull();
  });
});
