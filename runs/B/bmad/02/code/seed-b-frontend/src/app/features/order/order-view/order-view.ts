import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { OrderApiService } from '../order-api.service';
import { Order } from '../order.model';

@Component({
  selector: 'app-order-view',
  templateUrl: './order-view.html'
})
export class OrderView {
  private route = inject(ActivatedRoute);
  private orderApi = inject(OrderApiService);

  protected readonly order = signal<Order | null>(null);
  protected readonly notFound = signal(false);
  protected readonly loadError = signal<string | null>(null);
  protected readonly loading = signal(true);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }
    this.orderApi.getOrder(id).subscribe({
      next: (order) => {
        this.order.set(order);
        this.loading.set(false);
      },
      error: (err: HttpErrorResponse) => {
        this.loading.set(false);
        if (err.status === 404) {
          this.notFound.set(true);
        } else {
          this.loadError.set('Bestellung konnte nicht geladen werden.');
        }
      }
    });
  }
}
