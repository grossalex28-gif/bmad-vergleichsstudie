import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';

import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss'
})
export class Checkout {
  protected readonly cartService = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  protected readonly name = signal('');
  protected readonly strasse = signal('');
  protected readonly plz = signal('');
  protected readonly ort = signal('');
  protected readonly land = signal('');
  protected readonly email = signal('');
  protected readonly telefon = signal('');

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal('');

  protected submitOrder(): void {
    const items = this.cartService.cartItems();
    if (items.length === 0) {
      this.errorMessage.set('Der Warenkorb ist leer.');
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set('');

    this.orderService
      .createOrder({
        lieferdaten: {
          name: this.name(),
          strasse: this.strasse(),
          plz: this.plz(),
          ort: this.ort(),
          land: this.land()
        },
        kontakt: {
          email: this.email(),
          telefon: this.telefon()
        },
        positionen: items.map((item) => ({
          produktId: item.produktId,
          lieferantId: item.lieferantId,
          menge: item.menge
        }))
      })
      .subscribe({
        next: (order) => {
          this.cartService.clear();
          this.router.navigate(['/orders', order.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          this.errorMessage.set(err.error?.error ?? 'Die Bestellung konnte nicht abgeschlossen werden.');
        }
      });
  }
}
