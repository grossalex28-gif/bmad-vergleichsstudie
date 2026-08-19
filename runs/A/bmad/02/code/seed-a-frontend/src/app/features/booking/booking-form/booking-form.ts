import { Component, computed, inject, input, output, signal } from '@angular/core';

import { BookingSelectionService } from '../booking-selection.service';

const EMAIL_PATTERN = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;

@Component({
  selector: 'app-booking-form',
  imports: [],
  templateUrl: './booking-form.html',
  styleUrl: './booking-form.scss'
})
export class BookingForm {
  protected readonly selection = inject(BookingSelectionService);

  readonly submitting = input<boolean>(false);
  readonly submitted = output<{ name: string; email: string }>();

  readonly name = signal('');
  readonly email = signal('');
  protected readonly nameTouched = signal(false);
  protected readonly emailTouched = signal(false);

  readonly nameError = computed(() =>
    this.nameTouched() && this.name().trim() === '' ? 'Bitte geben Sie Ihren Namen ein.' : null
  );
  readonly emailError = computed(() =>
    this.emailTouched() && !EMAIL_PATTERN.test(this.email()) ? 'Bitte geben Sie eine gültige E-Mail-Adresse ein.' : null
  );

  readonly canSubmit = computed(() =>
    this.selection.allSelectedSeatsHaveCategory() &&
    this.name().trim() !== '' &&
    EMAIL_PATTERN.test(this.email()) &&
    !this.submitting()
  );

  onSubmit(): void {
    this.nameTouched.set(true);
    this.emailTouched.set(true);
    if (!this.canSubmit()) {
      return;
    }
    this.submitted.emit({ name: this.name().trim(), email: this.email() });
  }
}
