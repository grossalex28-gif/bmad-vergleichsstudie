import { Component, computed, effect, inject, input, signal, viewChildren } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

import { SeatMapService } from '../../../core/services/seat-map.service';
import { BookingService } from '../../../core/services/booking.service';
import { PriceCategory, Seat as SeatModel, SeatMap } from '../../../core/models/seat-map.model';
import { CreateBookingRequest } from '../../../core/models/booking.model';
import { ConflictProblemDetails, ValidationProblemDetails } from '../../../core/models/problem-details.model';
import { Seat } from '../seat/seat';
import { PriceCategoryPanel } from '../price-category-panel/price-category-panel';
import { SelectedSeatSummary, SummaryPanel } from '../summary-panel/summary-panel';
import { ErrorBanner } from '../../../shared/error-banner/error-banner';

interface GridCell {
  type: 'seat' | 'aisle';
  seat?: SeatModel;
}

interface Point {
  x: number;
  y: number;
}

const MIN_ZOOM = 0.5;
const MAX_ZOOM = 3;
const PLACEHOLDER_ROWS = 8;
const PLACEHOLDER_COLUMNS = 10;

function distance(a: Point, b: Point): number {
  return Math.hypot(a.x - b.x, a.y - b.y);
}

function midpoint(a: Point, b: Point): Point {
  return { x: (a.x + b.x) / 2, y: (a.y + b.y) / 2 };
}

function clamp(value: number, min: number, max: number): number {
  return Math.min(max, Math.max(min, value));
}

@Component({
  selector: 'app-sitzplan-page',
  imports: [Seat, PriceCategoryPanel, SummaryPanel, ErrorBanner],
  templateUrl: './sitzplan-page.html',
  styleUrl: './sitzplan-page.scss'
})
export class SitzplanPage {
  private readonly seatMapService = inject(SeatMapService);
  private readonly bookingService = inject(BookingService);
  private readonly router = inject(Router);

  readonly id = input.required<string>();

  protected readonly seatMap = signal<SeatMap | null>(null);
  protected readonly loadError = signal(false);
  protected readonly selectedSeatIds = signal<ReadonlySet<string>>(new Set());
  protected readonly focusedSeatId = signal<string | null>(null);
  protected readonly assignedCategoryId = signal<ReadonlyMap<string, string>>(new Map());
  private readonly panelDismissed = signal(false);
  private readonly seatComponents = viewChildren(Seat);

  protected readonly bookingName = signal('');
  protected readonly bookingEmail = signal('');
  protected readonly submitting = signal(false);
  protected readonly conflictMessage = signal<string | null>(null);
  protected readonly fieldErrors = signal<ReadonlySet<string>>(new Set());

  protected readonly priceCategories = computed(() => this.seatMap()?.priceCategories ?? []);

  protected readonly panelSeatId = computed<string | null>(() => {
    const id = this.focusedSeatId();
    if (!id || this.panelDismissed() || this.priceCategories().length <= 1) {
      return null;
    }
    return this.selectedSeatIds().has(id) ? id : null;
  });

  protected readonly panelSeatRow = computed<string | null>(() => {
    const id = this.panelSeatId();
    return id ? (this.seatMap()?.seats.find((s) => s.seatId === id)?.row ?? null) : null;
  });

  protected readonly zoom = signal(1);
  protected readonly panX = signal(0);
  protected readonly panY = signal(0);

  protected readonly transform = computed(() => `translate(${this.panX()}px, ${this.panY()}px) scale(${this.zoom()})`);

  protected readonly columnNumbers = computed<number[]>(() => {
    const map = this.seatMap();
    return map ? Array.from({ length: map.columns }, (_, index) => index + 1) : [];
  });

  private readonly seatByPosition = computed<Map<string, SeatModel>>(() => {
    const seatByPosition = new Map<string, SeatModel>();
    for (const seat of this.seatMap()?.seats ?? []) {
      seatByPosition.set(`${seat.row}:${seat.column}`, seat);
    }
    return seatByPosition;
  });

  protected readonly rowsWithCells = computed<{ row: string; cells: GridCell[] }[]>(() => {
    const map = this.seatMap();
    if (!map) {
      return [];
    }

    const seatByPosition = this.seatByPosition();
    const aisleColumns = new Set(map.aisleColumns);

    return map.rows.map((row) => ({
      row,
      cells: Array.from({ length: map.columns }, (_, index): GridCell => {
        const column = index + 1;
        if (aisleColumns.has(column)) {
          return { type: 'aisle' };
        }
        const seat = seatByPosition.get(`${row}:${column}`);
        return seat ? { type: 'seat', seat } : { type: 'aisle' };
      })
    }));
  });

  protected readonly placeholderCells = Array.from({ length: PLACEHOLDER_ROWS * PLACEHOLDER_COLUMNS });
  protected readonly placeholderColumns = PLACEHOLDER_COLUMNS;

  private readonly activePointers = new Map<number, Point>();
  private lastPanPoint: Point | null = null;
  private pinchStartDistance: number | null = null;
  private pinchStartZoom = 1;
  private pinchMidpoint: Point | null = null;

  constructor() {
    effect((onCleanup) => {
      this.seatMap.set(null);
      this.loadError.set(false);
      this.selectedSeatIds.set(new Set());
      this.focusedSeatId.set(null);
      this.assignedCategoryId.set(new Map());
      this.panelDismissed.set(false);

      const subscription = this.seatMapService.getSeatMap(this.id()).subscribe({
        next: (seatMap) => {
          this.seatMap.set(seatMap);
          this.setFocusedSeatId(seatMap.seats.find((s) => s.status === 'Free')?.seatId ?? null);
        },
        error: () => this.loadError.set(true)
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  private setFocusedSeatId(seatId: string | null): void {
    this.focusedSeatId.set(seatId);
    this.panelDismissed.set(false);
  }

  protected onSeatToggled(seatId: string): void {
    this.setFocusedSeatId(seatId);
    const wasSelected = this.selectedSeatIds().has(seatId);

    this.selectedSeatIds.update((current) => {
      const next = new Set(current);
      if (next.has(seatId)) {
        next.delete(seatId);
      } else {
        next.add(seatId);
      }
      return next;
    });

    if (wasSelected) {
      this.assignedCategoryId.update((current) => {
        const next = new Map(current);
        next.delete(seatId);
        return next;
      });
    } else if (this.priceCategories().length === 1) {
      const onlyCategoryId = this.priceCategories()[0].id;
      this.assignedCategoryId.update((current) => new Map(current).set(seatId, onlyCategoryId));
    }
  }

  protected onCategoryChosen(seatId: string, categoryId: string): void {
    this.assignedCategoryId.update((current) => new Map(current).set(seatId, categoryId));
  }

  protected categoryOf(seatId: string): PriceCategory | undefined {
    const categoryId = this.assignedCategoryId().get(seatId);
    return categoryId ? this.priceCategories().find((c) => c.id === categoryId) : undefined;
  }

  protected readonly selectedSeatSummaries = computed<SelectedSeatSummary[]>(() => {
    const map = this.seatMap();
    if (!map) {
      return [];
    }
    return Array.from(this.selectedSeatIds())
      .map((seatId) => map.seats.find((s) => s.seatId === seatId))
      .filter((seat): seat is SeatModel => seat !== undefined)
      .map((seat) => {
        const category = this.categoryOf(seat.seatId);
        return {
          seatId: seat.seatId,
          row: seat.row,
          column: seat.column,
          categoryName: category?.name ?? null,
          price: category?.price ?? 0
        };
      })
      .sort((a, b) => a.row.localeCompare(b.row) || a.column - b.column);
  });

  protected readonly totalPrice = computed<number>(() => {
    const totalCents = this.selectedSeatSummaries().reduce(
      (sum, seat) => sum + Math.round(seat.price * 100),
      0
    );
    return totalCents / 100;
  });

  protected readonly soldOut = computed<boolean>(() => {
    const map = this.seatMap();
    return map !== null && map.seats.length > 0 && !map.seats.some((seat) => seat.status === 'Free');
  });

  protected onGridKeydown(event: KeyboardEvent): void {
    if (event.key === 'Escape') {
      const seatId = this.panelSeatId();
      if (seatId) {
        event.preventDefault();
        this.panelDismissed.set(true);
        const seat = this.seatMap()?.seats.find((s) => s.seatId === seatId);
        if (seat) {
          this.seatComponents()
            .find((s) => s.row() === seat.row && s.column() === seat.column)
            ?.focusSeat();
        }
      }
      return;
    }

    const map = this.seatMap();
    const current = map?.seats.find((s) => s.seatId === this.focusedSeatId());
    if (!map || !current) {
      return;
    }

    const rowIndex = map.rows.indexOf(current.row);
    let nextSeat: SeatModel | undefined;

    switch (event.key) {
      case 'ArrowRight':
        for (let c = current.column + 1; c <= map.columns && !nextSeat; c++) {
          nextSeat = this.selectableSeatAt(current.row, c);
        }
        break;
      case 'ArrowLeft':
        for (let c = current.column - 1; c >= 1 && !nextSeat; c--) {
          nextSeat = this.selectableSeatAt(current.row, c);
        }
        break;
      case 'ArrowDown':
        for (let r = rowIndex + 1; r < map.rows.length && !nextSeat; r++) {
          nextSeat = this.selectableSeatAt(map.rows[r], current.column);
        }
        break;
      case 'ArrowUp':
        for (let r = rowIndex - 1; r >= 0 && !nextSeat; r--) {
          nextSeat = this.selectableSeatAt(map.rows[r], current.column);
        }
        break;
      default:
        return;
    }

    event.preventDefault();
    if (nextSeat) {
      this.setFocusedSeatId(nextSeat.seatId);
      this.seatComponents()
        .find((s) => s.row() === nextSeat!.row && s.column() === nextSeat!.column)
        ?.focusSeat();
    }
  }

  private selectableSeatAt(row: string, column: number): SeatModel | undefined {
    const seat = this.seatByPosition().get(`${row}:${column}`);
    return seat?.status === 'Free' ? seat : undefined;
  }

  protected onWheel(event: WheelEvent): void {
    event.preventDefault();
    if (event.deltaY === 0) {
      return;
    }
    const delta = event.deltaY > 0 ? -0.1 : 0.1;
    this.zoom.set(clamp(this.zoom() + delta, MIN_ZOOM, MAX_ZOOM));
  }

  protected onPointerDown(event: PointerEvent): void {
    (event.currentTarget as HTMLElement).setPointerCapture?.(event.pointerId);
    this.activePointers.set(event.pointerId, { x: event.clientX, y: event.clientY });

    if (this.activePointers.size === 1) {
      this.lastPanPoint = { x: event.clientX, y: event.clientY };
    } else if (this.activePointers.size === 2) {
      const [a, b] = Array.from(this.activePointers.values());
      this.pinchStartDistance = distance(a, b);
      this.pinchStartZoom = this.zoom();
      this.pinchMidpoint = midpoint(a, b);
    }
  }

  protected onPointerMove(event: PointerEvent): void {
    if (!this.activePointers.has(event.pointerId)) {
      return;
    }
    this.activePointers.set(event.pointerId, { x: event.clientX, y: event.clientY });

    if (this.activePointers.size === 2 && this.pinchStartDistance !== null && this.pinchMidpoint !== null) {
      const [a, b] = Array.from(this.activePointers.values());
      const currentDistance = distance(a, b);
      this.zoom.set(clamp(this.pinchStartZoom * (currentDistance / this.pinchStartDistance), MIN_ZOOM, MAX_ZOOM));

      const currentMidpoint = midpoint(a, b);
      this.panX.update((x) => x + (currentMidpoint.x - this.pinchMidpoint!.x));
      this.panY.update((y) => y + (currentMidpoint.y - this.pinchMidpoint!.y));
      this.pinchMidpoint = currentMidpoint;
    } else if (this.activePointers.size === 1 && event.buttons > 0 && this.lastPanPoint) {
      this.panX.update((x) => x + (event.clientX - this.lastPanPoint!.x));
      this.panY.update((y) => y + (event.clientY - this.lastPanPoint!.y));
      this.lastPanPoint = { x: event.clientX, y: event.clientY };
    }
  }

  protected onPointerUp(event: PointerEvent): void {
    (event.currentTarget as HTMLElement).releasePointerCapture?.(event.pointerId);
    this.activePointers.delete(event.pointerId);

    if (this.activePointers.size < 2) {
      this.pinchStartDistance = null;
      this.pinchMidpoint = null;
    }

    if (this.activePointers.size === 0) {
      this.lastPanPoint = null;
    } else {
      const [remaining] = Array.from(this.activePointers.values());
      this.lastPanPoint = remaining;
    }
  }

  protected onSubmitBooking(): void {
    const errors = new Set<string>();
    if (!this.bookingName().trim()) errors.add('name');
    if (!this.bookingEmail().trim()) errors.add('email');
    const summaries = this.selectedSeatSummaries();
    if (summaries.length === 0 || summaries.some((s) => s.categoryName === null)) {
      errors.add('seats');
    }
    if (errors.size > 0) {
      this.fieldErrors.set(errors);
      this.conflictMessage.set(null);
      return;
    }

    this.fieldErrors.set(new Set());
    this.conflictMessage.set(null);
    this.submitting.set(true);

    const map = this.seatMap();
    if (!map) return;

    const request: CreateBookingRequest = {
      eventId: this.id(),
      name: this.bookingName().trim(),
      email: this.bookingEmail().trim(),
      seats: summaries.map((s) => ({
        seatId: s.seatId,
        priceCategoryId: this.assignedCategoryId().get(s.seatId)!
      }))
    };

    this.bookingService.createBooking(request).subscribe({
      next: (booking) => {
        this.router.navigate(['/buchungen', booking.reference], { state: { booking, justBooked: true } });
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.status === 409) {
          const problem = err.error as ConflictProblemDetails;
          const labels = problem.conflictingSeats ?? [];
          this.conflictMessage.set(
            labels.length === 1
              ? `Platz ${labels[0]} ist inzwischen vergeben.`
              : `Plätze ${labels.join(', ')} sind inzwischen vergeben.`
          );
          this.reloadSeatMapAfterConflict(labels);
        } else if (err.status === 400) {
          const problem = err.error as ValidationProblemDetails;
          this.fieldErrors.set(new Set(Object.keys(problem.errors ?? {})));
        } else {
          this.conflictMessage.set('Buchung konnte nicht abgeschlossen werden. Bitte versuchen Sie es erneut.');
        }
      }
    });
  }

  private reloadSeatMapAfterConflict(conflictingLabels: string[]): void {
    this.seatMapService.getSeatMap(this.id()).subscribe({
      next: (seatMap) => {
        this.seatMap.set(seatMap);
        const conflictingSeatIds = new Set(
          seatMap.seats.filter((s) => conflictingLabels.includes(s.row + s.column)).map((s) => s.seatId)
        );
        this.selectedSeatIds.update((current) => new Set([...current].filter((id) => !conflictingSeatIds.has(id))));
        this.assignedCategoryId.update((current) => {
          const next = new Map(current);
          for (const id of conflictingSeatIds) next.delete(id);
          return next;
        });
      },
      // AC4 verlangt, dass Formularangaben und die übrige Auswahl erhalten bleiben — ein
      // fehlgeschlagener Reload darf die Seite daher nicht auf loadError() umschalten.
      error: () =>
        this.conflictMessage.set(
          'Sitzplan konnte nach dem Konflikt nicht aktualisiert werden. Bitte laden Sie die Seite neu.'
        )
    });
  }
}
