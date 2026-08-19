import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { Booking, CreateBookingRequest } from '../models/booking.model';

@Injectable({ providedIn: 'root' })
export class BookingService {
  private readonly http = inject(HttpClient);

  createBooking(request: CreateBookingRequest): Observable<Booking> {
    return this.http.post<Booking>('/api/bookings', request);
  }

  getBooking(reference: string): Observable<Booking> {
    return this.http.get<Booking>(`/api/bookings/${encodeURIComponent(reference)}`);
  }

  cancelBooking(reference: string): Observable<Booking> {
    return this.http.post<Booking>(`/api/bookings/${encodeURIComponent(reference)}/cancel`, {});
  }
}
