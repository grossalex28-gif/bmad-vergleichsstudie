import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { ProductDetail as ProductDetailModel, ProductsApi } from '../../core/api/products.api';
import { CartService } from '../cart/cart.service';
import { ProductDetail } from './product-detail';

describe('ProductDetail', () => {
  let productsApiMock: { getProduct: ReturnType<typeof vi.fn>; submitRating: ReturnType<typeof vi.fn> };
  let cartServiceMock: { addItem: ReturnType<typeof vi.fn> };

  const product: ProductDetailModel = {
    id: 'P1',
    name: 'Ohrhörer Modell Compact',
    description: 'Kompakte In-Ear-Kopfhörer',
    categoryId: 'K1',
    categoryName: 'Elektronik',
    subcategoryId: 'K1a',
    subcategoryName: 'Kopfhörer',
    properties: [{ name: 'Farbe', value: 'Schwarz' }],
    offers: [
      { supplierId: 'S1', supplierName: 'Lieferant 1', price: 27.5 },
      { supplierId: 'S2', supplierName: 'Lieferant 2', price: 29.9 },
    ],
    averageRating: 4.5,
    ratingCount: 2,
  };

  beforeEach(async () => {
    productsApiMock = { getProduct: vi.fn(), submitRating: vi.fn() };
    cartServiceMock = { addItem: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductsApi, useValue: productsApiMock },
        { provide: CartService, useValue: cartServiceMock },
      ],
    }).compileComponents();
  });

  it('renders name, description, category, properties, rating and offers on success', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    expect(productsApiMock.getProduct).toHaveBeenCalledWith('P1');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__name')?.textContent).toBe('Ohrhörer Modell Compact');
    expect(compiled.querySelector('.product-detail__description')?.textContent).toBe('Kompakte In-Ear-Kopfhörer');
    expect(compiled.querySelector('.product-detail__category')?.textContent).toContain('Elektronik');
    expect(compiled.querySelector('.product-detail__category')?.textContent).toContain('Kopfhörer');
    expect(compiled.querySelector('dt')?.textContent).toBe('Farbe');
    expect(compiled.querySelector('dd')?.textContent).toBe('Schwarz');
    expect(compiled.querySelector('.product-detail__rating')?.textContent).toContain('4.5');
    expect(compiled.querySelector('.product-detail__rating')?.textContent).toContain('2');

    const offers = compiled.querySelectorAll('.product-detail__offer');
    expect(offers.length).toBe(2);
    expect(offers[0].textContent).toContain('Lieferant 1');
    expect(offers[1].textContent).toContain('Lieferant 2');
  });

  it('shows "Noch keine Bewertungen" when averageRating is null', () => {
    productsApiMock.getProduct.mockReturnValue(of({ ...product, averageRating: null, ratingCount: 0 }));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__rating')?.textContent).toContain('Noch keine Bewertungen');
  });

  it('shows a distinct not-found state when the API responds with status 404', () => {
    productsApiMock.getProduct.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'unbekannt');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__not-found')).not.toBeNull();
    expect(compiled.querySelector('.product-detail__error')).toBeNull();
  });

  it('shows the generic error text for a non-404 error status', () => {
    productsApiMock.getProduct.mockReturnValue(throwError(() => ({ status: 500 })));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__error')).not.toBeNull();
    expect(compiled.querySelector('.product-detail__not-found')).toBeNull();
  });

  it('reloads and ignores a stale response for the previous id when id changes', () => {
    const p1Response = new Subject<ProductDetailModel>();
    const p2Response = new Subject<ProductDetailModel>();
    productsApiMock.getProduct.mockImplementation((id: string) => (id === 'P1' ? p1Response : p2Response));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    fixture.componentRef.setInput('id', 'P2');
    fixture.detectChanges();

    p2Response.next({ ...product, id: 'P2', name: 'Produkt 2' });
    p1Response.next(product);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__name')?.textContent).toBe('Produkt 2');
  });

  it('clears a previously rendered product when navigating to an id that is not found', () => {
    const p1Response = new Subject<ProductDetailModel>();
    const p2Response = new Subject<ProductDetailModel>();
    productsApiMock.getProduct.mockImplementation((id: string) => (id === 'P1' ? p1Response : p2Response));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();
    p1Response.next(product);
    fixture.detectChanges();

    fixture.componentRef.setInput('id', 'unbekannt');
    fixture.detectChanges();
    p2Response.error({ status: 404 });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__name')).toBeNull();
    expect(compiled.querySelector('.product-detail__not-found')).not.toBeNull();
  });

  it('shows a hint that no supplier currently offers the product', () => {
    productsApiMock.getProduct.mockReturnValue(of({ ...product, offers: [] }));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__no-offers')).not.toBeNull();
    expect(compiled.querySelectorAll('.product-detail__offer').length).toBe(0);
  });

  it('submits the entered author name and rating value', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));
    productsApiMock.submitRating.mockReturnValue(of(undefined));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const authorInput = compiled.querySelector('.product-detail__rating-author-input') as HTMLInputElement;
    authorInput.value = 'Mira';
    authorInput.dispatchEvent(new Event('input'));

    const valueSelect = compiled.querySelector('.product-detail__rating-value-select') as HTMLSelectElement;
    valueSelect.value = '3';
    valueSelect.dispatchEvent(new Event('change'));

    fixture.detectChanges();

    const form = compiled.querySelector('.product-detail__rating-form form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));

    expect(productsApiMock.submitRating).toHaveBeenCalledWith('P1', 'Mira', 3);
  });

  it('refreshes the product and clears the name field after a successful rating submission', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));
    productsApiMock.submitRating.mockReturnValue(of(undefined));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const authorInput = compiled.querySelector('.product-detail__rating-author-input') as HTMLInputElement;
    authorInput.value = 'Mira';
    authorInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = compiled.querySelector('.product-detail__rating-form form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(productsApiMock.getProduct).toHaveBeenCalledTimes(2);
    expect((compiled.querySelector('.product-detail__rating-author-input') as HTMLInputElement).value).toBe('');
  });

  it('shows an error message when the rating submission fails and does not reload the product', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));
    productsApiMock.submitRating.mockReturnValue(throwError(() => ({ status: 400 })));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const form = compiled.querySelector('.product-detail__rating-form form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(compiled.querySelector('.product-detail__rating-error')).not.toBeNull();
    expect(productsApiMock.getProduct).toHaveBeenCalledTimes(1);
  });

  it('wählt nach dem Laden automatisch den ersten Lieferanten vor und aktiviert den Absende-Button', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const radios = compiled.querySelectorAll('.product-detail__offer-radio') as NodeListOf<HTMLInputElement>;
    expect(radios[0].checked).toBe(true);
    expect(radios[1].checked).toBe(false);

    const submitButton = compiled.querySelector('.product-detail__add-to-cart-submit') as HTMLButtonElement;
    expect(submitButton.disabled).toBe(false);
  });

  it('legt beim Absenden mit gewähltem Lieferant und Menge eine Warenkorbposition an', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const radios = compiled.querySelectorAll('.product-detail__offer-radio') as NodeListOf<HTMLInputElement>;
    radios[1].checked = true;
    radios[1].dispatchEvent(new Event('change'));

    const quantityInput = compiled.querySelector('.product-detail__quantity-input') as HTMLInputElement;
    quantityInput.value = '4';
    quantityInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = compiled.querySelector('.product-detail__add-to-cart-form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));

    expect(cartServiceMock.addItem).toHaveBeenCalledWith({
      productId: 'P1',
      productName: 'Ohrhörer Modell Compact',
      supplierId: 'S2',
      supplierName: 'Lieferant 2',
      unitPrice: 29.9,
      quantity: 4,
    });
  });

  it('zeigt nach erfolgreichem Hinzufügen eine Bestätigung mit dem Produktnamen', () => {
    productsApiMock.getProduct.mockReturnValue(of(product));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const form = compiled.querySelector('.product-detail__add-to-cart-form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    const confirmation = compiled.querySelector('.product-detail__add-to-cart-confirmation');
    expect(confirmation?.textContent).toContain('Ohrhörer Modell Compact');
  });

  it('rendert kein Formular und ruft addItem nicht auf, wenn keine Lieferanten angeboten werden', () => {
    productsApiMock.getProduct.mockReturnValue(of({ ...product, offers: [] }));

    const fixture = TestBed.createComponent(ProductDetail);
    fixture.componentRef.setInput('id', 'P1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-detail__add-to-cart-form')).toBeNull();
    expect(compiled.querySelector('.product-detail__add-to-cart-submit')).toBeNull();
    expect(cartServiceMock.addItem).not.toHaveBeenCalled();
  });
});
