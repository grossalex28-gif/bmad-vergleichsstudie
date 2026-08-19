import { Component, input, model, output, signal } from '@angular/core';

import { formatEuroAmount } from '../../../shared/format-currency';

export interface SelectedSeatSummary {
  seatId: string;
  row: string;
  column: number;
  categoryName: string | null;
  price: number;
}

@Component({
  selector: 'app-summary-panel',
  imports: [],
  templateUrl: './summary-panel.html',
  styleUrl: './summary-panel.scss'
})
export class SummaryPanel {
  readonly seats = input.required<SelectedSeatSummary[]>();
  readonly totalPrice = input.required<number>();
  readonly soldOut = input<boolean>(false);
  readonly name = model<string>('');
  readonly email = model<string>('');
  readonly submitting = input<boolean>(false);
  readonly fieldErrors = input<ReadonlySet<string>>(new Set());
  readonly submitBooking = output<void>();

  protected readonly expanded = signal(false);

  protected readonly formatEuroAmount = formatEuroAmount;

  protected toggleExpanded(): void {
    this.expanded.update((value) => !value);
  }

  protected onSubmit(event: Event): void {
    event.preventDefault();
    this.submitBooking.emit();
  }

  protected onNameInput(value: string): void {
    this.name.set(value);
  }

  protected onEmailInput(value: string): void {
    this.email.set(value);
  }
}
