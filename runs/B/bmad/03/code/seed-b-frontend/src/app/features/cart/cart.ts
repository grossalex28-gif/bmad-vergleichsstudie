import { CurrencyPipe } from '@angular/common';
import { Component, OnInit, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';
import { forkJoin } from 'rxjs';

import { ProductService } from '../../core/services/product.service';
import { CartService } from './cart.service';

interface CartLine {
  productId: string;
  supplierId: string;
  productName: string;
  supplierName: string;
  price: number;
  quantity: number;
}

@Component({
  selector: 'app-cart',
  imports: [CurrencyPipe, RouterLink],
  templateUrl: './cart.html',
  styleUrl: './cart.scss'
})
export class Cart implements OnInit {
  private readonly cartService = inject(CartService);
  private readonly productService = inject(ProductService);

  readonly lines = signal<CartLine[]>([]);
  readonly loading = signal(true);
  readonly error = signal(false);

  readonly total = computed(() => this.lines().reduce((sum, line) => sum + line.price * line.quantity, 0));

  ngOnInit(): void {
    this.load();
  }

  private load(): void {
    const items = this.cartService.items();
    if (items.length === 0) {
      this.lines.set([]);
      this.loading.set(false);
      this.error.set(false);
      return;
    }
    this.loading.set(true);
    this.error.set(false);
    const distinctProductIds = [...new Set(items.map((item) => item.productId))];
    forkJoin(distinctProductIds.map((id) => this.productService.getProduct(id))).subscribe({
      next: (products) => {
        const productsById = new Map(products.map((product) => [product.id, product]));
        const resolvedLines: CartLine[] = [];
        for (const item of items) {
          const product = productsById.get(item.productId);
          const offer = product?.offers.find((o) => o.supplierId === item.supplierId);
          if (!product || !offer) {
            this.loading.set(false);
            this.error.set(true);
            return;
          }
          resolvedLines.push({
            productId: item.productId,
            supplierId: item.supplierId,
            productName: product.name,
            supplierName: offer.supplierName,
            price: offer.price,
            quantity: item.quantity
          });
        }
        this.lines.set(resolvedLines);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      }
    });
  }

  onQuantityChange(productId: string, supplierId: string, event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = Number(input.value);
    const clamped = Number.isInteger(raw) && raw >= 1 ? raw : 1;
    input.value = String(clamped);
    this.cartService.updateQuantity(productId, supplierId, clamped);
    this.lines.set(
      this.lines().map((line) =>
        line.productId === productId && line.supplierId === supplierId ? { ...line, quantity: clamped } : line
      )
    );
  }

  onRemove(productId: string, supplierId: string): void {
    this.cartService.removeItem(productId, supplierId);
    this.lines.set(
      this.lines().filter((line) => !(line.productId === productId && line.supplierId === supplierId))
    );
  }
}
