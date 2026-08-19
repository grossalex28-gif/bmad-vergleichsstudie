import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { EventDetail, EventListFilter, EventListItem } from '../models/event.model';

@Injectable({ providedIn: 'root' })
export class EventService {
  private readonly http = inject(HttpClient);

  getEvents(filter?: EventListFilter): Observable<EventListItem[]> {
    let params = new HttpParams();
    if (filter?.from) {
      params = params.set('from', filter.from);
    }
    if (filter?.to) {
      params = params.set('to', filter.to);
    }
    if (filter?.venueId) {
      params = params.set('venueId', filter.venueId);
    }
    return this.http.get<EventListItem[]>('/api/events', { params });
  }

  getEvent(id: string): Observable<EventDetail> {
    return this.http.get<EventDetail>(`/api/events/${encodeURIComponent(id)}`);
  }
}
