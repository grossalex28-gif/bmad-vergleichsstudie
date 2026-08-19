import { Injectable, computed, effect, signal } from '@angular/core';
import { CartItem } from '../models/cart.model';

const STORAGE_KEY = 'seed-b-cart';

function loadFromStorage(): CartItem[] {
  try {
    const raw = localStorage.getItem(STORAGE_KEY);
    return raw ? (JSON.parse(raw) as CartItem[]) : [];
  } catch {
    return [];
  }
}

// Cart lives entirely on the client: customers shop and check out without an
// account, in a single sitting, so there is nothing to persist server-side
// until an order is actually placed (B-F10).
@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly items = signal<CartItem[]>(loadFromStorage());

  readonly cartItems = this.items.asReadonly();
  readonly itemCount = computed(() => this.items().reduce((sum, i) => sum + i.quantity, 0));
  readonly total = computed(() => this.items().reduce((sum, i) => sum + i.unitPrice * i.quantity, 0));

  constructor() {
    effect(() => {
      localStorage.setItem(STORAGE_KEY, JSON.stringify(this.items()));
    });
  }

  addItem(item: Omit<CartItem, 'quantity'>, quantity: number): void {
    if (quantity < 1) return;
    this.items.update((current) => {
      const existingIndex = current.findIndex(
        (i) => i.productId === item.productId && i.supplierId === item.supplierId,
      );
      if (existingIndex >= 0) {
        const updated = [...current];
        updated[existingIndex] = {
          ...updated[existingIndex],
          quantity: updated[existingIndex].quantity + quantity,
        };
        return updated;
      }
      return [...current, { ...item, quantity }];
    });
  }

  updateQuantity(productId: number, supplierId: number, quantity: number): void {
    if (quantity < 1) {
      this.removeItem(productId, supplierId);
      return;
    }
    this.items.update((current) =>
      current.map((i) => (i.productId === productId && i.supplierId === supplierId ? { ...i, quantity } : i)),
    );
  }

  removeItem(productId: number, supplierId: number): void {
    this.items.update((current) => current.filter((i) => !(i.productId === productId && i.supplierId === supplierId)));
  }

  clear(): void {
    this.items.set([]);
  }
}
