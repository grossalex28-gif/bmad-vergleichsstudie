import { Component, effect, inject, input, signal } from '@angular/core';
import { DatePipe, DecimalPipe } from '@angular/common';
import type { Subscription } from 'rxjs';

import { Order, OrdersApi } from '../../core/api/orders.api';

const STATUS_LABELS: Record<string, string> = {
  Received: 'Eingegangen',
};

@Component({
  selector: 'app-order-confirmation',
  imports: [DatePipe, DecimalPipe],
  templateUrl: './order-confirmation.html',
})
export class OrderConfirmation {
  private readonly ordersApi = inject(OrdersApi);
  private subscription: Subscription | undefined;

  readonly publicId = input.required<string>();

  protected readonly order = signal<Order | null>(null);
  protected readonly loading = signal(false);
  protected readonly notFound = signal(false);
  protected readonly error = signal(false);

  constructor() {
    effect((onCleanup) => {
      const publicId = this.publicId();
      this.loadOrder(publicId);
      onCleanup(() => this.subscription?.unsubscribe());
    });
  }

  protected statusLabel(status: string): string {
    return STATUS_LABELS[status] ?? status;
  }

  private loadOrder(publicId: string): void {
    this.order.set(null);
    this.notFound.set(false);
    this.error.set(false);
    this.loading.set(true);
    this.subscription?.unsubscribe();
    this.subscription = this.ordersApi.getOrder(publicId).subscribe({
      next: (order) => {
        if (this.publicId() !== publicId) {
          return;
        }
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err) => {
        if (this.publicId() !== publicId) {
          return;
        }
        this.loading.set(false);
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set(true);
        }
      },
    });
  }
}
