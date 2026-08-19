import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CartService } from '../../../core/cart.service';
import { CartApiService } from '../cart-api.service';
import { Cart } from '../cart.model';

@Component({
  selector: 'app-cart-view',
  imports: [RouterLink],
  templateUrl: './cart-view.html'
})
export class CartView {
  private cartApi = inject(CartApiService);
  private cartService = inject(CartService);

  protected readonly cart = signal<Cart | null>(null);
  protected readonly loading = signal(true);
  protected readonly error = signal<string | null>(null);
  protected readonly busyKeys = signal<ReadonlySet<string>>(new Set());

  constructor() {
    this.loading.set(true);
    this.cartApi.getCart(this.cartService.cartId()).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Warenkorb konnte nicht geladen werden.');
      }
    });
  }

  protected lineKey(productId: string, supplierId: string): string {
    return `${productId}:${supplierId}`;
  }

  protected isBusy(productId: string, supplierId: string): boolean {
    return this.busyKeys().has(this.lineKey(productId, supplierId));
  }

  private setBusy(key: string, busy: boolean): void {
    const next = new Set(this.busyKeys());
    if (busy) {
      next.add(key);
    } else {
      next.delete(key);
    }
    this.busyKeys.set(next);
  }

  protected updateQuantity(productId: string, supplierId: string, quantityRaw: string): void {
    const cartId = this.cartService.cartId();
    const quantity = Number(quantityRaw);
    if (!cartId || !Number.isInteger(quantity) || quantity < 1) {
      this.error.set('Bitte eine gültige Menge (mindestens 1) angeben.');
      return;
    }

    const key = this.lineKey(productId, supplierId);
    this.setBusy(key, true);
    this.error.set(null);

    this.cartApi.updateQuantity(cartId, productId, supplierId, quantity).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.setBusy(key, false);
      },
      error: (err: HttpErrorResponse) => {
        this.setBusy(key, false);
        this.error.set(err.status === 409 ? 'Angebot nicht mehr verfügbar' : 'Menge konnte nicht geändert werden.');
      }
    });
  }

  protected removeItem(productId: string, supplierId: string): void {
    const cartId = this.cartService.cartId();
    if (!cartId) {
      return;
    }

    const key = this.lineKey(productId, supplierId);
    this.setBusy(key, true);
    this.error.set(null);

    this.cartApi.removeItem(cartId, productId, supplierId).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.setBusy(key, false);
      },
      error: () => {
        this.setBusy(key, false);
        this.error.set('Position konnte nicht entfernt werden.');
      }
    });
  }
}
