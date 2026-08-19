import { Component, inject, signal } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { HttpErrorResponse } from '@angular/common/http';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { OrderCreate, OrderValidationErrorResponse } from '../../core/models/order.model';

@Component({
  selector: 'app-checkout',
  imports: [DecimalPipe, FormsModule, RouterLink],
  templateUrl: './checkout.component.html',
  styleUrl: './checkout.component.scss',
})
export class CheckoutComponent {
  protected readonly cart = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  readonly recipientName = signal('');
  readonly street = signal('');
  readonly postalCode = signal('');
  readonly city = signal('');
  readonly email = signal('');

  readonly submitting = signal(false);
  readonly errors = signal<string[]>([]);

  submit(): void {
    const items = this.cart.cartItems();
    if (items.length === 0) {
      this.errors.set(['Der Warenkorb ist leer.']);
      return;
    }

    this.submitting.set(true);
    this.errors.set([]);

    const order: OrderCreate = {
      contact: {
        recipientName: this.recipientName().trim(),
        street: this.street().trim(),
        postalCode: this.postalCode().trim(),
        city: this.city().trim(),
        email: this.email().trim(),
      },
      items: items.map((i) => ({ productId: i.productId, supplierId: i.supplierId, quantity: i.quantity })),
    };

    this.orderService.createOrder(order).subscribe({
      next: (created) => {
        this.cart.clear();
        this.submitting.set(false);
        this.router.navigate(['/orders', created.id]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const body = err.error as OrderValidationErrorResponse | undefined;
        if (body?.errors?.length) {
          this.errors.set(body.errors.map((e) => e.message));
        } else {
          this.errors.set(['Die Bestellung konnte nicht abgeschlossen werden.']);
        }
      },
    });
  }
}
