import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { OrderDetail } from '../../../core/models/order.model';
import { OrderService } from '../../../core/services/order.service';

@Component({
  selector: 'app-order-confirmation',
  imports: [CurrencyPipe],
  templateUrl: './order-confirmation.html',
  styleUrl: './order-confirmation.scss'
})
export class OrderConfirmation implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly orderService = inject(OrderService);

  readonly order = signal<OrderDetail | null>(null);
  readonly notFound = signal(false);
  readonly error = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.load(params.get('id')!));
  }

  private load(id: string): void {
    this.order.set(null);
    this.notFound.set(false);
    this.error.set(false);
    this.orderService.getOrder(id).subscribe({
      next: (order) => this.order.set(order),
      error: (err) => {
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set(true);
        }
      }
    });
  }
}
