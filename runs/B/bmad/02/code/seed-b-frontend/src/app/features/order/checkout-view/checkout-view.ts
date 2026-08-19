import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { CartApiService } from '../../cart/cart-api.service';
import { Cart } from '../../cart/cart.model';
import { CartService } from '../../../core/cart.service';
import { OrderApiService } from '../order-api.service';
import { OrderRejection } from '../order.model';

const REASON_TEXT: Record<string, string> = {
  unzulaessige_menge: 'unzulässige Menge',
  angebot_nicht_mehr_verfuegbar: 'Angebot nicht mehr verfügbar'
};

@Component({
  selector: 'app-checkout-view',
  templateUrl: './checkout-view.html'
})
export class CheckoutView {
  private cartApi = inject(CartApiService);
  private orderApi = inject(OrderApiService);
  private cartService = inject(CartService);
  private router = inject(Router);

  protected readonly cart = signal<Cart | null>(null);
  protected readonly loading = signal(true);
  protected readonly loadError = signal<string | null>(null);

  protected readonly customerName = signal('');
  protected readonly deliveryAddress = signal('');
  protected readonly email = signal('');
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly rejectionLines = signal<string[]>([]);

  constructor() {
    this.cartApi.getCart(this.cartService.cartId()).subscribe({
      next: (cart) => {
        this.cart.set(cart);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.loadError.set('Warenkorb konnte nicht geladen werden.');
      }
    });
  }

  private describeLine(productId: string, supplierId: string): string {
    const item = this.cart()?.items.find((i) => i.productId === productId && i.supplierId === supplierId);
    return item ? `${item.productName} (${item.supplierName})` : `${productId}/${supplierId}`;
  }

  protected placeOrder(): void {
    if (this.submitting()) {
      return;
    }

    const cartId = this.cartService.cartId();
    const name = this.customerName().trim();
    const address = this.deliveryAddress().trim();
    const mail = this.email().trim();
    if (!cartId || !name || !address || !mail) {
      this.submitError.set('Bitte Name, Lieferadresse und E-Mail-Adresse angeben.');
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    this.rejectionLines.set([]);

    this.orderApi.placeOrder(cartId, name, address, mail).subscribe({
      next: (order) => {
        this.router.navigate(['/orders', order.orderId]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        if (err.status === 409) {
          const rejection = err.error as OrderRejection;
          this.submitError.set(rejection.error);
          this.rejectionLines.set(
            rejection.lines.map(
              (l) => `${this.describeLine(l.productId, l.supplierId)}: ${REASON_TEXT[l.reason] ?? l.reason}`
            )
          );
        } else {
          this.submitError.set('Bestellung konnte nicht angelegt werden. Bitte Eingaben prüfen.');
        }
      }
    });
  }
}
