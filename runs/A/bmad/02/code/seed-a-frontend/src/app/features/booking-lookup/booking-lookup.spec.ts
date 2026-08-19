import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { of, throwError } from 'rxjs';

import { BookingLookup } from './booking-lookup';
import { BookingsApiService } from '../../core/api/bookings-api.service';
import { Booking } from '../../core/api/booking';

describe('BookingLookup', () => {
  function configure(bookingsApiStub: Partial<BookingsApiService>) {
    TestBed.configureTestingModule({
      imports: [BookingLookup],
      providers: [{ provide: BookingsApiService, useValue: bookingsApiStub }, provideRouter([])]
    });
  }

  function submit(fixture: ReturnType<typeof TestBed.createComponent<BookingLookup>>, referenz: string): void {
    const root = fixture.nativeElement as HTMLElement;
    const input = root.querySelector('#buchung-referenz') as HTMLInputElement;
    input.value = referenz;
    input.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    (root.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();
  }

  it('zeigt bei ungültigem Referenzformat einen Inline-Fehler an, ohne getBooking aufzurufen', () => {
    const getBooking = vi.fn();
    configure({ getBooking });
    const fixture = TestBed.createComponent(BookingLookup);
    fixture.detectChanges();

    submit(fixture, 'zu-kurz');

    expect(getBooking).not.toHaveBeenCalled();
    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('#buchung-referenz-fehler')).not.toBeNull();
  });

  it('normalisiert Kleinschreibung und Bindestriche, ruft getBooking auf und navigiert mit Buchung im State', () => {
    const gefundeneBooking: Booking = {
      reference: 'AB4F7Q2K',
      name: 'Erika Musterfrau',
      status: 'aktiv',
      positionen: [{ rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 25 } }],
      gesamtpreis: 25
    };
    const getBooking = vi.fn().mockReturnValue(of(gefundeneBooking));
    configure({ getBooking });
    const fixture = TestBed.createComponent(BookingLookup);
    fixture.detectChanges();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    submit(fixture, 'ab4f-7q2k');

    expect(getBooking).toHaveBeenCalledWith('AB4F7Q2K');
    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', 'AB4F7Q2K'], { state: { booking: gefundeneBooking } });
  });

  it('zeigt bei BOOKING_NOT_FOUND ein Banner mit role="alert" und aria-live="assertive"', () => {
    const notFoundError = new HttpErrorResponse({
      status: 404,
      error: { code: 'BOOKING_NOT_FOUND', message: 'Buchung nicht gefunden.' }
    });
    const getBooking = vi.fn().mockReturnValue(throwError(() => notFoundError));
    configure({ getBooking });
    const fixture = TestBed.createComponent(BookingLookup);
    fixture.detectChanges();

    submit(fixture, 'AB4F7Q2K');

    const root = fixture.nativeElement as HTMLElement;
    const banner = root.querySelector('.booking-lookup__error');
    expect(banner?.textContent).toContain('Buchungsreferenz nicht gefunden.');
    expect(banner?.getAttribute('role')).toBe('alert');
    expect(banner?.getAttribute('aria-live')).toBe('assertive');
  });

  it('zeigt bei einem anderen Fehler eine generische Fallback-Meldung', () => {
    const getBooking = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 500 })));
    configure({ getBooking });
    const fixture = TestBed.createComponent(BookingLookup);
    fixture.detectChanges();

    submit(fixture, 'AB4F7Q2K');

    const root = fixture.nativeElement as HTMLElement;
    const text = root.querySelector('.booking-lookup__error')?.textContent;
    expect(text).toContain('Die Buchung konnte nicht abgerufen werden. Bitte versuchen Sie es später erneut.');
    expect(text).not.toContain('500');
  });
});
