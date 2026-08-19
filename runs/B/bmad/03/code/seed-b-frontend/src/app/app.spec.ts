import { provideRouter } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { of } from 'rxjs';

import { App } from './app';
import { routes } from './app.routes';
import { ProductList } from './features/catalog/product-list/product-list';
import { ProductDetail } from './features/product-detail/product-detail';
import { Cart } from './features/cart/cart';
import { CartService } from './features/cart/cart.service';
import { CategoryService } from './core/services/category.service';
import { ProductService } from './core/services/product.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [
        provideRouter(routes),
        {
          provide: ProductService,
          useValue: {
            getProducts: () => of({ items: [], page: 1, pageSize: 20, totalCount: 0, totalPages: 0 }),
            getProduct: () =>
              of({
                id: 'P1',
                name: 'Produkt 1',
                description: 'Beschreibung',
                categoryId: 'C1',
                categoryName: 'Kategorie 1',
                subcategoryId: 'S1',
                subcategoryName: 'Unterkategorie 1',
                attributes: [],
                averageRating: null,
                ratingCount: 0,
                offers: [{ supplierId: 'L1', supplierName: 'Lieferant 1', price: 10 }]
              })
          }
        },
        { provide: CategoryService, useValue: { getCategories: () => of([]) } },
        {
          provide: CartService,
          useValue: { items: signal([{ productId: 'P1', supplierId: 'L1', quantity: 2 }]) }
        }
      ]
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('resolves the empty path route to the ProductList component', async () => {
    const harness = await RouterTestingHarness.create();
    const activatedComponent = await harness.navigateByUrl('/', ProductList);

    expect(activatedComponent).toBeInstanceOf(ProductList);
  });

  it('resolves the products/:id route to the ProductDetail component', async () => {
    const harness = await RouterTestingHarness.create();
    const activatedComponent = await harness.navigateByUrl('/products/P1', ProductDetail);

    expect(activatedComponent).toBeInstanceOf(ProductDetail);
  });

  it('resolves the cart route to the Cart component', async () => {
    const harness = await RouterTestingHarness.create();
    const activatedComponent = await harness.navigateByUrl('/cart', Cart);

    expect(activatedComponent).toBeInstanceOf(Cart);
  });

  it('shows the total item quantity from the cart in the navigation link', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Warenkorb (2)');
  });
});
