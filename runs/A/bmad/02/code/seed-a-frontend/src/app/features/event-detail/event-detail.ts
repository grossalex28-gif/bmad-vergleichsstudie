import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { DatePipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';

import { EventsApiService } from '../../core/api/events-api.service';
import { EventDetail } from '../../core/api/event-detail';

@Component({
  selector: 'app-event-detail',
  imports: [DatePipe, RouterLink],
  templateUrl: './event-detail.html',
  styleUrl: './event-detail.scss'
})
export class EventDetailPage {
  private readonly route = inject(ActivatedRoute);
  private readonly eventsApi = inject(EventsApiService);

  readonly event = signal<EventDetail | null>(null);
  readonly loading = signal(true);
  readonly notFound = signal(false);
  readonly loadError = signal(false);

  constructor() {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    // Backend-Route ist `{id:int}` (Int32) — größere Werte matchen dort keine Action und liefern
    // ein envelope-loses Framework-404 statt EVENT_NOT_FOUND, daher hier bereits abfangen.
    if (!Number.isInteger(id) || id > 2147483647) {
      this.loading.set(false);
      this.notFound.set(true);
      return;
    }

    this.eventsApi.getEvent(id).subscribe({
      next: event => {
        this.event.set(event);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.error?.code === 'EVENT_NOT_FOUND') {
          this.notFound.set(true);
        } else {
          this.loadError.set(true);
        }
      }
    });
  }
}
