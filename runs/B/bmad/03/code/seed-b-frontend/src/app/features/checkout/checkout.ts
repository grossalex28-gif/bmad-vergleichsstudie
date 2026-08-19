import { Component, computed, inject, signal } from '@angular/core';
import { Router } from '@angular/router';

import { OrderService } from '../../core/services/order.service';
import { CartService } from '../cart/cart.service';

@Component({
  selector: 'app-checkout',
  imports: [],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss'
})
export class Checkout {
  private readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly name = signal('');
  readonly street = signal('');
  readonly postalCode = signal('');
  readonly city = signal('');
  readonly country = signal('');
  readonly email = signal('');

  readonly submitting = signal(false);
  readonly errorMessage = signal<string | null>(null);

  readonly cartIsEmpty = computed(() => this.cartService.items().length === 0);

  onSubmit(): void {
    if (this.cartIsEmpty() || this.submitting()) {
      return;
    }
    this.submitting.set(true);
    this.errorMessage.set(null);
    this.orderService
      .createOrder({
        name: this.name().trim(),
        street: this.street().trim(),
        postalCode: this.postalCode().trim(),
        city: this.city().trim(),
        country: this.country().trim(),
        email: this.email().trim(),
        items: this.cartService.items()
      })
      .subscribe({
        next: (result) => {
          this.cartService.clear();
          this.submitting.set(false);
          this.router.navigate(['/orders', result.orderId]).catch(() => {
            this.errorMessage.set('Die Bestellung wurde angelegt, konnte aber nicht angezeigt werden.');
          });
        },
        error: (err) => {
          this.submitting.set(false);
          this.errorMessage.set(this.describeError(err?.error?.reason));
        }
      });
  }

  private describeError(reason: string | undefined): string {
    switch (reason) {
      case 'empty-cart':
        return 'Der Warenkorb ist leer.';
      case 'invalid-quantity':
        return 'Der Warenkorb enthält eine ungültige Menge.';
      case 'offer-unavailable':
        return 'Ein Angebot im Warenkorb ist nicht mehr verfügbar.';
      default:
        return 'Die Bestellung konnte nicht angelegt werden.';
    }
  }
}
