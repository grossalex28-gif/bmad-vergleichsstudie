import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../core/api-client';
import { Order } from './order.model';

@Injectable({ providedIn: 'root' })
export class OrderApiService {
  private apiClient = inject(ApiClient);

  placeOrder(cartId: string, customerName: string, deliveryAddress: string, email: string): Observable<Order> {
    return this.apiClient.post<Order>(
      '/orders',
      { customerName, deliveryAddress, email },
      { headers: { 'X-Cart-Id': cartId } }
    );
  }

  getOrder(id: string): Observable<Order> {
    return this.apiClient.get<Order>(`/orders/${id}`);
  }
}
