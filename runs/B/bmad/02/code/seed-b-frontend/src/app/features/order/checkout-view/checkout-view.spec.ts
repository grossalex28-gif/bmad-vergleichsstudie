import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { vi } from 'vitest';

import { CartService } from '../../../core/cart.service';
import { Cart } from '../../cart/cart.model';
import { CheckoutView } from './checkout-view';
import { Order } from '../order.model';

describe('CheckoutView', () => {
  let httpMock: HttpTestingController;
  let cartService: CartService;
  let router: Router;

  const fullCart: Cart = {
    cartId: 'C1',
    items: [
      { productId: 'P1', productName: 'Kopfhörer', supplierId: 'L1', supplierName: 'Lieferant Eins', quantity: 2, unitPrice: 10, lineTotal: 20 }
    ],
    totalPrice: 20
  };

  function configure(): void {
    TestBed.configureTestingModule({
      imports: [CheckoutView],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });
    httpMock = TestBed.inject(HttpTestingController);
    cartService = TestBed.inject(CartService);
    router = TestBed.inject(Router);
    cartService.setCartId('C1');
  }

  afterEach(() => httpMock.verify());

  it('shows an empty-cart message and no form when the cart has no items (AC 7)', () => {
    configure();
    const fixture = TestBed.createComponent(CheckoutView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush({ cartId: 'C1', items: [], totalPrice: 0 });
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    expect(el.textContent).toContain('Ihr Warenkorb ist leer');
    expect(el.querySelector('form')).toBeNull();
  });

  it('shows the loaded cart summary before submission (AC 7)', () => {
    configure();
    const fixture = TestBed.createComponent(CheckoutView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Kopfhörer');
  });

  it('navigates to the order view on successful placement (AC 1)', () => {
    configure();
    const fixture = TestBed.createComponent(CheckoutView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    const el = fixture.nativeElement as HTMLElement;
    const inputs = el.querySelectorAll('input');
    (inputs[0] as HTMLInputElement).value = 'Mara Muster';
    (inputs[0] as HTMLInputElement).dispatchEvent(new Event('input'));
    (inputs[1] as HTMLInputElement).value = 'Musterstraße 1';
    (inputs[1] as HTMLInputElement).dispatchEvent(new Event('input'));
    (inputs[2] as HTMLInputElement).value = 'mara@example.com';
    (inputs[2] as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    const order: Order = {
      orderId: 'O1',
      status: 'Neu',
      items: [{ productId: 'P1', productName: 'Kopfhörer', supplierId: 'L1', supplierName: 'Lieferant Eins', quantity: 2, unitPrice: 10, lineTotal: 20 }],
      totalPrice: 20
    };
    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    req.flush(order);

    expect(navigateSpy).toHaveBeenCalledWith(['/orders', 'O1']);
  });

  it('shows the structured rejection message with resolved line names on a 409 response (AC 5)', () => {
    configure();
    const fixture = TestBed.createComponent(CheckoutView);
    fixture.detectChanges();
    httpMock.expectOne('/api/cart').flush(fullCart);
    fixture.detectChanges();

    const el = fixture.nativeElement as HTMLElement;
    const inputs = el.querySelectorAll('input');
    (inputs[0] as HTMLInputElement).value = 'Mara Muster';
    (inputs[0] as HTMLInputElement).dispatchEvent(new Event('input'));
    (inputs[1] as HTMLInputElement).value = 'Musterstraße 1';
    (inputs[1] as HTMLInputElement).dispatchEvent(new Event('input'));
    (inputs[2] as HTMLInputElement).value = 'mara@example.com';
    (inputs[2] as HTMLInputElement).dispatchEvent(new Event('input'));
    (el.querySelector('form') as HTMLFormElement).dispatchEvent(new Event('submit'));

    httpMock.expectOne('/api/orders').flush(
      { error: 'Bestellung abgelehnt', lines: [{ productId: 'P1', supplierId: 'L1', reason: 'angebot_nicht_mehr_verfuegbar' }] },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    expect(el.textContent).toContain('Bestellung abgelehnt');
    expect(el.textContent).toContain('Kopfhörer');
    expect(el.textContent).toContain('Angebot nicht mehr verfügbar');
  });
});
