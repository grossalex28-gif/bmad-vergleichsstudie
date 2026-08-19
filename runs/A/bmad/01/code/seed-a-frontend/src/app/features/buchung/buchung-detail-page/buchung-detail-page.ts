import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, computed, effect, inject, input, signal } from '@angular/core';
import { Router } from '@angular/router';

import { Booking } from '../../../core/models/booking.model';
import { BookingService } from '../../../core/services/booking.service';
import { formatEuroAmount } from '../../../shared/format-currency';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';
import { StatusBadge } from '../../../shared/status-badge/status-badge';
import { CancelDialog } from '../cancel-dialog/cancel-dialog';

@Component({
  selector: 'app-buchung-detail-page',
  imports: [DatePipe, ErrorBanner, StatusBadge, CancelDialog],
  templateUrl: './buchung-detail-page.html',
  styleUrl: './buchung-detail-page.scss'
})
export class BuchungDetailPage {
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);

  readonly referenz = input.required<string>();

  // Lazy computed() statt eagerly initialisiertem signal(): referenz() ist erst nach
  // Konstruktion gesetzt (Router-Input-Binding), ein Zugriff im Field-Initializer würde
  // NG0950 auslösen. computed() wertet erst beim ersten Lesen aus.
  private readonly navigationState = computed(() => {
    const state = this.router.getCurrentNavigation()?.extras.state ?? history.state;
    const typed = state as { booking?: Booking; justBooked?: boolean } | undefined;
    return typed?.booking && typed.booking.reference === this.referenz()
      ? { booking: typed.booking, justBooked: typed.justBooked === true }
      : null;
  });

  private readonly fetchedBooking = signal<Booking | null>(null);
  protected readonly loadError = signal<'not-found' | 'failed' | null>(null);

  private readonly cancelledBooking = signal<Booking | null>(null);
  protected readonly showCancelDialog = signal(false);
  protected readonly cancelling = signal(false);
  protected readonly cancelError = signal<string | null>(null);
  protected readonly justCancelled = signal(false);

  protected readonly booking = computed<Booking | null>(
    () => this.cancelledBooking() ?? this.navigationState()?.booking ?? this.fetchedBooking()
  );
  protected readonly justBooked = computed(
    () => this.cancelledBooking() === null && this.navigationState()?.justBooked === true
  );
  protected readonly copied = signal(false);

  protected readonly formatEuroAmount = formatEuroAmount;

  constructor() {
    effect((onCleanup) => {
      this.cancelledBooking.set(null);
      this.showCancelDialog.set(false);
      this.cancelling.set(false);
      this.cancelError.set(null);
      this.justCancelled.set(false);

      if (this.navigationState()) {
        return;
      }

      this.fetchedBooking.set(null);
      this.loadError.set(null);

      const subscription = this.bookingService.getBooking(this.referenz()).subscribe({
        next: (booking) => this.fetchedBooking.set(booking),
        error: (err: HttpErrorResponse) => this.loadError.set(err.status === 404 ? 'not-found' : 'failed')
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected copyReference(): void {
    navigator.clipboard.writeText(this.referenz()).then(() => {
      this.copied.set(true);
      setTimeout(() => this.copied.set(false), 2000);
    });
  }

  protected openCancelDialog(): void {
    this.cancelError.set(null);
    this.showCancelDialog.set(true);
  }

  protected closeCancelDialog(): void {
    this.showCancelDialog.set(false);
  }

  protected confirmCancel(): void {
    if (this.cancelling()) {
      return;
    }
    this.cancelling.set(true);
    this.cancelError.set(null);

    this.bookingService.cancelBooking(this.referenz()).subscribe({
      next: (booking) => {
        this.cancelling.set(false);
        this.showCancelDialog.set(false);
        this.cancelledBooking.set(booking);
        this.justCancelled.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.cancelling.set(false);
        this.showCancelDialog.set(false);

        if (err.status === 409) {
          this.bookingService.getBooking(this.referenz()).subscribe({
            next: (booking) => this.cancelledBooking.set(booking),
            error: () => this.cancelError.set('Stornierung fehlgeschlagen. Bitte versuchen Sie es erneut.')
          });
          return;
        }

        this.cancelError.set('Stornierung fehlgeschlagen. Bitte versuchen Sie es erneut.');
      }
    });
  }
}
