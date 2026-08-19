import { CurrencyPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, ElementRef, effect, inject, signal, viewChild } from '@angular/core';
import { Router, RouterLink } from '@angular/router';

import { Booking } from '../../core/api/booking';
import { BookingsApiService } from '../../core/api/bookings-api.service';

@Component({
  selector: 'app-booking-detail',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './booking-detail.html',
  styleUrl: './booking-detail.scss'
})
export class BookingDetailPage {
  private readonly router = inject(Router);
  private readonly bookingsApi = inject(BookingsApiService);

  readonly booking = signal<Booking | null>(null);
  readonly notAvailable = signal(false);
  readonly copied = signal(false);

  readonly confirmingCancel = signal(false);
  readonly cancelling = signal(false);
  readonly cancelError = signal<string | null>(null);

  private readonly cancelDialog = viewChild<ElementRef<HTMLDialogElement>>('cancelDialog');

  constructor() {
    const state = this.router.getCurrentNavigation()?.extras.state as { booking?: Booking } | undefined;
    if (state?.booking) {
      this.booking.set(state.booking);
    } else {
      this.notAvailable.set(true);
    }

    effect(() => {
      const dialog = this.cancelDialog()?.nativeElement;
      if (!dialog) {
        return;
      }
      if (this.confirmingCancel()) {
        dialog.showModal();
      } else {
        dialog.close();
      }
    });
  }

  formattedReference(): string {
    const reference = this.booking()?.reference ?? '';
    return `${reference.slice(0, 4)}-${reference.slice(4, 8)}`;
  }

  async copyReference(): Promise<void> {
    const reference = this.booking()?.reference;
    if (!reference) {
      return;
    }
    try {
      await navigator.clipboard.writeText(reference);
      this.copied.set(true);
    } catch {
      this.copied.set(false);
    }
  }

  requestCancel(): void {
    this.cancelError.set(null);
    this.confirmingCancel.set(true);
  }

  dismissCancel(event?: Event): void {
    if (this.cancelling()) {
      event?.preventDefault();
      return;
    }
    this.confirmingCancel.set(false);
    this.cancelError.set(null);
  }

  confirmCancel(): void {
    if (this.cancelling()) {
      return;
    }
    const reference = this.booking()?.reference;
    if (!reference) {
      return;
    }

    this.cancelling.set(true);
    this.cancelError.set(null);

    this.bookingsApi.cancelBooking(reference).subscribe({
      next: aktualisierteBuchung => {
        this.booking.set(aktualisierteBuchung);
        this.cancelling.set(false);
        this.confirmingCancel.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.cancelling.set(false);
        if (err.error?.code === 'ALREADY_CANCELLED') {
          this.cancelError.set('Diese Buchung wurde bereits storniert.');
          this.bookingsApi.getBooking(reference).subscribe(aktuelleBuchung => this.booking.set(aktuelleBuchung));
        } else {
          this.cancelError.set('Die Stornierung konnte nicht durchgeführt werden. Bitte versuchen Sie es später erneut.');
        }
      }
    });
  }
}
