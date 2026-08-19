import { CurrencyPipe, DecimalPipe } from '@angular/common';
import { Component, OnInit, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../cart/cart.service';

@Component({
  selector: 'app-product-detail',
  imports: [CurrencyPipe, DecimalPipe],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss'
})
export class ProductDetail implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);

  readonly product = signal<Product | null>(null);
  readonly notFound = signal(false);
  readonly error = signal(false);

  readonly ratingValue = signal<number | null>(null);
  readonly ratingAuthorName = signal('');
  readonly ratingSubmitting = signal(false);
  readonly ratingError = signal(false);

  readonly selectedSupplierId = signal<string | null>(null);
  readonly quantity = signal(1);
  readonly addedToCart = signal(false);

  ngOnInit(): void {
    this.route.paramMap.subscribe((params) => this.load(params.get('id')!));
  }

  private load(id: string): void {
    this.product.set(null);
    this.notFound.set(false);
    this.error.set(false);
    this.selectedSupplierId.set(null);
    this.quantity.set(1);
    this.addedToCart.set(false);
    this.productService.getProduct(id).subscribe({
      next: (product) => this.product.set(product),
      error: (err) => {
        if (err?.status === 404) {
          this.notFound.set(true);
        } else {
          this.error.set(true);
        }
      }
    });
  }

  onRatingValueChange(event: Event): void {
    const raw = (event.target as HTMLSelectElement).value;
    this.ratingValue.set(raw ? Number(raw) : null);
  }

  onRatingAuthorNameChange(event: Event): void {
    this.ratingAuthorName.set((event.target as HTMLInputElement).value);
  }

  onRatingSubmit(): void {
    const value = this.ratingValue();
    const authorName = this.ratingAuthorName().trim();
    const currentProduct = this.product();
    if (value === null || !authorName || !currentProduct) {
      return;
    }
    this.ratingSubmitting.set(true);
    this.ratingError.set(false);
    this.productService.submitRating(currentProduct.id, authorName, value).subscribe({
      next: (summary) => {
        this.ratingSubmitting.set(false);
        this.ratingValue.set(null);
        this.ratingAuthorName.set('');
        this.product.set({ ...currentProduct, averageRating: summary.averageRating, ratingCount: summary.ratingCount });
      },
      error: () => {
        this.ratingSubmitting.set(false);
        this.ratingError.set(true);
      }
    });
  }

  onSupplierChange(supplierId: string): void {
    this.selectedSupplierId.set(supplierId);
    this.addedToCart.set(false);
  }

  onQuantityChange(event: Event): void {
    const input = event.target as HTMLInputElement;
    const raw = Number(input.value);
    const clamped = Number.isInteger(raw) && raw >= 1 ? raw : 1;
    this.quantity.set(clamped);
    input.value = String(clamped);
    this.addedToCart.set(false);
  }

  onAddToCart(): void {
    const supplierId = this.selectedSupplierId();
    const currentProduct = this.product();
    if (!supplierId || !currentProduct) {
      return;
    }
    this.cartService.addToCart(currentProduct.id, supplierId, this.quantity());
    this.addedToCart.set(true);
  }
}
