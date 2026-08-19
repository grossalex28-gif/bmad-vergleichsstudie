import { Component, ElementRef, computed, effect, inject, input, signal, viewChild } from '@angular/core';

import { SeatMap, SeatMapCell } from '../../../core/api/seat-map';
import { BookingSelectionService } from '../booking-selection.service';

interface FocusTarget {
  rowIndex: number;
  columnIndex: number;
}

const KEY_DELTAS: Record<string, { rowDelta: number; columnDelta: number }> = {
  ArrowUp: { rowDelta: -1, columnDelta: 0 },
  ArrowDown: { rowDelta: 1, columnDelta: 0 },
  ArrowLeft: { rowDelta: 0, columnDelta: -1 },
  ArrowRight: { rowDelta: 0, columnDelta: 1 }
};

@Component({
  selector: 'app-seat-map',
  imports: [],
  templateUrl: './seat-map.html',
  styleUrl: './seat-map.scss'
})
export class SeatMapGrid {
  readonly seatMap = input.required<SeatMap>();

  private readonly selection = inject(BookingSelectionService);

  private readonly root = viewChild.required<ElementRef<HTMLElement>>('root');

  private readonly focusTarget = signal<FocusTarget | null>(null);

  private readonly firstFreeTarget = computed<FocusTarget | null>(() => {
    const rows = this.seatMap().rows;
    for (let rowIndex = 0; rowIndex < rows.length; rowIndex++) {
      const columnIndex = rows[rowIndex].cells.findIndex(cell => cell.status === 'free');
      if (columnIndex !== -1) {
        return { rowIndex, columnIndex };
      }
    }
    return null;
  });

  readonly hasFreeSeats = computed(() => this.firstFreeTarget() !== null);

  constructor() {
    effect(() => {
      const target = this.focusTarget();
      if (target === null) {
        return;
      }
      const selector = `[data-row-index="${target.rowIndex}"][data-column-index="${target.columnIndex}"]`;
      this.root().nativeElement.querySelector<HTMLButtonElement>(selector)?.focus();
    });
  }

  isRovingTabTarget(rowIndex: number, columnIndex: number): boolean {
    const current = this.focusTarget() ?? this.firstFreeTarget();
    return current !== null && current.rowIndex === rowIndex && current.columnIndex === columnIndex;
  }

  ariaLabel(rowLabel: string, cell: SeatMapCell): string {
    const statusText = cell.status === 'occupied'
      ? 'belegt'
      : this.isSelected(rowLabel, cell.columnNumber) ? 'ausgewählt' : 'frei';
    return `Reihe ${rowLabel}, Platz ${cell.columnNumber}, ${statusText}`;
  }

  isSelected(rowLabel: string, columnNumber: number): boolean {
    return this.selection.isSelected(rowLabel, columnNumber);
  }

  onToggle(rowLabel: string, columnNumber: number): void {
    this.selection.toggle(rowLabel, columnNumber);
  }

  onFocus(rowIndex: number, columnIndex: number): void {
    this.focusTarget.set({ rowIndex, columnIndex });
  }

  onKeydown(event: KeyboardEvent, rowIndex: number, columnIndex: number): void {
    const delta = KEY_DELTAS[event.key];
    if (!delta) {
      return;
    }
    event.preventDefault();
    const next = this.findNextFocusable(rowIndex, columnIndex, delta.rowDelta, delta.columnDelta);
    if (next) {
      this.focusTarget.set(next);
    }
  }

  private findNextFocusable(rowIndex: number, columnIndex: number, rowDelta: number, columnDelta: number): FocusTarget | null {
    const rows = this.seatMap().rows;
    let r = rowIndex + rowDelta;
    let c = columnIndex + columnDelta;
    while (r >= 0 && r < rows.length && c >= 0 && c < rows[r].cells.length) {
      if (rows[r].cells[c].status === 'free') {
        return { rowIndex: r, columnIndex: c };
      }
      r += rowDelta;
      c += columnDelta;
    }
    return null;
  }
}
