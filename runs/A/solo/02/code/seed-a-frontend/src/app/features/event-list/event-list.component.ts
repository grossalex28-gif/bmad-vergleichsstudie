import { DatePipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';

import { EventsService } from '../../core/services/events.service';
import { VenuesService } from '../../core/services/venues.service';
import { EventListItem } from '../../core/models/event.model';
import { Venue } from '../../core/models/venue.model';

@Component({
  selector: 'app-event-list',
  imports: [FormsModule, RouterLink, DatePipe],
  templateUrl: './event-list.component.html',
  styleUrl: './event-list.component.scss'
})
export class EventListComponent implements OnInit {
  private readonly eventsService = inject(EventsService);
  private readonly venuesService = inject(VenuesService);

  protected readonly events = signal<EventListItem[]>([]);
  protected readonly venues = signal<Venue[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal<string | null>(null);

  protected from = '';
  protected to = '';
  protected venueId = '';

  ngOnInit(): void {
    this.venuesService.getAll().subscribe({
      next: (venues) => this.venues.set(venues)
    });
    this.loadEvents();
  }

  protected applyFilters(): void {
    this.loadEvents();
  }

  protected resetFilters(): void {
    this.from = '';
    this.to = '';
    this.venueId = '';
    this.loadEvents();
  }

  private loadEvents(): void {
    this.loading.set(true);
    this.error.set(null);
    this.eventsService
      .getAll({
        from: this.from || undefined,
        to: this.to || undefined,
        venueId: this.venueId || undefined
      })
      .subscribe({
        next: (events) => {
          this.events.set(events);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Die Veranstaltungen konnten nicht geladen werden.');
          this.loading.set(false);
        }
      });
  }
}
