import { Injectable, computed, signal } from '@angular/core';

import { CartItem } from '../models/cart-item.model';

const STORAGE_KEY = 'seed-b.cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly items = signal<CartItem[]>(loadFromStorage());

  readonly cartItems = this.items.asReadonly();
  readonly itemCount = computed(() => this.items().reduce((sum, item) => sum + item.menge, 0));
  readonly total = computed(() => this.items().reduce((sum, item) => sum + item.preis * item.menge, 0));

  addItem(item: CartItem): void {
    this.items.update((current) => {
      const existing = current.find((i) => i.produktId === item.produktId && i.lieferantId === item.lieferantId);
      const next = existing
        ? current.map((i) => (i === existing ? { ...i, menge: i.menge + item.menge } : i))
        : [...current, item];
      this.persist(next);
      return next;
    });
  }

  updateQuantity(produktId: string, lieferantId: string, menge: number): void {
    this.items.update((current) => {
      const next = current.map((i) =>
        i.produktId === produktId && i.lieferantId === lieferantId ? { ...i, menge } : i
      );
      this.persist(next);
      return next;
    });
  }

  removeItem(produktId: string, lieferantId: string): void {
    this.items.update((current) => {
      const next = current.filter((i) => !(i.produktId === produktId && i.lieferantId === lieferantId));
      this.persist(next);
      return next;
    });
  }

  clear(): void {
    this.items.set([]);
    this.persist([]);
  }

  private persist(items: CartItem[]): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(items));
  }
}

function loadFromStorage(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartItem[]) : [];
  } catch {
    return [];
  }
}
