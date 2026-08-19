import { HttpErrorResponse } from '@angular/common/http';
import { Component, inject, signal } from '@angular/core';
import { ActivatedRoute } from '@angular/router';

import { CartApiService } from '../../cart/cart-api.service';
import { CartService } from '../../../core/cart.service';
import { ProductApiService } from '../product-api.service';
import { ProductDetail as ProductDetailModel } from '../product.model';

@Component({
  selector: 'app-product-detail',
  templateUrl: './product-detail.html'
})
export class ProductDetail {
  private route = inject(ActivatedRoute);
  private productApi = inject(ProductApiService);
  private cartApi = inject(CartApiService);
  private cartService = inject(CartService);

  protected readonly product = signal<ProductDetailModel | null>(null);
  protected readonly notFound = signal(false);
  protected readonly loading = signal(true);

  protected readonly authorName = signal('');
  protected readonly score = signal<number | null>(null);
  protected readonly submitting = signal(false);
  protected readonly submitError = signal<string | null>(null);
  protected readonly submitSuccess = signal(false);

  protected readonly addingToCart = signal(false);
  protected readonly cartError = signal<string | null>(null);
  protected readonly cartSuccess = signal(false);

  constructor() {
    const id = this.route.snapshot.paramMap.get('id');
    if (!id) {
      this.notFound.set(true);
      this.loading.set(false);
      return;
    }
    this.productApi.getProduct(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }

  protected onAuthorNameChange(value: string): void {
    this.authorName.set(value);
    this.submitSuccess.set(false);
    this.submitError.set(null);
  }

  protected onScoreChange(value: string): void {
    this.score.set(value ? Number(value) : null);
    this.submitSuccess.set(false);
    this.submitError.set(null);
  }

  protected submitRating(): void {
    if (this.submitting()) {
      return;
    }

    const current = this.product();
    const author = this.authorName().trim();
    const scoreValue = this.score();
    if (!current || !author || scoreValue === null) {
      this.submitError.set('Bitte Sternewert und Namen angeben.');
      this.submitSuccess.set(false);
      return;
    }

    this.submitting.set(true);
    this.submitError.set(null);
    this.submitSuccess.set(false);

    this.productApi.submitRating(current.id, { authorName: author, score: scoreValue }).subscribe({
      next: (result) => {
        this.product.set({ ...current, averageRating: result.averageRating, ratingCount: result.ratingCount });
        this.submitting.set(false);
        this.submitSuccess.set(true);
        this.authorName.set('');
        this.score.set(null);
      },
      error: (err: HttpErrorResponse) => {
        this.submitting.set(false);
        this.submitError.set(
          err.status === 404
            ? 'Dieses Produkt ist nicht mehr verfügbar.'
            : 'Bewertung konnte nicht gespeichert werden. Bitte Eingaben prüfen.'
        );
      }
    });
  }

  protected addToCart(supplierId: string, quantityRaw: string): void {
    if (this.addingToCart()) {
      return;
    }

    const current = this.product();
    const quantity = Number(quantityRaw);
    if (!current || !Number.isInteger(quantity) || quantity < 1) {
      this.cartError.set('Bitte eine gültige Menge (mindestens 1) angeben.');
      this.cartSuccess.set(false);
      return;
    }

    this.addingToCart.set(true);
    this.cartError.set(null);
    this.cartSuccess.set(false);

    this.cartApi.addItem(this.cartService.cartId(), current.id, supplierId, quantity).subscribe({
      next: (cart) => {
        if (cart.cartId) {
          this.cartService.setCartId(cart.cartId);
        }
        this.addingToCart.set(false);
        this.cartSuccess.set(true);
      },
      error: (err: HttpErrorResponse) => {
        this.addingToCart.set(false);
        this.cartError.set(
          err.status === 404
            ? 'Dieses Angebot ist nicht mehr verfügbar.'
            : 'Konnte nicht zum Warenkorb hinzugefügt werden.'
        );
      }
    });
  }
}
