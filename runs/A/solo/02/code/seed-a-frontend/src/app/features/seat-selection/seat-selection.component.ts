import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { firstValueFrom } from 'rxjs';

import { EventsService } from '../../core/services/events.service';
import { BookingsService } from '../../core/services/bookings.service';
import { EventDetail } from '../../core/models/event.model';
import { Seat, SeatMap } from '../../core/models/seat-map.model';
import { BookingConflict } from '../../core/models/booking.model';

interface SelectedSeat {
  row: string;
  column: number;
  priceCategoryId: string;
}

function seatKey(row: string, column: number): string {
  return `${row}-${column}`;
}

@Component({
  selector: 'app-seat-selection',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './seat-selection.component.html',
  styleUrl: './seat-selection.component.scss'
})
export class SeatSelectionComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly eventsService = inject(EventsService);
  private readonly bookingsService = inject(BookingsService);

  private eventId = '';

  protected readonly event = signal<EventDetail | null>(null);
  protected readonly seatMap = signal<SeatMap | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);

  protected readonly selectedSeats = signal<Map<string, SelectedSeat>>(new Map());

  protected customerName = '';
  protected customerEmail = '';

  protected readonly selectedSeatList = computed(() =>
    Array.from(this.selectedSeats().values()).sort((a, b) => a.row.localeCompare(b.row) || a.column - b.column)
  );

  protected readonly totalPrice = computed(() => {
    const event = this.event();
    if (!event) {
      return 0;
    }
    return this.selectedSeatList().reduce((sum, seat) => {
      const category = event.priceCategories.find((c) => c.id === seat.priceCategoryId);
      return sum + (category?.price ?? 0);
    }, 0);
  });

  ngOnInit(): void {
    this.eventId = this.route.snapshot.paramMap.get('id')!;
    this.loadData();
  }

  private loadData(): void {
    this.loading.set(true);
    this.loadError.set(null);

    Promise.all([
      firstValueFrom(this.eventsService.getById(this.eventId)),
      firstValueFrom(this.eventsService.getSeatMap(this.eventId))
    ])
      .then(([event, seatMap]) => {
        this.event.set(event);
        this.seatMap.set(seatMap);
        this.loading.set(false);
      })
      .catch(() => {
        this.loadError.set('Der Sitzplan konnte nicht geladen werden.');
        this.loading.set(false);
      });
  }

  protected seatClasses(seat: Seat, row: string): string {
    if (seat.type === 'aisle') {
      return 'seat seat--aisle';
    }
    if (seat.status === 'occupied') {
      return 'seat seat--occupied';
    }
    return this.selectedSeats().has(seatKey(row, seat.column)) ? 'seat seat--selected' : 'seat seat--free';
  }

  protected onSeatClick(seat: Seat, row: string): void {
    if (seat.type === 'aisle' || seat.status === 'occupied') {
      return;
    }

    const key = seatKey(row, seat.column);
    const next = new Map(this.selectedSeats());
    if (next.has(key)) {
      next.delete(key);
    } else {
      const defaultCategoryId = this.event()?.priceCategories[0]?.id ?? '';
      next.set(key, { row, column: seat.column, priceCategoryId: defaultCategoryId });
    }
    this.selectedSeats.set(next);
  }

  protected onCategoryChange(seat: SelectedSeat, priceCategoryId: string): void {
    const key = seatKey(seat.row, seat.column);
    const next = new Map(this.selectedSeats());
    next.set(key, { ...seat, priceCategoryId });
    this.selectedSeats.set(next);
  }

  protected submitBooking(): void {
    if (this.selectedSeatList().length === 0 || this.submitting()) {
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);

    this.bookingsService
      .create({
        eventId: this.eventId,
        customerName: this.customerName,
        customerEmail: this.customerEmail,
        seats: this.selectedSeatList().map((s) => ({
          row: s.row,
          column: s.column,
          priceCategoryId: s.priceCategoryId
        }))
      })
      .subscribe({
        next: (booking) => {
          this.router.navigate(['/bookings', booking.reference]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          if (err.status === 409) {
            const conflict = err.error as BookingConflict;
            this.submitError.set(conflict.message);
            this.markConflictingSeatsOccupied(conflict.conflictingSeats);
          } else if (err.status === 400) {
            this.submitError.set((err.error as { message?: string })?.message ?? 'Die Auswahl ist ungültig.');
          } else {
            this.submitError.set('Die Buchung konnte nicht abgeschlossen werden.');
          }
        }
      });
  }

  private markConflictingSeatsOccupied(conflictingSeats: { row: string; column: number }[]): void {
    const conflictKeys = new Set(conflictingSeats.map((s) => seatKey(s.row, s.column)));

    const map = this.seatMap();
    if (map) {
      this.seatMap.set({
        ...map,
        rows: map.rows.map((r) => ({
          row: r.row,
          seats: r.seats.map((s) =>
            conflictKeys.has(seatKey(r.row, s.column)) ? { ...s, status: 'occupied' as const } : s
          )
        }))
      });
    }

    const next = new Map(this.selectedSeats());
    for (const key of conflictKeys) {
      next.delete(key);
    }
    this.selectedSeats.set(next);
  }
}
