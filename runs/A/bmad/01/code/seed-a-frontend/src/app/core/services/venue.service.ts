import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { VenueListItem } from '../models/venue.model';

@Injectable({ providedIn: 'root' })
export class VenueService {
  private readonly http = inject(HttpClient);

  getVenues(): Observable<VenueListItem[]> {
    return this.http.get<VenueListItem[]>('/api/venues');
  }
}
