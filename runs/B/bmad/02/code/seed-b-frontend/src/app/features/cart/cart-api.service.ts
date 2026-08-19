import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../core/api-client';
import { Cart } from './cart.model';

@Injectable({ providedIn: 'root' })
export class CartApiService {
  private apiClient = inject(ApiClient);

  addItem(cartId: string | null, productId: string, supplierId: string, quantity: number): Observable<Cart> {
    const headers = cartId ? { 'X-Cart-Id': cartId } : undefined;
    return this.apiClient.post<Cart>('/cart/items', { productId, supplierId, quantity }, { headers });
  }

  getCart(cartId: string | null): Observable<Cart> {
    const headers = cartId ? { 'X-Cart-Id': cartId } : undefined;
    return this.apiClient.get<Cart>('/cart', undefined, headers);
  }

  updateQuantity(cartId: string, productId: string, supplierId: string, quantity: number): Observable<Cart> {
    return this.apiClient.patch<Cart>(`/cart/items/${productId}/${supplierId}`, { quantity }, { headers: { 'X-Cart-Id': cartId } });
  }

  removeItem(cartId: string, productId: string, supplierId: string): Observable<Cart> {
    return this.apiClient.delete<Cart>(`/cart/items/${productId}/${supplierId}`, { headers: { 'X-Cart-Id': cartId } });
  }
}
