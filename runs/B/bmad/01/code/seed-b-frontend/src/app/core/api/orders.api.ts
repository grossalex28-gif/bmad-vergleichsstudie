import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface Delivery {
  name: string;
  street: string;
  postalCode: string;
  city: string;
  country: string;
  email: string;
  phone?: string;
}

export interface OrderItemLine {
  productId: string;
  supplierId: string;
  quantity: number;
}

export interface OrderCreateRequest {
  items: OrderItemLine[];
  delivery: Delivery;
}

export interface OrderItem {
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  unitPrice: number;
  quantity: number;
  lineTotal: number;
}

export interface InvalidOrderLine {
  productId: string;
  supplierId: string;
  reason: string;
}

export interface Order {
  publicId: string;
  status: string;
  delivery: Delivery;
  items: OrderItem[];
  totalAmount: number;
  createdAt: string;
}

@Injectable({ providedIn: 'root' })
export class OrdersApi {
  private readonly http = inject(HttpClient);

  createOrder(request: OrderCreateRequest): Observable<Order> {
    return this.http.post<Order>('/api/orders', request);
  }

  getOrder(publicId: string): Observable<Order> {
    return this.http.get<Order>(`/api/orders/${encodeURIComponent(publicId)}`);
  }
}
