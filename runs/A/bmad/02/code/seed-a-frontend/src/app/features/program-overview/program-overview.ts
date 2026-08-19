import { Component, effect, inject, signal } from '@angular/core';

import { EventsApiService } from '../../core/api/events-api.service';
import { EventSummary } from '../../core/api/event-summary';
import { VenuesApiService } from '../../core/api/venues-api.service';
import { Venue } from '../../core/api/venue';
import { EventCard } from './event-card/event-card';
import { EventFilterBar } from './event-filter-bar/event-filter-bar';

@Component({
  selector: 'app-program-overview',
  imports: [EventCard, EventFilterBar],
  templateUrl: './program-overview.html',
  styleUrl: './program-overview.scss'
})
export class ProgramOverview {
  private readonly eventsApi = inject(EventsApiService);
  private readonly venuesApi = inject(VenuesApiService);

  readonly events = signal<EventSummary[]>([]);
  readonly loading = signal(true);
  readonly loadError = signal(false);
  readonly von = signal<string | null>(null);
  readonly bis = signal<string | null>(null);
  readonly venueId = signal<number | null>(null);
  readonly venues = signal<Venue[]>([]);

  constructor() {
    this.venuesApi.getVenues().subscribe(venues => this.venues.set(venues));

    effect(onCleanup => {
      const von = this.von();
      const bis = this.bis();
      const venueId = this.venueId();
      this.loading.set(true);
      this.loadError.set(false);
      const subscription = this.eventsApi.getEvents({ von, bis, venueId }).subscribe({
        next: events => {
          this.events.set(events);
          this.loading.set(false);
        },
        error: () => {
          this.loadError.set(true);
          this.loading.set(false);
        }
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }
}
