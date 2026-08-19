import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';

import { Product } from '../../core/models/product.model';
import { ProductService } from '../../core/services/product.service';
import { CartService } from '../cart/cart.service';
import { ProductDetail } from './product-detail';

describe('ProductDetail', () => {
  const productP1: Product = {
    id: 'P1',
    name: 'Produkt 1',
    description: 'Eine Beschreibung',
    categoryId: 'C1',
    categoryName: 'Kategorie 1',
    subcategoryId: 'S1',
    subcategoryName: 'Unterkategorie 1',
    attributes: [{ name: 'Bauform', value: 'In-Ear' }],
    averageRating: 4.3,
    ratingCount: 3,
    offers: [{ supplierId: 'L1', supplierName: 'Lieferant 1', price: 9.99 }]
  };

  let getProduct: ReturnType<typeof vi.fn>;
  let submitRating: ReturnType<typeof vi.fn>;
  let addToCart: ReturnType<typeof vi.fn>;
  let paramMap: Subject<ParamMap>;

  beforeEach(async () => {
    getProduct = vi.fn(() => of(productP1));
    submitRating = vi.fn(() => of({ averageRating: 5, ratingCount: 1 }));
    addToCart = vi.fn();
    paramMap = new Subject<ParamMap>();

    await TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    }).compileComponents();
  });

  it('loads and renders name, description, category, attributes and suppliers for the routed product id', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();

    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    expect(getProduct).toHaveBeenCalledWith('P1');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Produkt 1');
    expect(compiled.textContent).toContain('Eine Beschreibung');
    expect(compiled.textContent).toContain('Kategorie 1');
    expect(compiled.textContent).toContain('Unterkategorie 1');
    expect(compiled.textContent).toContain('Bauform');
    expect(compiled.textContent).toContain('In-Ear');
    expect(compiled.textContent).toContain('Lieferant 1');
  });

  it('shows "Keine Bewertungen" instead of "null" or "0" when the product has no ratings', () => {
    getProduct = vi.fn(() => of({ ...productP1, averageRating: null, ratingCount: 0 }));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        expect(compiled.textContent).toContain('Keine Bewertungen');
        expect(compiled.textContent).not.toContain('null');
        expect(compiled.querySelector('.product-detail__rating')?.textContent?.trim()).not.toBe('0');
      });
  });

  it('renders the average rating with exactly one decimal place, not just "4"', () => {
    getProduct = vi.fn(() => of({ ...productP1, averageRating: 4, ratingCount: 2 }));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const ratingText = fixture.nativeElement.querySelector('.product-detail__rating')?.textContent ?? '';
        expect(ratingText).toMatch(/4[.,]0/);
      });
  });

  it('reacts to a second paramMap emission with a different id without recreating the component', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();

    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P2' }));
    fixture.detectChanges();

    expect(getProduct).toHaveBeenNthCalledWith(1, 'P1');
    expect(getProduct).toHaveBeenNthCalledWith(2, 'P2');
  });

  it('shows a "nicht gefunden" message, not the generic error message, on a 404 response', () => {
    getProduct = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 404 })));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P9' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        expect(compiled.textContent).toContain('nicht gefunden');
        expect(compiled.textContent).not.toContain('Das Produkt konnte nicht geladen werden');
      });
  });

  it('submits the rating form with the selected value and entered author name', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector('select') as HTMLSelectElement;
    select.value = '5';
    select.dispatchEvent(new Event('change'));
    const nameInput = compiled.querySelector('.product-detail__rating-form input[type="text"]') as HTMLInputElement;
    nameInput.value = 'Jonas';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const form = compiled.querySelector('form.product-detail__rating-form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(submitRating).toHaveBeenCalledWith('P1', 'Jonas', 5);
  });

  it('patches averageRating and ratingCount from the submitRating response without refetching the product', () => {
    submitRating = vi.fn(() => of({ averageRating: 4.5, ratingCount: 3 }));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const select = compiled.querySelector('select') as HTMLSelectElement;
        select.value = '5';
        select.dispatchEvent(new Event('change'));
        const nameInput = compiled.querySelector('.product-detail__rating-form input[type="text"]') as HTMLInputElement;
        nameInput.value = 'Jonas';
        nameInput.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        getProduct.mockClear();
        const form = compiled.querySelector('form.product-detail__rating-form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { cancelable: true }));
        fixture.detectChanges();

        expect(compiled.textContent).toMatch(/4[.,]5/);
        expect(compiled.textContent).toContain('3 Bewertungen');
        expect(getProduct).not.toHaveBeenCalled();
      });
  });

  it('replaces "Keine Bewertungen" with the new average after the first rating on a previously unrated product', () => {
    getProduct = vi.fn(() => of({ ...productP1, averageRating: null, ratingCount: 0 }));
    submitRating = vi.fn(() => of({ averageRating: 5, ratingCount: 1 }));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        expect(compiled.textContent).toContain('Keine Bewertungen');

        const select = compiled.querySelector('select') as HTMLSelectElement;
        select.value = '5';
        select.dispatchEvent(new Event('change'));
        const nameInput = compiled.querySelector('.product-detail__rating-form input[type="text"]') as HTMLInputElement;
        nameInput.value = 'Jonas';
        nameInput.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        const form = compiled.querySelector('form.product-detail__rating-form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { cancelable: true }));
        fixture.detectChanges();

        expect(compiled.textContent).not.toContain('Keine Bewertungen');
        expect(compiled.textContent).toMatch(/5[.,]0/);
        expect(compiled.textContent).toContain('1 Bewertungen');
      });
  });

  it('disables the submit button until a rating value is selected and a non-blank author name is entered', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('.product-detail__rating-form button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    const nameInput = compiled.querySelector('.product-detail__rating-form input[type="text"]') as HTMLInputElement;
    nameInput.value = '   ';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(button.disabled).toBe(true);

    const select = compiled.querySelector('select') as HTMLSelectElement;
    select.value = '5';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(button.disabled).toBe(true);

    nameInput.value = 'Jonas';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
  });

  it('shows an error message and keeps the product unchanged when submitRating fails', () => {
    submitRating = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 400 })));

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const select = compiled.querySelector('select') as HTMLSelectElement;
        select.value = '5';
        select.dispatchEvent(new Event('change'));
        const nameInput = compiled.querySelector('.product-detail__rating-form input[type="text"]') as HTMLInputElement;
        nameInput.value = 'Jonas';
        nameInput.dispatchEvent(new Event('input'));
        fixture.detectChanges();

        const form = compiled.querySelector('form.product-detail__rating-form') as HTMLFormElement;
        form.dispatchEvent(new Event('submit', { cancelable: true }));
        fixture.detectChanges();

        expect(compiled.textContent).toContain('Die Bewertung konnte nicht gespeichert werden');
        expect(compiled.textContent).toMatch(/4[.,]3/);
        expect(compiled.textContent).toContain('3 Bewertungen');
      });
  });

  it('disables the "In den Warenkorb" button until a supplier is selected', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('.product-detail__add-to-cart button') as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    const radio = compiled.querySelector('input[type="radio"]') as HTMLInputElement;
    radio.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(button.disabled).toBe(false);
  });

  it('adds the product to the cart with the selected supplier and default quantity 1', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const radio = compiled.querySelector('input[type="radio"]') as HTMLInputElement;
    radio.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const button = compiled.querySelector('.product-detail__add-to-cart button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(addToCart).toHaveBeenCalledWith('P1', 'L1', 1);
  });

  it('adds the product to the cart with the supplier selected from among several offers', () => {
    getProduct = vi.fn(() =>
      of({
        ...productP1,
        offers: [
          { supplierId: 'L1', supplierName: 'Lieferant 1', price: 9.99 },
          { supplierId: 'L2', supplierName: 'Lieferant 2', price: 12.5 }
        ]
      })
    );

    return TestBed.configureTestingModule({
      imports: [ProductDetail],
      providers: [
        { provide: ProductService, useValue: { getProduct, submitRating } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } },
        { provide: CartService, useValue: { addToCart, items: signal([]) } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductDetail);
        fixture.detectChanges();
        paramMap.next(convertToParamMap({ id: 'P1' }));
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const radios = compiled.querySelectorAll('input[type="radio"]');
        expect(radios.length).toBe(2);
        (radios[1] as HTMLInputElement).dispatchEvent(new Event('change'));
        fixture.detectChanges();

        const button = compiled.querySelector('.product-detail__add-to-cart button') as HTMLButtonElement;
        button.click();
        fixture.detectChanges();

        expect(addToCart).toHaveBeenCalledWith('P1', 'L2', 1);
      });
  });

  it('adds the product to the cart with the entered quantity', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const radio = compiled.querySelector('input[type="radio"]') as HTMLInputElement;
    radio.dispatchEvent(new Event('change'));
    const quantityInput = compiled.querySelector('.product-detail__add-to-cart input[type="number"]') as HTMLInputElement;
    quantityInput.value = '3';
    quantityInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    const button = compiled.querySelector('.product-detail__add-to-cart button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();

    expect(addToCart).toHaveBeenCalledWith('P1', 'L1', 3);
  });

  it('corrects the quantity input back to 1 when an invalid value is entered', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const quantityInput = compiled.querySelector('.product-detail__add-to-cart input[type="number"]') as HTMLInputElement;
    quantityInput.value = '0';
    quantityInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(quantityInput.value).toBe('1');
  });

  it('shows the "In den Warenkorb gelegt." confirmation after adding, and hides it again on a new selection', () => {
    const fixture = TestBed.createComponent(ProductDetail);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'P1' }));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const radio = compiled.querySelector('input[type="radio"]') as HTMLInputElement;
    radio.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const button = compiled.querySelector('.product-detail__add-to-cart button') as HTMLButtonElement;
    button.click();
    fixture.detectChanges();
    expect(compiled.textContent).toContain('In den Warenkorb gelegt.');

    const quantityInput = compiled.querySelector('.product-detail__add-to-cart input[type="number"]') as HTMLInputElement;
    quantityInput.value = '2';
    quantityInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    expect(compiled.textContent).not.toContain('In den Warenkorb gelegt.');
  });
});
