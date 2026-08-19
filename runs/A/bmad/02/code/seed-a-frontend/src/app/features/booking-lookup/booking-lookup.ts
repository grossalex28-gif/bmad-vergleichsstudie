import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

import { BookingsApiService } from '../../core/api/bookings-api.service';

const REFERENCE_PATTERN = /^[23456789ABCDEFGHJKMNPQRSTUVWXYZ]{8}$/;

function normalizeReference(raw: string): string {
  return raw.replace(/[\s-]/g, '').toUpperCase();
}

@Component({
  selector: 'app-booking-lookup',
  imports: [],
  templateUrl: './booking-lookup.html',
  styleUrl: './booking-lookup.scss'
})
export class BookingLookup {
  private readonly bookingsApi = inject(BookingsApiService);
  private readonly router = inject(Router);

  readonly reference = signal('');
  readonly touched = signal(false);
  readonly submitting = signal(false);
  readonly submitError = signal<string | null>(null);

  readonly referenceError = computed(() =>
    this.touched() && !REFERENCE_PATTERN.test(normalizeReference(this.reference()))
      ? 'Bitte geben Sie eine gültige Buchungsreferenz ein (8 Zeichen, z. B. AB4F-7Q2K).'
      : null
  );

  onSubmit(): void {
    if (this.submitting()) {
      return;
    }

    this.touched.set(true);
    this.submitError.set(null);

    const normalized = normalizeReference(this.reference());
    if (!REFERENCE_PATTERN.test(normalized)) {
      return;
    }

    this.submitting.set(true);

    this.bookingsApi.getBooking(normalized).subscribe({
      next: booking => {
        this.router.navigate(['/buchungen', booking.reference], { state: { booking } }).then(navigated => {
          if (!navigated) {
            this.submitting.set(false);
          }
        });
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.error?.code === 'BOOKING_NOT_FOUND') {
          this.submitError.set('Buchungsreferenz nicht gefunden.');
        } else {
          this.submitError.set('Die Buchung konnte nicht abgerufen werden. Bitte versuchen Sie es später erneut.');
        }
      }
    });
  }
}
