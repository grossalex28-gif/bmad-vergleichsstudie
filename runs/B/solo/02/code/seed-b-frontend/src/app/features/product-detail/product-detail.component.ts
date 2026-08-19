import { Component, OnInit, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { DecimalPipe, KeyValuePipe } from '@angular/common';
import { ActivatedRoute, RouterLink } from '@angular/router';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../../core/services/cart.service';
import { ProductDetail } from '../../core/models/product.model';

@Component({
  selector: 'app-product-detail',
  imports: [FormsModule, DecimalPipe, KeyValuePipe, RouterLink],
  templateUrl: './product-detail.component.html',
  styleUrl: './product-detail.component.scss',
})
export class ProductDetailComponent implements OnInit {
  private readonly route = inject(ActivatedRoute);
  private readonly productService = inject(ProductService);
  private readonly cartService = inject(CartService);

  readonly product = signal<ProductDetail | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly selectedSupplierId = signal<number | null>(null);
  readonly quantity = signal(1);
  readonly addedMessage = signal<string | null>(null);

  readonly reviewAuthor = signal('');
  readonly reviewRating = signal(5);
  readonly reviewSubmitting = signal(false);
  readonly reviewMessage = signal<string | null>(null);
  readonly reviewError = signal<string | null>(null);

  readonly ratingOptions = [1, 2, 3, 4, 5];

  ngOnInit(): void {
    const id = Number(this.route.snapshot.paramMap.get('id'));
    this.load(id);
  }

  private load(id: number): void {
    this.loading.set(true);
    this.error.set(null);
    this.productService.getProduct(id).subscribe({
      next: (p) => {
        this.product.set(p);
        this.selectedSupplierId.set(p.offers[0]?.supplierId ?? null);
        this.loading.set(false);
      },
      error: () => {
        this.error.set('Produkt konnte nicht geladen werden.');
        this.loading.set(false);
      },
    });
  }

  addToCart(): void {
    const p = this.product();
    const supplierId = this.selectedSupplierId();
    if (!p || supplierId == null) return;
    const offer = p.offers.find((o) => o.supplierId === supplierId);
    if (!offer) return;

    this.cartService.addItem(
      {
        productId: p.id,
        productName: p.name,
        subcategoryId: p.subcategoryId,
        supplierId: offer.supplierId,
        supplierName: offer.supplierName,
        unitPrice: offer.price,
      },
      this.quantity(),
    );

    this.addedMessage.set('Zum Warenkorb hinzugefügt.');
    setTimeout(() => this.addedMessage.set(null), 2500);
  }

  submitReview(): void {
    const p = this.product();
    if (!p) return;

    const author = this.reviewAuthor().trim();
    if (!author) {
      this.reviewError.set('Bitte einen Namen angeben.');
      return;
    }

    this.reviewSubmitting.set(true);
    this.reviewError.set(null);
    this.reviewMessage.set(null);

    this.productService.addReview(p.id, { authorName: author, rating: this.reviewRating() }).subscribe({
      next: (result) => {
        this.product.update((current) =>
          current ? { ...current, averageRating: result.averageRating, ratingCount: result.ratingCount } : current,
        );
        this.reviewMessage.set('Danke für Ihre Bewertung!');
        this.reviewSubmitting.set(false);
      },
      error: () => {
        this.reviewError.set('Bewertung konnte nicht gespeichert werden.');
        this.reviewSubmitting.set(false);
      },
    });
  }
}
