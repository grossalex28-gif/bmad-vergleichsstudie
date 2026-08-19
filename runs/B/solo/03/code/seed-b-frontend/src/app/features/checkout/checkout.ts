import { DecimalPipe } from '@angular/common';
import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { CartService } from '../../core/services/cart.service';
import { OrderService } from '../../core/services/order.service';
import { ApiProblem } from '../../core/models/order.model';

@Component({
  selector: 'app-checkout',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly cart = inject(CartService);
  private readonly orderService = inject(OrderService);
  private readonly router = inject(Router);

  protected readonly items = this.cart.items;
  protected readonly total = this.cart.total;

  protected readonly contactName = signal('');
  protected readonly email = signal('');
  protected readonly street = signal('');
  protected readonly postalCode = signal('');
  protected readonly city = signal('');

  protected readonly submitting = signal(false);
  protected readonly errorMessage = signal<string | undefined>(undefined);

  protected submit(): void {
    if (this.items().length === 0) {
      return;
    }

    this.submitting.set(true);
    this.errorMessage.set(undefined);

    this.orderService
      .create({
        contactName: this.contactName().trim(),
        email: this.email().trim(),
        street: this.street().trim(),
        postalCode: this.postalCode().trim(),
        city: this.city().trim(),
        items: this.items().map((item) => ({
          productId: item.productId,
          supplierId: item.supplierId,
          quantity: item.quantity,
        })),
      })
      .subscribe({
        next: (order) => {
          this.submitting.set(false);
          this.cart.clear();
          this.router.navigate(['/bestellungen', order.id]);
        },
        error: (err: HttpErrorResponse) => {
          this.submitting.set(false);
          const problem = err.error as ApiProblem | undefined;
          this.errorMessage.set(
            problem?.detail ?? problem?.title ?? 'Die Bestellung konnte nicht abgeschickt werden.',
          );
        },
      });
  }
}
