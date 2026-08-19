import { Component, computed, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';

import { EventService } from '../../../core/services/event.service';
import { VenueService } from '../../../core/services/venue.service';
import { EventListItem } from '../../../core/models/event.model';
import { VenueListItem } from '../../../core/models/venue.model';
import { EventCard } from '../event-card/event-card';
import { FilterBar } from '../filter-bar/filter-bar';

interface ActiveFilterChip {
  key: string;
  label: string;
  onClear: () => void;
}

@Component({
  selector: 'app-event-list-page',
  imports: [EventCard, FilterBar],
  templateUrl: './event-list-page.html',
  styleUrl: './event-list-page.scss'
})
export class EventListPage {
  private readonly eventService = inject(EventService);
  private readonly venueService = inject(VenueService);

  protected readonly events = signal<EventListItem[] | null>(null);
  protected readonly loadError = signal(false);
  protected readonly placeholderCount = 5;
  protected readonly placeholders = Array.from({ length: this.placeholderCount });

  protected readonly von = signal<string | null>(null);
  protected readonly bis = signal<string | null>(null);
  protected readonly venueId = signal<string | null>(null);
  protected readonly venues = signal<VenueListItem[]>([]);

  protected readonly hasActiveFilter = computed(() => this.von() !== null || this.bis() !== null || this.venueId() !== null);

  protected readonly activeFilterChips = computed<ActiveFilterChip[]>(() => {
    const chips: ActiveFilterChip[] = [];

    const von = this.von();
    if (von !== null) {
      chips.push({ key: 'von', label: `Von ${this.formatDate(von)}`, onClear: () => this.von.set(null) });
    }

    const bis = this.bis();
    if (bis !== null) {
      chips.push({ key: 'bis', label: `Bis ${this.formatDate(bis)}`, onClear: () => this.bis.set(null) });
    }

    const venueId = this.venueId();
    if (venueId !== null) {
      const venueName = this.venues().find((venue) => venue.id === venueId)?.name ?? venueId;
      chips.push({ key: 'venue', label: venueName, onClear: () => this.venueId.set(null) });
    }

    return chips;
  });

  constructor() {
    this.venueService
      .getVenues()
      .pipe(takeUntilDestroyed())
      .subscribe({
        next: (venues) => this.venues.set(venues),
        error: () => this.venues.set([])
      });

    effect((onCleanup) => {
      const from = this.von() ?? undefined;
      const to = this.bis() ?? undefined;
      const venueId = this.venueId() ?? undefined;

      this.events.set(null);
      this.loadError.set(false);

      const subscription = this.eventService.getEvents({ from, to, venueId }).subscribe({
        next: (events) => this.events.set(events),
        error: () => this.loadError.set(true)
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }

  protected resetFilters(): void {
    this.von.set(null);
    this.bis.set(null);
    this.venueId.set(null);
  }

  private formatDate(isoDate: string): string {
    const [year, month, day] = isoDate.split('-');
    return `${day}.${month}.${year}`;
  }
}
