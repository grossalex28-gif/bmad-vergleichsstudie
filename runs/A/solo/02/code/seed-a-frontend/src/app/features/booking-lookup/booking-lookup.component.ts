import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router } from '@angular/router';

@Component({
  selector: 'app-booking-lookup',
  imports: [FormsModule],
  templateUrl: './booking-lookup.component.html',
  styleUrl: './booking-lookup.component.scss'
})
export class BookingLookupComponent {
  private readonly router = inject(Router);

  protected reference = '';
  protected readonly error = signal<string | null>(null);

  protected lookup(): void {
    const trimmed = this.reference.trim().toUpperCase();
    if (!trimmed) {
      this.error.set('Bitte eine Buchungsreferenz eingeben.');
      return;
    }
    this.error.set(null);
    this.router.navigate(['/bookings', trimmed]);
  }
}
