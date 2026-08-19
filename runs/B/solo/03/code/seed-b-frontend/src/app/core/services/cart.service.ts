import { Injectable, computed, signal } from '@angular/core';
import { CartItem } from '../models/cart.model';

const STORAGE_KEY = 'seed-b-cart';

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly items$ = signal<CartItem[]>(this.loadFromStorage());

  readonly items = this.items$.asReadonly();
  readonly itemCount = computed(() => this.items$().reduce((sum, item) => sum + item.quantity, 0));
  readonly total = computed(() => this.items$().reduce((sum, item) => sum + item.unitPrice * item.quantity, 0));

  addItem(item: CartItem): void {
    this.items$.update((items) => {
      const existing = items.find((i) => i.productId === item.productId && i.supplierId === item.supplierId);
      if (existing) {
        return items.map((i) =>
          i === existing ? { ...i, quantity: i.quantity + item.quantity, unitPrice: item.unitPrice } : i,
        );
      }
      return [...items, item];
    });
    this.persist();
  }

  updateQuantity(productId: string, supplierId: string, quantity: number): void {
    if (quantity < 1) {
      return;
    }
    this.items$.update((items) =>
      items.map((i) => (i.productId === productId && i.supplierId === supplierId ? { ...i, quantity } : i)),
    );
    this.persist();
  }

  removeItem(productId: string, supplierId: string): void {
    this.items$.update((items) => items.filter((i) => !(i.productId === productId && i.supplierId === supplierId)));
    this.persist();
  }

  clear(): void {
    this.items$.set([]);
    this.persist();
  }

  private persist(): void {
    localStorage.setItem(STORAGE_KEY, JSON.stringify(this.items$()));
  }

  private loadFromStorage(): CartItem[] {
    try {
      const raw = localStorage.getItem(STORAGE_KEY);
      return raw ? (JSON.parse(raw) as CartItem[]) : [];
    } catch {
      return [];
    }
  }
}
