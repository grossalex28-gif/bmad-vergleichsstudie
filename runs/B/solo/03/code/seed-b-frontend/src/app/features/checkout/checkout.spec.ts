import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Checkout } from './checkout';
import { CartService } from '../../core/services/cart.service';

@Component({ template: '' })
class DummyComponent {}

describe('Checkout', () => {
  let httpMock: HttpTestingController;
  let cart: CartService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Checkout],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([{ path: 'bestellungen/:id', component: DummyComponent }]),
      ],
    });

    cart = TestBed.inject(CartService);
    cart.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 2,
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('submits the cart contents as an order and clears the cart on success', () => {
    const fixture = TestBed.createComponent(Checkout);
    const component = fixture.componentInstance;

    component['contactName'].set('Max Mustermann');
    component['email'].set('max@example.com');
    component['street'].set('Musterweg 1');
    component['postalCode'].set('12345');
    component['city'].set('Musterstadt');

    component['submit']();

    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({
      contactName: 'Max Mustermann',
      email: 'max@example.com',
      street: 'Musterweg 1',
      postalCode: '12345',
      city: 'Musterstadt',
      items: [{ productId: 'P1', supplierId: 'L1', quantity: 2 }],
    });

    req.flush({
      id: 1,
      status: 'Eingegangen',
      createdAt: new Date().toISOString(),
      contactName: 'Max Mustermann',
      email: 'max@example.com',
      street: 'Musterweg 1',
      postalCode: '12345',
      city: 'Musterstadt',
      items: [],
      total: 59.98,
    });

    expect(cart.items()).toEqual([]);
  });

  it('shows the server-provided error message when order creation is rejected', () => {
    const fixture = TestBed.createComponent(Checkout);
    const component = fixture.componentInstance;

    component['contactName'].set('Max Mustermann');
    component['email'].set('max@example.com');
    component['street'].set('Musterweg 1');
    component['postalCode'].set('12345');
    component['city'].set('Musterstadt');

    component['submit']();

    const req = httpMock.expectOne('/api/orders');
    req.flush(
      { detail: 'Produkt P1 wird beim gewählten Lieferanten nicht mehr angeboten.' },
      { status: 422, statusText: 'Unprocessable Entity' },
    );

    expect(component['errorMessage']()).toContain('nicht mehr angeboten');
    expect(cart.items().length).toBe(1);
  });
});
