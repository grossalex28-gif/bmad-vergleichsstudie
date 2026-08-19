import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Venue } from '../models/venue.model';

@Injectable({ providedIn: 'root' })
export class VenuesService {
  private readonly http = inject(HttpClient);

  getAll(): Observable<Venue[]> {
    return this.http.get<Venue[]>('/api/venues');
  }
}
