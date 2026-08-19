import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { CreateOrderRequest, OrderCreated, OrderDetail } from '../models/order.model';

@Injectable({ providedIn: 'root' })
export class OrderService {
  private readonly http = inject(HttpClient);

  createOrder(request: CreateOrderRequest): Observable<OrderCreated> {
    return this.http.post<OrderCreated>('/api/orders', request);
  }

  getOrder(id: string): Observable<OrderDetail> {
    return this.http.get<OrderDetail>(`/api/orders/${encodeURIComponent(id)}`);
  }
}
