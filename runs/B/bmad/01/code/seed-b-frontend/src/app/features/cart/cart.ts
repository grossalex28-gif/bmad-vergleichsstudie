import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { RouterLink } from '@angular/router';

import { CartItem, CartService } from './cart.service';

@Component({
  selector: 'app-cart',
  imports: [DecimalPipe, RouterLink],
  templateUrl: './cart.html',
  styleUrl: './cart.scss',
})
export class Cart {
  private readonly cartService = inject(CartService);

  protected readonly items = this.cartService.items;
  protected readonly totalAmount = this.cartService.totalAmount;
  protected readonly quantityErrors = signal<Record<string, string>>({});

  protected lineTotal(item: CartItem): number {
    return item.unitPrice * item.quantity;
  }

  protected itemKey(item: CartItem): string {
    return `${item.productId}:${item.supplierId}`;
  }

  protected onQuantityChange(item: CartItem, input: HTMLInputElement): void {
    const quantity = +input.value;
    const key = this.itemKey(item);
    const ok = this.cartService.updateQuantity(item.productId, item.supplierId, quantity);
    this.quantityErrors.update((errors) => {
      const next = { ...errors };
      if (ok) {
        delete next[key];
      } else {
        next[key] = 'Ungültige Menge. Bitte eine ganze Zahl ab 1 eingeben.';
        input.value = String(item.quantity);
      }
      return next;
    });
  }

  protected onRemove(item: CartItem): void {
    const key = this.itemKey(item);
    this.cartService.removeItem(item.productId, item.supplierId);
    this.quantityErrors.update((errors) => {
      const next = { ...errors };
      delete next[key];
      return next;
    });
  }
}
