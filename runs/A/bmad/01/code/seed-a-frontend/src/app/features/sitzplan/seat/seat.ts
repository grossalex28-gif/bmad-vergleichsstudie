import { Component, computed, ElementRef, inject, input, output } from '@angular/core';

import { SeatStatus } from '../../../core/models/seat-map.model';
import { formatEuroAmount } from '../../../shared/format-currency';

@Component({
  selector: 'app-seat',
  imports: [],
  templateUrl: './seat.html',
  styleUrl: './seat.scss'
})
export class Seat {
  private readonly hostRef = inject(ElementRef<HTMLElement>);

  readonly row = input.required<string>();
  readonly column = input.required<number>();
  readonly status = input.required<SeatStatus>();
  readonly selected = input<boolean>(false);
  readonly focused = input<boolean>(false);
  readonly categoryName = input<string | null>(null);
  readonly categoryPrice = input<number | null>(null);

  readonly toggled = output<void>();

  protected readonly ariaLabel = computed(() => {
    const state = this.selected() ? 'ausgewählt' : this.status() === 'Free' ? 'frei' : 'belegt';
    const name = this.categoryName();
    const price = this.categoryPrice();
    const category = this.selected() && name !== null && price !== null ? `, ${name}, ${formatEuroAmount(price)}` : '';
    return `Reihe ${this.row()}, Platz ${this.column()}, ${state}${category}`;
  });

  protected onActivate(): void {
    if (this.status() === 'Occupied') {
      return;
    }
    this.focusSeat();
    this.toggled.emit();
  }

  protected onSpace(event: Event): void {
    event.preventDefault();
    this.onActivate();
  }

  public focusSeat(): void {
    (this.hostRef.nativeElement.querySelector('.seat') as HTMLElement | null)?.focus();
  }
}
