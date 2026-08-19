import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, Router } from '@angular/router';
import { HttpErrorResponse } from '@angular/common/http';
import { forkJoin } from 'rxjs';

import { EventsApiService } from '../../core/api/events-api.service';
import { BookingsApiService } from '../../core/api/bookings-api.service';
import { EventDetail } from '../../core/api/event-detail';
import { SeatMap } from '../../core/api/seat-map';
import { parseSeatCode } from '../../core/api/booking';
import { SeatMapGrid } from './seat-map/seat-map';
import { PriceSummary } from './price-summary/price-summary';
import { BookingForm } from './booking-form/booking-form';
import { BookingSelectionService } from './booking-selection.service';

@Component({
  selector: 'app-booking',
  imports: [SeatMapGrid, PriceSummary, BookingForm],
  providers: [BookingSelectionService],
  templateUrl: './booking.html',
  styleUrl: './booking.scss'
})
export class BookingPage {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly eventsApi = inject(EventsApiService);
  private readonly bookingsApi = inject(BookingsApiService);
  private readonly selection = inject(BookingSelectionService);

  readonly seatMap = signal<SeatMap | null>(null);
  readonly event = signal<EventDetail | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly loadError = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  private eventId = 0;

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    if (!Number.isInteger(id) || id > 2147483647) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }
    this.eventId = id;

    forkJoin({
      seatMap: this.eventsApi.getSeatMap(id),
      event: this.eventsApi.getEvent(id)
    }).subscribe({
      next: ({ seatMap, event }) => {
        this.seatMap.set(seatMap);
        this.event.set(event);
        this.selection.setPriceCategories(event.preiskategorien);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.error?.code === 'EVENT_NOT_FOUND') {
          this.notFound.set(true);
        } else {
          this.loadError.set(true);
        }
      }
    });
  }

  onFormSubmitted(daten: { name: string; email: string }): void {
    const positionen = this.selection.selectedSeats().map(seat => ({
      rowLabel: seat.rowLabel,
      columnNumber: seat.columnNumber,
      priceCategoryId: seat.priceCategoryId as number // Button nur aktiv, wenn allSelectedSeatsHaveCategory() true ist
    }));

    this.submitting.set(true);
    this.submitError.set(null);

    this.bookingsApi
      .createBooking({ eventId: this.eventId, name: daten.name, email: daten.email, positionen })
      .subscribe({
        next: booking => {
          this.router.navigate(['/buchungen', booking.reference], { state: { booking } });
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          const code = err.error?.code as string | undefined;
          if (code === 'SEAT_CONFLICT') {
            const conflictingSeats = (err.error?.details as string[] | undefined) ?? [];
            const parsed = conflictingSeats.map(parseSeatCode);
            this.selection.removeSeats(parsed);
            this.markSeatsOccupied(parsed);
            this.submitError.set(this.buildSeatConflictMessage(conflictingSeats));
          } else {
            this.submitError.set('Die Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.');
          }
        }
      });
  }

  private markSeatsOccupied(seats: { rowLabel: string; columnNumber: number }[]): void {
    this.seatMap.update(current => {
      if (!current) {
        return current;
      }
      return {
        ...current,
        rows: current.rows.map(row => ({
          ...row,
          cells: row.cells.map(cell =>
            seats.some(s => s.rowLabel === row.rowLabel && s.columnNumber === cell.columnNumber)
              ? { ...cell, status: 'occupied' as const }
              : cell
          )
        }))
      };
    });
  }

  private buildSeatConflictMessage(seatCodes: string[]): string {
    const nomen = seatCodes.length === 1 ? 'Sitzplatz' : 'Sitzplätze';
    const verb = seatCodes.length === 1 ? 'ist' : 'sind';
    return `${nomen} ${seatCodes.join(', ')} ${verb} inzwischen vergeben. Bitte wählen Sie erneut.`;
  }
}
