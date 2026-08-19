import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CartApiService } from './cart-api.service';
import { Cart } from './cart.model';

describe('CartApiService', () => {
  let service: CartApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(CartApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('adds an item via POST without an X-Cart-Id header when no cart id is known', () => {
    const response: Cart = { cartId: 'C1', items: [], totalPrice: 0 };

    service.addItem(null, 'P1', 'L1', 2).subscribe();

    const req = httpMock.expectOne('/api/cart/items');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ productId: 'P1', supplierId: 'L1', quantity: 2 });
    expect(req.request.headers.has('X-Cart-Id')).toBe(false);
    req.flush(response);
  });

  it('adds an item via POST with the X-Cart-Id header when a cart id is known', () => {
    const response: Cart = { cartId: 'C1', items: [], totalPrice: 0 };

    service.addItem('C1', 'P1', 'L1', 1).subscribe();

    const req = httpMock.expectOne('/api/cart/items');
    expect(req.request.headers.get('X-Cart-Id')).toBe('C1');
    req.flush(response);
  });

  it('reads the cart via GET without an X-Cart-Id header when no cart id is known', () => {
    const response: Cart = { cartId: null, items: [], totalPrice: 0 };

    service.getCart(null).subscribe();

    const req = httpMock.expectOne('/api/cart');
    expect(req.request.method).toBe('GET');
    expect(req.request.headers.has('X-Cart-Id')).toBe(false);
    req.flush(response);
  });

  it('reads the cart via GET with the X-Cart-Id header when a cart id is known', () => {
    const response: Cart = { cartId: 'C1', items: [], totalPrice: 0 };

    service.getCart('C1').subscribe();

    const req = httpMock.expectOne('/api/cart');
    expect(req.request.headers.get('X-Cart-Id')).toBe('C1');
    req.flush(response);
  });

  it('updates a line quantity via PATCH with the X-Cart-Id header and the new quantity in the body', () => {
    const response: Cart = { cartId: 'C1', items: [], totalPrice: 0 };

    service.updateQuantity('C1', 'P1', 'L1', 5).subscribe();

    const req = httpMock.expectOne('/api/cart/items/P1/L1');
    expect(req.request.method).toBe('PATCH');
    expect(req.request.headers.get('X-Cart-Id')).toBe('C1');
    expect(req.request.body).toEqual({ quantity: 5 });
    req.flush(response);
  });

  it('removes a line via DELETE with the X-Cart-Id header', () => {
    const response: Cart = { cartId: 'C1', items: [], totalPrice: 0 };

    service.removeItem('C1', 'P1', 'L1').subscribe();

    const req = httpMock.expectOne('/api/cart/items/P1/L1');
    expect(req.request.method).toBe('DELETE');
    expect(req.request.headers.get('X-Cart-Id')).toBe('C1');
    req.flush(response);
  });
});
