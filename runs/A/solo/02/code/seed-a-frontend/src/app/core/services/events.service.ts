import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { EventDetail, EventListItem } from '../models/event.model';
import { SeatMap } from '../models/seat-map.model';

export interface EventFilter {
  from?: string;
  to?: string;
  venueId?: string;
}

@Injectable({ providedIn: 'root' })
export class EventsService {
  private readonly http = inject(HttpClient);

  getAll(filter: EventFilter = {}): Observable<EventListItem[]> {
    let params = new HttpParams();
    if (filter.from) {
      params = params.set('from', filter.from);
    }
    if (filter.to) {
      params = params.set('to', filter.to);
    }
    if (filter.venueId) {
      params = params.set('venueId', filter.venueId);
    }

    return this.http.get<EventListItem[]>('/api/events', { params });
  }

  getById(id: string): Observable<EventDetail> {
    return this.http.get<EventDetail>(`/api/events/${id}`);
  }

  getSeatMap(id: string): Observable<SeatMap> {
    return this.http.get<SeatMap>(`/api/events/${id}/seatmap`);
  }
}
