import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { BookingService } from '../../../core/services/booking.service';

@Component({
  selector: 'app-buchung-abrufen-page',
  imports: [],
  templateUrl: './buchung-abrufen-page.html',
  styleUrl: './buchung-abrufen-page.scss'
})
export class BuchungAbrufenPage {
  private readonly router = inject(Router);
  private readonly bookingService = inject(BookingService);

  protected readonly reference = signal('');
  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | null>(null);

  protected onReferenceInput(value: string): void {
    this.reference.set(value);
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    if (this.submitting()) {
      return;
    }

    const reference = this.reference().trim();
    if (!reference) {
      this.errorMessage.set('Bitte geben Sie eine Buchungsreferenz ein.');
      return;
    }

    this.errorMessage.set(null);
    this.submitting.set(true);

    this.bookingService.getBooking(reference).subscribe({
      next: (booking) => {
        this.submitting.set(false);
        this.router.navigate(['/buchungen', booking.reference], { state: { booking } });
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.errorMessage.set(
          err.status === 404
            ? 'Keine Buchung mit dieser Referenz gefunden.'
            : 'Buchung konnte nicht abgerufen werden. Bitte versuchen Sie es erneut.'
        );
      }
    });
  }
}
