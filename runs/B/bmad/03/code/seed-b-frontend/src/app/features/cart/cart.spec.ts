import { TestBed } from '@angular/core/testing';
import { signal } from '@angular/core';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { CartItem } from '../../core/models/cart.model';
import { CartService } from './cart.service';
import { Cart } from './cart';

describe('Cart', () => {
  const productP1: Product = {
    id: 'P1',
    name: 'Produkt 1',
    description: 'Beschreibung 1',
    categoryId: 'C1',
    categoryName: 'Kategorie 1',
    subcategoryId: 'S1',
    subcategoryName: 'Unterkategorie 1',
    attributes: [],
    averageRating: null,
    ratingCount: 0,
    offers: [
      { supplierId: 'L1', supplierName: 'Lieferant 1', price: 10 },
      { supplierId: 'L2', supplierName: 'Lieferant 2', price: 12 }
    ]
  };

  const productP2: Product = {
    id: 'P2',
    name: 'Produkt 2',
    description: 'Beschreibung 2',
    categoryId: 'C1',
    categoryName: 'Kategorie 1',
    subcategoryId: 'S1',
    subcategoryName: 'Unterkategorie 1',
    attributes: [],
    averageRating: null,
    ratingCount: 0,
    offers: [{ supplierId: 'L3', supplierName: 'Lieferant 3', price: 5 }]
  };

  let getProduct: ReturnType<typeof vi.fn>;
  let updateQuantity: ReturnType<typeof vi.fn>;
  let removeItem: ReturnType<typeof vi.fn>;

  function configure(items: CartItem[]): void {
    TestBed.configureTestingModule({
      imports: [Cart],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProduct } },
        { provide: CartService, useValue: { items: signal(items), updateQuantity, removeItem } }
      ]
    });
  }

  beforeEach(() => {
    getProduct = vi.fn((id: string) => of(id === 'P1' ? productP1 : productP2));
    updateQuantity = vi.fn();
    removeItem = vi.fn();
  });

  it('requests each distinct product exactly once, not once per cart line', async () => {
    configure([
      { productId: 'P1', supplierId: 'L1', quantity: 2 },
      { productId: 'P1', supplierId: 'L2', quantity: 1 }
    ]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    expect(getProduct).toHaveBeenCalledTimes(1);
    expect(getProduct).toHaveBeenCalledWith('P1');
  });

  it('renders supplier name, current price from getProduct, quantity and line/overall totals', async () => {
    configure([
      { productId: 'P1', supplierId: 'L1', quantity: 2 },
      { productId: 'P2', supplierId: 'L3', quantity: 3 }
    ]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Lieferant 1');
    expect(compiled.textContent).toContain('Lieferant 3');
    expect(compiled.textContent).toMatch(/10[.,]00/);
    expect(compiled.textContent).toMatch(/20[.,]00/);
    expect(compiled.textContent).toMatch(/35[.,]00/);
  });

  it('calls cartService.updateQuantity with the changed quantity and updates line/overall totals without refetching', async () => {
    configure([{ productId: 'P1', supplierId: 'L1', quantity: 2 }]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    getProduct.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const quantityInput = compiled.querySelector('input[type="number"]') as HTMLInputElement;
    quantityInput.value = '5';
    quantityInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(updateQuantity).toHaveBeenCalledWith('P1', 'L1', 5);
    expect(compiled.textContent).toMatch(/50[.,]00/);
    expect(getProduct).not.toHaveBeenCalled();
  });

  it('calls cartService.removeItem, removes the line from view and reduces the total', async () => {
    configure([
      { productId: 'P1', supplierId: 'L1', quantity: 2 },
      { productId: 'P2', supplierId: 'L3', quantity: 3 }
    ]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const removeButton = compiled.querySelector('button') as HTMLButtonElement;
    removeButton.click();
    fixture.detectChanges();

    expect(removeItem).toHaveBeenCalledWith('P1', 'L1');
    expect(compiled.textContent).not.toContain('Lieferant 1');
    expect(compiled.textContent).toMatch(/15[.,]00/);
  });

  it('shows "Der Warenkorb ist leer." for an empty cart without calling getProduct', async () => {
    configure([]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Der Warenkorb ist leer.');
    expect(getProduct).not.toHaveBeenCalled();
  });

  it('shows an error message when getProduct fails', async () => {
    getProduct = vi.fn(() => throwError(() => new Error('network error')));
    configure([{ productId: 'P1', supplierId: 'L1', quantity: 1 }]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Der Warenkorb konnte nicht geladen werden.');
  });

  it('shows an error message instead of throwing when a cart item references a supplier no longer among the product offers', async () => {
    configure([{ productId: 'P1', supplierId: 'L9', quantity: 1 }]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);

    expect(() => fixture.detectChanges()).not.toThrow();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Der Warenkorb konnte nicht geladen werden.');
  });

  it('renders a link to /checkout when the cart has at least one item', async () => {
    configure([{ productId: 'P1', supplierId: 'L1', quantity: 2 }]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const checkoutLink = compiled.querySelector('a[href="/checkout"]');
    expect(checkoutLink).not.toBeNull();
  });
});
