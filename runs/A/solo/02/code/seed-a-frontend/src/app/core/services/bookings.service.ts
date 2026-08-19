import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Booking, CreateBookingRequest } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingsService {
  private readonly http = inject(HttpClient);

  create(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>('/api/bookings', request);
  }

  getByReference(reference: string): Observable<Booking> {
    return this.http.get<Booking>(`/api/bookings/${reference}`);
  }

  cancel(reference: string): Observable<Booking> {
    return this.http.post<Booking>(`/api/bookings/${reference}/cancel`, {});
  }
}
