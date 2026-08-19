import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CartService } from '../../../core/cart.service';
import { Cart } from '../cart.model';
import { CartView } from './cart-view';

describe('CartView', () => {
  let httpMock: HttpTestingController;
  let cartService: CartService;

  function configure(initialCartId: string | null): void {
    TestBed.configureTestingModule({
      imports: [CartView],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
    cartService = TestBed.inject(CartService);
    if (initialCartId) {
      cartService.setCartId(initialCartId);
    }
  }

  afterEach(() => {
    httpMock.verify();
  });

  const fullCart: Cart = {
    cartId: 'C1',
    items: [
      { productId: 'P1', productName: 'Kopfhörer', supplierId: 'L1', supplierName: 'Lieferant Eins', quantity: 2, unitPrice: 10, lineTotal: 20 }
    ],
    totalPrice: 20
  };

  it('shows an empty-cart message when no items exist (AC 1)', () => {
    configure(null);
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush({ cartId: null, items: [], totalPrice: 0 });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Ihr Warenkorb ist leer.');
  });

  it('renders product, supplier, unit price, quantity, line total and grand total (AC 1)', () => {
    configure('C1');
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Kopfhörer');
    expect(text).toContain('Lieferant Eins');
    expect(text).toContain('20,00');
  });

  it('sends a PATCH with the new quantity and updates totals on success (AC 4)', () => {
    configure('C1');
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const qtyInput = el.querySelector('input[type="number"]') as HTMLInputElement;
    qtyInput.value = '4';
    qtyInput.dispatchEvent(new Event('change'));

    const req = httpMock.expectOne('/api/cart/items/P1/L1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.body).toEqual({ quantity: 4 });
    req.flush({ cartId: 'C1', items: [{ ...fullCart.items[0], quantity: 4, lineTotal: 40 }], totalPrice: 40 });
    fixture.detectChanges();

    expect(el.textContent).toContain('40,00');
  });

  it('shows the German conflict message on a 409 PATCH response (AC 5)', () => {
    configure('C1');
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const qtyInput = el.querySelector('input[type="number"]') as HTMLInputElement;
    qtyInput.value = '9';
    qtyInput.dispatchEvent(new Event('change'));

    httpMock.expectOne('/api/cart/items/P1/L1').flush({ error: 'Angebot nicht mehr verfügbar' }, { status: 409, statusText: 'Conflict' });
    fixture.detectChanges();

    expect(el.textContent).toContain('Angebot nicht mehr verfügbar');
  });

  it('sends a DELETE and removes the line from the view on success (AC 6)', () => {
    configure('C1');
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const button = el.querySelector('button[type="button"]') as HTMLButtonElement;
    button.click();

    const req = httpMock.expectOne('/api/cart/items/P1/L1');
    expect(req.request.method).toBe('DELETE');
    req.flush({ cartId: 'C1', items: [], totalPrice: 0 });
    fixture.detectChanges();

    expect(el.textContent).toContain('Ihr Warenkorb ist leer.');
  });

  it('shows a checkout link when the cart has items (AC 7)', () => {
    configure('C1');
    const fixture = TestBed.createComponent(CartView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a[routerLink="/checkout"]');
    expect(link).not.toBeNull();
  });
});
