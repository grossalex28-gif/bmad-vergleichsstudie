import { DecimalPipe, KeyValuePipe } from '@angular/common';
import { Component, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { FormsModule } from '@angular/forms';

import { ProductDetail as ProductDetailModel } from '../../core/models/product.model';
import { CartService } from '../../core/services/cart.service';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-product-detail',
  imports: [DecimalPipe, KeyValuePipe, RouterLink, FormsModule],
  templateUrl: './product-detail.html',
  styleUrl: './product-detail.scss'
})
export class ProductDetail {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);

  protected readonly product = signal<ProductDetailModel | null>(null);
  protected readonly loading = signal(true);
  protected readonly notFound = signal(false);

  protected readonly selectedSupplierId = signal('');
  protected readonly quantity = signal(1);
  protected readonly addedMessage = signal('');

  protected readonly ratingAuthor = signal('');
  protected readonly ratingValue = signal(5);
  protected readonly ratingMessage = signal('');

  constructor() {
    this.route.paramMap.pipe(takeUntilDestroyed()).subscribe((params) => {
      const id = params.get('id');
      if (id) {
        this.loadProduct(id);
      }
    });
  }

  protected addToCart(): void {
    const product = this.product();
    const supplier = product?.angebote.find((o) => o.lieferantId === this.selectedSupplierId());
    if (!product || !supplier || this.quantity() < 1) {
      return;
    }

    this.cartService.addItem({
      produktId: product.id,
      produktName: product.name,
      lieferantId: supplier.lieferantId,
      lieferantName: supplier.lieferantName,
      preis: supplier.preis,
      menge: this.quantity()
    });
    this.addedMessage.set(`${this.quantity()} × ${product.name} wurde in den Warenkorb gelegt.`);
  }

  protected submitRating(): void {
    const product = this.product();
    const author = this.ratingAuthor().trim();
    if (!product || !author) {
      return;
    }

    this.productService.addRating(product.id, { autorName: author, wert: this.ratingValue() }).subscribe({
      next: (result) => {
        this.product.update((p) =>
          p ? { ...p, durchschnittsBewertung: result.durchschnittsBewertung, anzahlBewertungen: result.anzahlBewertungen } : p
        );
        this.ratingMessage.set('Danke für deine Bewertung!');
        this.ratingAuthor.set('');
      },
      error: () => this.ratingMessage.set('Die Bewertung konnte nicht gespeichert werden.')
    });
  }

  private loadProduct(id: string): void {
    this.loading.set(true);
    this.notFound.set(false);
    this.addedMessage.set('');
    this.ratingMessage.set('');

    this.productService.getProduct(id).subscribe({
      next: (product) => {
        this.product.set(product);
        this.selectedSupplierId.set(product.angebote[0]?.lieferantId ?? '');
        this.quantity.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.notFound.set(true);
        this.loading.set(false);
      }
    });
  }
}
