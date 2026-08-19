import { TestBed } from '@angular/core/testing';
import { By } from '@angular/platform-browser';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { BookingPage } from './booking';
import { EventsApiService } from '../../core/api/events-api.service';
import { BookingsApiService } from '../../core/api/bookings-api.service';
import { SeatMap } from '../../core/api/seat-map';
import { EventDetail } from '../../core/api/event-detail';
import { Booking } from '../../core/api/booking';
import { PriceSummary } from './price-summary/price-summary';
import { BookingSelectionService } from './booking-selection.service';
import { SeatMapGrid } from './seat-map/seat-map';

describe('BookingPage', () => {
  const seatMap: SeatMap = {
    rowLabels: ['A', 'B'],
    columnCount: 4,
    aisleColumns: [3],
    rows: [
      {
        rowLabel: 'A',
        cells: [
          { columnNumber: 1, status: 'free' },
          { columnNumber: 2, status: 'occupied' },
          { columnNumber: 3, status: 'aisle' },
          { columnNumber: 4, status: 'free' }
        ]
      },
      {
        rowLabel: 'B',
        cells: [
          { columnNumber: 1, status: 'free' },
          { columnNumber: 2, status: 'free' },
          { columnNumber: 3, status: 'aisle' },
          { columnNumber: 4, status: 'occupied' }
        ]
      }
    ]
  };

  const eventDetail: EventDetail = {
    id: 1,
    titel: 'Testveranstaltung',
    beschreibung: '...',
    dauerMinuten: 90,
    altersfreigabe: 0,
    spielstaette: 'Halle',
    raum: 'Saal 1',
    zeitpunkt: '2026-09-01T19:00:00',
    preiskategorien: [{ id: 10, name: 'Kategorie A', preis: 25 }]
  };

  function configure(eventsApiStub: Partial<EventsApiService>, id = '1', bookingsApiStub?: Partial<BookingsApiService>) {
    TestBed.configureTestingModule({
      imports: [BookingPage],
      providers: [
        { provide: EventsApiService, useValue: eventsApiStub },
        { provide: BookingsApiService, useValue: bookingsApiStub ?? {} },
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } }
      ]
    });
  }

  function fillAndSubmitForm(
    fixture: ReturnType<typeof TestBed.createComponent<BookingPage>>,
    seats: { rowLabel: string; columnNumber: number; priceCategoryId: number }[] = [{ rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 }]
  ): void {
    const selection = fixture.debugElement.injector.get(BookingSelectionService);
    for (const seat of seats) {
      selection.toggle(seat.rowLabel, seat.columnNumber);
      selection.assignCategory(seat.rowLabel, seat.columnNumber, seat.priceCategoryId);
    }
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const nameInput = root.querySelector('#buchung-name') as HTMLInputElement;
    nameInput.value = 'Erika Musterfrau';
    nameInput.dispatchEvent(new Event('input'));
    const emailInput = root.querySelector('#buchung-email') as HTMLInputElement;
    emailInput.value = 'erika@example.com';
    emailInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    (root.querySelector('button[type="submit"]') as HTMLButtonElement).click();
    fixture.detectChanges();
  }

  it('rendert <app-seat-map> und die Überschrift "Sitzplan wählen" nach erfolgreichem Laden', () => {
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('app-seat-map')).not.toBeNull();
    expect(root.textContent).toContain('Sitzplan wählen');
  });

  it('zeigt "Veranstaltung nicht gefunden." bei Fehlercode EVENT_NOT_FOUND', () => {
    const error = new HttpErrorResponse({ status: 404, error: { code: 'EVENT_NOT_FOUND', message: 'Veranstaltung nicht gefunden.' } });
    configure({ getSeatMap: () => throwError(() => error), getEvent: () => throwError(() => error) });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Veranstaltung nicht gefunden.');
  });

  it('zeigt bei einem anderen Fehler eine generische Meldung, keinen technischen Fehlertext', () => {
    const error = new HttpErrorResponse({ status: 500 });
    configure({ getSeatMap: () => throwError(() => error), getEvent: () => throwError(() => error) });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain('Bitte versuchen Sie es später erneut.');
    expect(text).not.toContain('500');
  });

  it('zeigt "Veranstaltung nicht gefunden." bei nicht-numerischer Id, ohne getSeatMap aufzurufen', () => {
    const getSeatMap = vi.fn();
    configure({ getSeatMap }, 'abc');
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    expect(getSeatMap).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Veranstaltung nicht gefunden.');
  });

  it('rendert <app-price-summary> mit den Preiskategorien der Veranstaltung nach erfolgreichem Laden', () => {
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).querySelector('app-price-summary')).not.toBeNull();
    const priceSummary = fixture.debugElement.query(By.directive(PriceSummary));
    expect(priceSummary.componentInstance.priceCategories()).toEqual(eventDetail.preiskategorien);
  });

  it('loest bei erfolgreichem Submit createBooking mit korrekt zusammengesetztem Request aus und navigiert zu /buchungen/<reference>', () => {
    const gebuchteBooking: Booking = {
      reference: 'ABCD2345',
      name: 'Erika Musterfrau',
      status: 'aktiv',
      positionen: [{ rowLabel: 'A', columnNumber: 1, preiskategorie: { id: 10, name: 'Kategorie A', preis: 25 } }],
      gesamtpreis: 25
    };
    const createBooking = vi.fn().mockReturnValue(of(gebuchteBooking));
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) }, '1', { createBooking });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fillAndSubmitForm(fixture);

    expect(createBooking).toHaveBeenCalledWith({
      eventId: 1,
      name: 'Erika Musterfrau',
      email: 'erika@example.com',
      positionen: [{ rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 }]
    });
    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', 'ABCD2345'], { state: { booking: gebuchteBooking } });
  });

  it('zeigt bei fehlgeschlagenem Submit den submitError-Text im .booking__error-Banner und setzt submitting zurueck', () => {
    const createBooking = vi.fn().mockReturnValue(throwError(() => new HttpErrorResponse({ status: 409 })));
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) }, '1', { createBooking });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    fillAndSubmitForm(fixture);

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.booking__error')?.textContent).toContain(
      'Die Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.'
    );

    const submitBtn = root.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(submitBtn.disabled).toBe(false);
  });

  it('zeigt bei SEAT_CONFLICT die konkrete Sitzplatz-Meldung, markiert den Platz als belegt und entfernt ihn aus der Auswahl', () => {
    const conflictError = new HttpErrorResponse({
      status: 409,
      error: { code: 'SEAT_CONFLICT', message: 'Mindestens ein gewählter Sitzplatz ist inzwischen belegt.', details: ['A1'] }
    });
    const createBooking = vi.fn().mockReturnValue(throwError(() => conflictError));
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) }, '1', { createBooking });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    fillAndSubmitForm(fixture);

    const root = fixture.nativeElement as HTMLElement;
    const banner = root.querySelector('.booking__error');
    expect(banner?.textContent).toContain('Sitzplatz A1 ist inzwischen vergeben. Bitte wählen Sie erneut.');
    expect(banner?.getAttribute('aria-live')).toBe('assertive');

    const seatMapGrid = fixture.debugElement.query(By.directive(SeatMapGrid)).componentInstance as SeatMapGrid;
    const zelleA1 = seatMapGrid.seatMap().rows[0].cells[0];
    expect(zelleA1.status).toBe('occupied');

    const selection = fixture.debugElement.injector.get(BookingSelectionService);
    expect(selection.isSelected('A', 1)).toBe(false);
  });

  it('zeigt bei mehreren SEAT_CONFLICT-Sitzplätzen die Pluralmeldung und lässt die übrige gültige Auswahl unangetastet', () => {
    const conflictError = new HttpErrorResponse({
      status: 409,
      error: {
        code: 'SEAT_CONFLICT',
        message: 'Mindestens ein gewählter Sitzplatz ist inzwischen belegt.',
        details: ['A1', 'B2']
      }
    });
    const createBooking = vi.fn().mockReturnValue(throwError(() => conflictError));
    configure({ getSeatMap: () => of(seatMap), getEvent: () => of(eventDetail) }, '1', { createBooking });
    const fixture = TestBed.createComponent(BookingPage);
    fixture.detectChanges();

    fillAndSubmitForm(fixture, [
      { rowLabel: 'A', columnNumber: 1, priceCategoryId: 10 },
      { rowLabel: 'B', columnNumber: 2, priceCategoryId: 10 },
      { rowLabel: 'B', columnNumber: 1, priceCategoryId: 10 }
    ]);

    const root = fixture.nativeElement as HTMLElement;
    expect(root.querySelector('.booking__error')?.textContent).toContain(
      'Sitzplätze A1, B2 sind inzwischen vergeben. Bitte wählen Sie erneut.'
    );

    const seatMapGrid = fixture.debugElement.query(By.directive(SeatMapGrid)).componentInstance as SeatMapGrid;
    const rows = seatMapGrid.seatMap().rows;
    expect(rows[0].cells[0].status).toBe('occupied'); // A1
    expect(rows[1].cells[1].status).toBe('occupied'); // B2

    const selection = fixture.debugElement.injector.get(BookingSelectionService);
    expect(selection.selectedSeats()).toEqual([{ rowLabel: 'B', columnNumber: 1, priceCategoryId: 10 }]);
  });
});
