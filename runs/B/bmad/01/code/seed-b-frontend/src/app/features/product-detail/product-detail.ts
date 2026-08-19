import { Component, computed, effect, inject, input, signal, untracked } from '@angular/core';
import { DecimalPipe } from '@angular/common';
import type { Subscription } from 'rxjs';

import { ProductDetail as ProductDetailModel, ProductsApi } from '../../core/api/products.api';
import { CartService } from '../cart/cart.service';

@Component({
  selector: 'app-product-detail',
  imports: [DecimalPipe],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail {
  private readonly productsApi = inject(ProductsApi);
  private readonly cartService = inject(CartService);
  private subscription: Subscription | undefined;

  readonly id = input.required<string>();

  protected readonly product = signal<ProductDetailModel | null>(null);
  protected readonly loading = signal(false);
  protected readonly notFound = signal(false);
  protected readonly error = signal(false);

  protected readonly ratingAuthorName = signal('');
  protected readonly ratingValue = signal(5);
  protected readonly ratingSubmitting = signal(false);
  protected readonly ratingError = signal<string | null>(null);

  protected readonly selectedSupplierId = signal<string | null>(null);
  protected readonly quantity = signal(1);
  protected readonly addedToCartMessage = signal<string | null>(null);

  protected readonly canAddToCart = computed(() => {
    const supplierId = this.selectedSupplierId();
    const quantity = this.quantity();
    return supplierId !== null && Number.isInteger(quantity) && quantity >= 1;
  });

  constructor() {
    effect((onCleanup) => {
      const id = this.id();
      this.loadProduct(id);
      onCleanup(() => this.subscription?.unsubscribe());
    });
  }

  private loadProduct(id: string, options: { resetFirst: boolean } = { resetFirst: true }): void {
    if (options.resetFirst) {
      this.product.set(null);
      this.notFound.set(false);
      this.error.set(false);
      this.selectedSupplierId.set(null);
      this.quantity.set(1);
      this.addedToCartMessage.set(null);
      this.ratingAuthorName.set('');
      this.ratingValue.set(5);
      this.ratingSubmitting.set(false);
      this.ratingError.set(null);
    }
    this.loading.set(true);
    this.subscription?.unsubscribe();
    this.subscription = this.productsApi.getProduct(id).subscribe({
      next: (product) => {
        if (this.id() !== id) {
          return;
        }
        this.product.set(product);
        const stillOffered = untracked(() => product.offers.some((o) => o.supplierId === this.selectedSupplierId()));
        if (!stillOffered) {
          this.selectedSupplierId.set(product.offers[0]?.supplierId ?? null);
          this.addedToCartMessage.set(null);
        }
        this.loading.set(false);
      },
      error: (err) => {
        if (this.id() !== id) {
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

  protected submitRating(): void {
    const id = this.id();
    this.ratingSubmitting.set(true);
    this.ratingError.set(null);
    this.productsApi.submitRating(id, this.ratingAuthorName(), this.ratingValue()).subscribe({
      next: () => {
        if (this.id() !== id) {
          return;
        }
        this.ratingSubmitting.set(false);
        this.ratingAuthorName.set('');
        this.loadProduct(id, { resetFirst: false });
      },
      error: () => {
        if (this.id() !== id) {
          return;
        }
        this.ratingSubmitting.set(false);
        this.ratingError.set('Bewertung konnte nicht gespeichert werden. Bitte überprüfen Sie Ihre Eingabe.');
      },
    });
  }

  protected addToCart(): void {
    const product = this.product();
    const supplierId = this.selectedSupplierId();
    const quantity = this.quantity();
    if (product === null || supplierId === null || !Number.isInteger(quantity) || quantity < 1) {
      return;
    }
    const offer = product.offers.find((o) => o.supplierId === supplierId);
    if (offer === undefined) {
      return;
    }
    this.cartService.addItem({
      productId: product.id,
      productName: product.name,
      supplierId: offer.supplierId,
      supplierName: offer.supplierName,
      unitPrice: offer.price,
      quantity,
    });
    this.addedToCartMessage.set(`${quantity} × ${product.name} (${offer.supplierName}) wurde in den Warenkorb gelegt.`);
  }
}
