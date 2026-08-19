import { Injectable, signal } from '@angular/core';

import { CartItem } from '../../core/models/cart.model';

const CART_STORAGE_KEY = 'cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  readonly items = signal<CartItem[]>(this.loadFromStorage());

  addToCart(productId: string, supplierId: string, quantity: number): void {
    const current = this.items();
    const existingIndex = current.findIndex(
      (item) => item.productId === productId && item.supplierId === supplierId
    );
    const updated =
      existingIndex >= 0
        ? current.map((item, index) =>
            index === existingIndex ? { ...item, quantity: item.quantity + quantity } : item
          )
        : [...current, { productId, supplierId, quantity }];
    this.items.set(updated);
    this.persist(updated);
  }

  updateQuantity(productId: string, supplierId: string, quantity: number): void {
    const updated = this.items().map((item) =>
      item.productId === productId && item.supplierId === supplierId ? { ...item, quantity } : item
    );
    this.items.set(updated);
    this.persist(updated);
  }

  removeItem(productId: string, supplierId: string): void {
    const updated = this.items().filter(
      (item) => !(item.productId === productId && item.supplierId === supplierId)
    );
    this.items.set(updated);
    this.persist(updated);
  }

  clear(): void {
    this.items.set([]);
    this.persist([]);
  }

  private persist(items: CartItem[]): void {
    try {
      localStorage.setItem(CART_STORAGE_KEY, JSON.stringify(items));
    } catch {
      // z. B. Private-Browsing/Storage-Quota — In-Memory-Warenkorb bleibt trotzdem nutzbar
    }
  }

  private loadFromStorage(): CartItem[] {
    try {
      const raw = localStorage.getItem(CART_STORAGE_KEY);
      return raw ? (JSON.parse(raw) as CartItem[]) : [];
    } catch {
      return [];
    }
  }
}
