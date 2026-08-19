import { Component, computed, inject, signal } from '@angular/core';
import { HttpErrorResponse } from '@angular/common/http';
import { Router } from '@angular/router';

import { CartService } from '../cart/cart.service';
import { Delivery, InvalidOrderLine, OrdersApi } from '../../core/api/orders.api';

type DeliveryFormState = Delivery;

const EMPTY_DELIVERY: DeliveryFormState = {
  name: '', street: '', postalCode: '', city: '', country: '', email: '', phone: '',
};

const REASON_LABELS: Record<string, string> = {
  OFFER_NOT_FOUND: 'Dieses Angebot ist nicht mehr verfügbar.',
  INVALID_QUANTITY: 'Die angegebene Menge ist ungültig.',
};

@Component({
  selector: 'app-checkout',
  imports: [],
  templateUrl: './checkout.html',
  styleUrl: './checkout.scss',
})
export class Checkout {
  private readonly cartService = inject(CartService);
  private readonly ordersApi = inject(OrdersApi);
  private readonly router = inject(Router);

  protected readonly items = this.cartService.items;
  protected readonly totalAmount = this.cartService.totalAmount;
  protected readonly delivery = signal<DeliveryFormState>(EMPTY_DELIVERY);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly invalidLines = signal<InvalidOrderLine[]>([]);

  protected readonly canSubmit = computed(() => this.items().length > 0 && !this.submitting());

  protected updateField(field: keyof DeliveryFormState, value: string): void {
    this.delivery.update((d) => ({ ...d, [field]: value }));
  }

  protected labelFor(line: InvalidOrderLine): string {
    return REASON_LABELS[line.reason] ?? line.reason;
  }

  protected productNameFor(line: InvalidOrderLine): string {
    const item = this.items().find((i) => i.productId === line.productId && i.supplierId === line.supplierId);
    return item?.productName ?? line.productId;
  }

  protected submit(): void {
    if (!this.canSubmit()) {
      return;
    }
    this.submitting.set(true);
    this.submitError.set(null);
    this.invalidLines.set([]);
    const request = {
      items: this.items().map((i) => ({ productId: i.productId, supplierId: i.supplierId, quantity: i.quantity })),
      delivery: this.delivery(),
    };
    this.ordersApi.createOrder(request).subscribe({
      next: (order) => {
        this.cartService.clear();
        this.router.navigate(['/orders', order.publicId]);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        const problem = err.error as { detail?: string; invalidLines?: InvalidOrderLine[] } | null;
        this.submitError.set(
          problem?.detail ?? 'Die Bestellung konnte nicht abgeschlossen werden. Bitte überprüfen Sie Ihre Angaben.',
        );
        this.invalidLines.set(problem?.invalidLines ?? []);
      },
    });
  }
}
