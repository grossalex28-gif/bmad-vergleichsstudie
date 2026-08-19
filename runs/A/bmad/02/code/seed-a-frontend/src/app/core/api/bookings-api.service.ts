import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Booking, CreateBookingRequest } from './booking';

@Injectable({ providedIn: 'root' })
export class BookingsApiService {
  private readonly http = inject(HttpClient);

  createBooking(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>('/api/bookings', request);
  }

  getBooking(reference: string): Observable<Booking> {
    return this.http.get<Booking>(`/api/bookings/${reference}`);
  }

  cancelBooking(reference: string): Observable<Booking> {
    return this.http.post<Booking>(`/api/bookings/${reference}/cancel`, null);
  }
}
