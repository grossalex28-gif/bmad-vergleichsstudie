import { Injectable, computed, signal } from '@angular/core';

import { PriceCategory } from '../../core/api/event-detail';

export interface SelectedSeat {
  rowLabel: string;
  columnNumber: number;
  priceCategoryId: number | null;
}

@Injectable()
export class BookingSelectionService {
  private readonly selectedSeatsState = signal<SelectedSeat[]>([]);
  private readonly priceCategoriesState = signal<PriceCategory[]>([]);

  readonly selectedSeats = this.selectedSeatsState.asReadonly();

  readonly allSelectedSeatsHaveCategory = computed(() =>
    this.selectedSeatsState().length > 0 &&
    this.selectedSeatsState().every(seat => seat.priceCategoryId !== null)
  );

  readonly totalPrice = computed(() => {
    const categories = this.priceCategoriesState();
    return this.selectedSeatsState().reduce((sum, seat) => {
      const category = categories.find(c => c.id === seat.priceCategoryId);
      return sum + (category?.preis ?? 0);
    }, 0);
  });

  isSelected(rowLabel: string, columnNumber: number): boolean {
    return this.selectedSeatsState().some(seat => seat.rowLabel === rowLabel && seat.columnNumber === columnNumber);
  }

  toggle(rowLabel: string, columnNumber: number): void {
    this.selectedSeatsState.update(seats =>
      seats.some(seat => seat.rowLabel === rowLabel && seat.columnNumber === columnNumber)
        ? seats.filter(seat => !(seat.rowLabel === rowLabel && seat.columnNumber === columnNumber))
        : [...seats, { rowLabel, columnNumber, priceCategoryId: null }]
    );
  }

  assignCategory(rowLabel: string, columnNumber: number, priceCategoryId: number): void {
    this.selectedSeatsState.update(seats =>
      seats.map(seat =>
        seat.rowLabel === rowLabel && seat.columnNumber === columnNumber
          ? { ...seat, priceCategoryId }
          : seat
      )
    );
  }

  setPriceCategories(categories: PriceCategory[]): void {
    this.priceCategoriesState.set(categories);
  }

  removeSeats(seats: { rowLabel: string; columnNumber: number }[]): void {
    this.selectedSeatsState.update(current =>
      current.filter(seat => !seats.some(s => s.rowLabel === seat.rowLabel && s.columnNumber === seat.columnNumber))
    );
  }
}
