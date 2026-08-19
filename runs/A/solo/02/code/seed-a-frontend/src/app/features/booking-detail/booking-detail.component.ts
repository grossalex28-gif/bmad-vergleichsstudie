import { DatePipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';

import { BookingsService } from '../../core/services/bookings.service';
import { Booking } from '../../core/models/booking.model';

@Component({
  selector: 'app-booking-detail',
  imports: [DatePipe, DecimalPipe, RouterLink],
  templateUrl: './booking-detail.component.html',
  styleUrl: './booking-detail.component.scss'
})
export class BookingDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly bookingsService = inject(BookingsService);

  private reference = '';

  protected readonly booking = signal<Booking | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly cancelling = signal(false);

  ngOnInit(): void {
    this.reference = this.route.snapshot.paramMap.get('reference')!;
    this.loadBooking();
  }

  private loadBooking(): void {
    this.loading.set(true);
    this.error.set(null);
    this.bookingsService.getByReference(this.reference).subscribe({
      next: (booking) => {
        this.booking.set(booking);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Zu dieser Buchungsreferenz wurde keine Buchung gefunden.');
        this.loading.set(false);
      }
    });
  }

  protected cancelBooking(): void {
    if (this.cancelling()) {
      return;
    }
    this.cancelling.set(true);
    this.bookingsService.cancel(this.reference).subscribe({
      next: (booking) => {
        this.booking.set(booking);
        this.cancelling.set(false);
      },
      error: () => {
        this.error.set('Die Buchung konnte nicht storniert werden.');
        this.cancelling.set(false);
      }
    });
  }
}
