import { Injectable, computed, signal } from '@angular/core';

export interface CartItem {
  productId: string;
  productName: string;
  supplierId: string;
  supplierName: string;
  unitPrice: number;
  quantity: number;
}

@Injectable({ providedIn: 'root' })
export class CartService {
  private readonly _items = signal<CartItem[]>([]);
  readonly items = this._items.asReadonly();
  readonly totalAmount = computed(() => this._items().reduce((sum, item) => sum + item.unitPrice * item.quantity, 0));

  addItem(item: {
    productId: string;
    productName: string;
    supplierId: string;
    supplierName: string;
    unitPrice: number;
    quantity: number;
  }): void {
    this._items.update((items) => {
      const index = items.findIndex((i) => i.productId === item.productId && i.supplierId === item.supplierId);
      if (index === -1) {
        return [...items, { ...item }];
      }
      const updated = [...items];
      updated[index] = { ...updated[index], quantity: updated[index].quantity + item.quantity };
      return updated;
    });
  }

  updateQuantity(productId: string, supplierId: string, quantity: number): boolean {
    if (!Number.isInteger(quantity) || quantity < 1) {
      return false;
    }
    const items = this._items();
    const index = items.findIndex((i) => i.productId === productId && i.supplierId === supplierId);
    if (index === -1) {
      return false;
    }
    const updated = [...items];
    updated[index] = { ...updated[index], quantity };
    this._items.set(updated);
    return true;
  }

  removeItem(productId: string, supplierId: string): void {
    this._items.update((items) => items.filter((i) => !(i.productId === productId && i.supplierId === supplierId)));
  }

  clear(): void {
    this._items.set([]);
  }
}
