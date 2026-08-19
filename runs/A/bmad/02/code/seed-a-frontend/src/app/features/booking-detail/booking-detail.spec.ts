import { TestBed } from '@angular/core/testing';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import localeDe from '@angular/common/locales/de';
import { Router, provideRouter } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { BookingDetailPage } from './booking-detail';
import { Booking } from '../../core/api/booking';
import { BookingsApiService } from '../../core/api/bookings-api.service';

registerLocaleData(localeDe);

// jsdom implementiert HTMLDialogElement.showModal()/close() nicht (Stand jsdom 28) — Polyfill nur für Tests,
// echte Browser unterstützen das native <dialog>-Element bereits vollständig.
beforeAll(() => {
  if (!HTMLDialogElement.prototype.showModal) {
    HTMLDialogElement.prototype.showModal = function (this: HTMLDialogElement) {
      this.setAttribute('open', '');
    };
  }
  if (!HTMLDialogElement.prototype.close) {
    HTMLDialogElement.prototype.close = function (this: HTMLDialogElement) {
      this.removeAttribute('open');
    };
  }
});

describe('BookingDetailPage', () => {
  const booking: Booking = {
    reference: 'ABCD2345',
    name: 'Erika Musterfrau',
    status: 'aktiv',
    positionen: [
      { rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 32 } },
      { rowLabel: 'A', columnNumber: 2, preiskategorie: { id: 20, name: 'Kategorie B', preis: 22 } }
    ],
    gesamtpreis: 54
  };

  const stornierteBooking: Booking = { ...booking, status: 'storniert' };

  function createFixture(
    navigationState: { booking?: Booking } | undefined,
    bookingsApiStub: Partial<BookingsApiService> = {}
  ) {
    TestBed.configureTestingModule({
      imports: [BookingDetailPage],
      providers: [
        provideRouter([]),
        { provide: LOCALE_ID, useValue: 'de-DE' },
        { provide: BookingsApiService, useValue: bookingsApiStub }
      ]
    });

    const router = TestBed.inject(Router);
    vi.spyOn(router, 'getCurrentNavigation').mockReturnValue(
      navigationState === undefined
        ? null
        : ({ extras: { state: navigationState } } as ReturnType<Router['getCurrentNavigation']>)
    );

    return TestBed.createComponent(BookingDetailPage);
  }

  it('rendert formatierte Referenz, alle Positionen, Gesamtpreis und badge--active bei Status "aktiv"', () => {
    const fixture = createFixture({ booking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.textContent).toContain('ABCD-2345');
    expect(root.textContent).toContain('Sitzplatz A1');
    expect(root.textContent).toContain('Sitzplatz A2');
    expect(root.textContent).toContain('54,00 €');
    expect(root.querySelector('.badge--active')).not.toBeNull();
  });

  it('setzt aria-live="polite" auf dem Bestaetigungstext', () => {
    const fixture = createFixture({ booking });
    fixture.detectChanges();

    const confirmation = (fixture.nativeElement as HTMLElement).querySelector('.booking-detail__confirmation');
    expect(confirmation?.getAttribute('aria-live')).toBe('polite');
  });

  it('kopiert bei Klick auf den Copy-Button die rohe, unformatierte Referenz und zeigt danach "Kopiert"', async () => {
    const writeText = vi.fn().mockResolvedValue(undefined);
    vi.stubGlobal('navigator', { clipboard: { writeText } });

    const fixture = createFixture({ booking });
    fixture.detectChanges();

    const button = (fixture.nativeElement as HTMLElement).querySelector('.booking-detail__copy') as HTMLButtonElement;
    button.click();
    await fixture.whenStable();
    fixture.detectChanges();

    expect(writeText).toHaveBeenCalledWith('ABCD2345');
    expect(button.textContent).toContain('Kopiert');

    vi.unstubAllGlobals();
  });

  it('zeigt den notAvailable-Hinweis mit Link zu /buchung-abrufen ohne Navigations-State', () => {
    const fixture = createFixture(undefined);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.booking-detail')).toBeNull();
    const link = root.querySelector('a[href="/buchung-abrufen"]');
    expect(link).not.toBeNull();
  });

  it('zeigt den Storno-Button bei Status "aktiv"', () => {
    const fixture = createFixture({ booking });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.booking-detail__cancel')).not.toBeNull();
  });

  it('zeigt den Storno-Button nicht bei Status "storniert"', () => {
    const fixture = createFixture({ booking: stornierteBooking });
    fixture.detectChanges();
    expect((fixture.nativeElement as HTMLElement).querySelector('.booking-detail__cancel')).toBeNull();
  });

  it('oeffnet bei Klick auf den Storno-Button den Bestaetigungsdialog, ohne cancelBooking aufzurufen', () => {
    const cancelBooking = vi.fn();
    const fixture = createFixture({ booking }, { cancelBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    expect(dialog.hasAttribute('open')).toBe(true);
    expect(cancelBooking).not.toHaveBeenCalled();
  });

  it('loest beim Abbrechen im Dialog keinen Aufruf aus und laesst die Buchung unveraendert (AC 4)', () => {
    const cancelBooking = vi.fn();
    const fixture = createFixture({ booking }, { cancelBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [abbrechenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    abbrechenButton.click();
    fixture.detectChanges();

    expect(dialog.hasAttribute('open')).toBe(false);
    expect(cancelBooking).not.toHaveBeenCalled();
    expect(root.querySelector('.badge--active')).not.toBeNull();
  });

  it('ruft bei Bestaetigung cancelBooking auf und aktualisiert die Anzeige aus der Server-Antwort', () => {
    const cancelBooking = vi.fn().mockReturnValue(of(stornierteBooking));
    const fixture = createFixture({ booking }, { cancelBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [, stornierenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    stornierenButton.click();
    fixture.detectChanges();

    expect(cancelBooking).toHaveBeenCalledWith('ABCD2345');
    expect(root.querySelector('.badge--cancelled')).not.toBeNull();
    expect(root.querySelector('.booking-detail__cancel')).toBeNull();
    const hinweis = root.querySelector('.booking-detail__cancel-confirmation');
    expect(hinweis?.textContent).toContain('Buchung storniert. Die Plätze sind wieder frei.');
    expect(hinweis?.getAttribute('aria-live')).toBe('polite');
  });

  it('zeigt bei ALREADY_CANCELLED einen Fehler-Banner, gleicht die Buchung mit der Server-Antwort ab und Dialog bleibt zum Schliessen nutzbar', () => {
    const cancelBooking = vi
      .fn()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ error: { code: 'ALREADY_CANCELLED' }, status: 409 })));
    const getBooking = vi.fn().mockReturnValue(of(stornierteBooking));
    const fixture = createFixture({ booking }, { cancelBooking, getBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [abbrechenButton, stornierenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    stornierenButton.click();
    fixture.detectChanges();

    const fehlerBanner = root.querySelector('.booking-detail__cancel-error');
    expect(fehlerBanner?.textContent).toContain('Diese Buchung wurde bereits storniert.');
    expect(fehlerBanner?.getAttribute('role')).toBe('alert');
    expect(fehlerBanner?.getAttribute('aria-live')).toBe('assertive');
    expect(getBooking).toHaveBeenCalledWith('ABCD2345');
    expect(root.querySelector('.badge--cancelled')).not.toBeNull();

    abbrechenButton.click();
    fixture.detectChanges();
    expect(dialog.hasAttribute('open')).toBe(false);
  });

  it('entfernt den Fehler-Banner beim Abbrechen im Dialog, statt ihn stehen zu lassen', () => {
    const cancelBooking = vi
      .fn()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ error: { code: 'ALREADY_CANCELLED' }, status: 409 })));
    const getBooking = vi.fn().mockReturnValue(of(booking));
    const fixture = createFixture({ booking }, { cancelBooking, getBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [abbrechenButton, stornierenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    stornierenButton.click();
    fixture.detectChanges();
    expect(root.querySelector('.booking-detail__cancel-error')).not.toBeNull();

    abbrechenButton.click();
    fixture.detectChanges();

    expect(root.querySelector('.booking-detail__cancel-error')).toBeNull();
  });

  it('zeigt bei einem generischen Fehlercode eine allgemeine Fehlermeldung', () => {
    const cancelBooking = vi
      .fn()
      .mockReturnValue(throwError(() => new HttpErrorResponse({ error: { code: 'SERVER_ERROR' }, status: 500 })));
    const fixture = createFixture({ booking }, { cancelBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [, stornierenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    stornierenButton.click();
    fixture.detectChanges();

    const fehlerBanner = root.querySelector('.booking-detail__cancel-error');
    expect(fehlerBanner?.textContent).toContain('Die Stornierung konnte nicht durchgeführt werden. Bitte versuchen Sie es später erneut.');
    expect(root.querySelector('.badge--active')).not.toBeNull();
  });

  it('ignoriert das native Schliessen des Dialogs (z. B. Escape-Taste), solange eine Stornierung noch laeuft', () => {
    const cancelBooking = vi.fn().mockReturnValue(new Subject<Booking>());
    const fixture = createFixture({ booking }, { cancelBooking });
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    (root.querySelector('.booking-detail__cancel') as HTMLButtonElement).click();
    fixture.detectChanges();

    const dialog = root.querySelector('.booking-detail__cancel-dialog') as HTMLDialogElement;
    const [, stornierenButton] = Array.from(dialog.querySelectorAll('button')) as HTMLButtonElement[];
    stornierenButton.click();
    fixture.detectChanges();

    const cancelEvent = new Event('cancel', { cancelable: true });
    dialog.dispatchEvent(cancelEvent);
    fixture.detectChanges();

    expect(cancelEvent.defaultPrevented).toBe(true);
    expect(dialog.hasAttribute('open')).toBe(true);
  });
});
