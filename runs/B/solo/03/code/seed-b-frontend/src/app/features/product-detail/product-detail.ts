import { DecimalPipe, KeyValuePipe } from '@angular/common';
import { Component, effect, inject, signal } from '@angular/core';
import { toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { ActivatedRoute, Router } from '@angular/router';
import { map } from 'rxjs';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ProductDetail as ProductDetailModel } from '../../core/models/product.model';

@Component({
  selector: 'app-product-detail',
  imports: [FormsModule, DecimalPipe, KeyValuePipe],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss',
})
export class ProductDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);
  private readonly cart = inject(CartService);

  private readonly productId = toSignal(this.route.paramMap.pipe(map((params) => params.get('id') ?? '')), {
    initialValue: '',
  });

  protected readonly product = signal<ProductDetailModel | undefined>(undefined);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly selectedSupplierId = signal<string | undefined>(undefined);
  protected readonly quantity = signal(1);
  protected readonly addedToCart = signal(false);

  protected readonly ratingAuthor = signal('');
  protected readonly ratingStars = signal(5);
  protected readonly ratingSubmitting = signal(false);
  protected readonly ratingError = signal<string | undefined>(undefined);

  constructor() {
    effect(() => {
      const id = this.productId();
      if (!id) {
        return;
      }
      this.loadProduct(id);
    });
  }

  private loadProduct(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);
    this.addedToCart.set(false);

    this.productService.getDetail(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.selectedSupplierId.set(product.offers[0]?.supplierId);
        this.loading.set(false);
      },
      error: () => {
        this.product.set(undefined);
        this.notFound.set(true);
        this.loading.set(false);
      },
    });
  }

  protected onSupplierChange(supplierId: string): void {
    this.selectedSupplierId.set(supplierId);
    this.addedToCart.set(false);
  }

  protected addToCart(): void {
    const product = this.product();
    const supplierId = this.selectedSupplierId() ?? product?.offers[0]?.supplierId;
    if (!product || !supplierId) {
      return;
    }
    const offer = product.offers.find((o) => o.supplierId === supplierId);
    if (!offer) {
      return;
    }

    this.cart.addItem({
      productId: product.id,
      productName: product.name,
      supplierId: offer.supplierId,
      supplierName: offer.supplierName,
      unitPrice: offer.price,
      quantity: this.quantity(),
    });
    this.addedToCart.set(true);
  }

  protected submitRating(): void {
    const product = this.product();
    const author = this.ratingAuthor().trim();
    if (!product || !author) {
      return;
    }

    this.ratingSubmitting.set(true);
    this.ratingError.set(undefined);

    this.productService.rate(product.id, { authorName: author, stars: this.ratingStars() }).subscribe({
      next: (rating) => {
        this.ratingSubmitting.set(false);
        this.ratingAuthor.set('');
        this.ratingStars.set(5);
        this.product.update((current) =>
          current ? { ...current, averageRating: rating.averageRating, ratingCount: rating.ratingCount } : current,
        );
      },
      error: () => {
        this.ratingSubmitting.set(false);
        this.ratingError.set('Bewertung konnte nicht gespeichert werden.');
      },
    });
  }

  protected goToCart(): void {
    this.router.navigate(['/warenkorb']);
  }
}
