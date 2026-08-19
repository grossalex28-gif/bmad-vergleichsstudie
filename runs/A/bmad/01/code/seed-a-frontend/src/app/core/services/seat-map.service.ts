import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { SeatMap } from '../models/seat-map.model';

@Injectable({ providedIn: 'root' })
export class SeatMapService {
  private readonly http = inject(HttpClient);

  getSeatMap(eventId: string): Observable<SeatMap> {
    return this.http.get<SeatMap>(`/api/events/${encodeURIComponent(eventId)}/sitzplan`);
  }
}
