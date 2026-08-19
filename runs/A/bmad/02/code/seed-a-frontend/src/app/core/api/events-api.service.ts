import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { EventSummary } from './event-summary';
import { EventDetail } from './event-detail';
import { SeatMap } from './seat-map';

export interface EventsFilter {
  von?: string | null;
  bis?: string | null;
  venueId?: number | null;
}

@Injectable({ providedIn: 'root' })
export class EventsApiService {
  private readonly http = inject(HttpClient);

  getEvents(filter?: EventsFilter): Observable<EventSummary[]> {
    let params = new HttpParams();
    if (filter?.von) {
      params = params.set('von', filter.von);
    }
    if (filter?.bis) {
      params = params.set('bis', filter.bis);
    }
    if (filter?.venueId != null) {
      params = params.set('venueId', filter.venueId);
    }
    return this.http.get<EventSummary[]>('/api/events', { params });
  }

  getEvent(id: number): Observable<EventDetail> {
    return this.http.get<EventDetail>(`/api/events/${id}`);
  }

  getSeatMap(id: number): Observable<SeatMap> {
    return this.http.get<SeatMap>(`/api/events/${id}/seatmap`);
  }
}
