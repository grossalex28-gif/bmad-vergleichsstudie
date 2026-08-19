import { DatePipe } from '@angular/common';
import { Component, effect, inject, input, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { EventService } from '../../../core/services/event.service';
import { EventDetail } from '../../../core/models/event.model';

@Component({
  selector: 'app-event-detail-page',
  imports: [RouterLink, DatePipe],
  templateUrl: './event-detail-page.html',
  styleUrl: './event-detail-page.scss'
})
export class EventDetailPage {
  private readonly eventService = inject(EventService);

  readonly id = input.required<string>();

  protected readonly event = signal<EventDetail | null>(null);
  protected readonly loadError = signal(false);

  constructor() {
    effect((onCleanup) => {
      this.event.set(null);
      this.loadError.set(false);

      const subscription = this.eventService.getEvent(this.id()).subscribe({
        next: (event) => this.event.set(event),
        error: () => this.loadError.set(true)
      });

      onCleanup(() => subscription.unsubscribe());
    });
  }
}
