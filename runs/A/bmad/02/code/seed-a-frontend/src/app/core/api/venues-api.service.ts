import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Venue } from './venue';

@Injectable({ providedIn: 'root' })
export class VenuesApiService {
  private readonly http = inject(HttpClient);

  getVenues(): Observable<Venue[]> {
    return this.http.get<Venue[]>('/api/venues');
  }
}
