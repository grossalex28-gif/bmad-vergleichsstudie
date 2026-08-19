import { TestBed } from '@angular/core/testing';
import { provideRouter, Router } from '@angular/router';
import { of, throwError } from 'rxjs';
import { vi } from 'vitest';

import { CartService } from '../cart/cart.service';
import { Order, OrdersApi } from '../../core/api/orders.api';
import { Checkout } from './checkout';

describe('Checkout', () => {
  let ordersApiMock: { createOrder: ReturnType<typeof vi.fn> };
  let cartService: CartService;
  let router: Router;

  const order: Order = {
    publicId: 'ORD-1',
    status: 'Received',
    delivery: {
      name: 'Mira Muster',
      street: 'Musterstraße 1',
      postalCode: '12345',
      city: 'Musterstadt',
      country: 'Deutschland',
      email: 'mira@example.com',
    },
    items: [],
    totalAmount: 20,
    createdAt: '2026-08-17T00:00:00Z',
  };

  beforeEach(async () => {
    ordersApiMock = { createOrder: vi.fn() };

    await TestBed.configureTestingModule({
      imports: [Checkout],
      providers: [provideRouter([]), { provide: OrdersApi, useValue: ordersApiMock }],
    }).compileComponents();

    cartService = TestBed.inject(CartService);
    router = TestBed.inject(Router);
  });

  function fillDeliveryForm(element: HTMLElement): void {
    const inputs = element.querySelectorAll('input');
    const values: Record<string, string> = {
      name: 'Mira Muster',
      street: 'Musterstraße 1',
      postalCode: '12345',
      city: 'Musterstadt',
      country: 'Deutschland',
      email: 'mira@example.com',
    };
    const order = ['name', 'street', 'postalCode', 'city', 'country', 'email'];
    order.forEach((field, index) => {
      const input = inputs[index] as HTMLInputElement;
      input.value = values[field];
      input.dispatchEvent(new Event('input'));
    });
  }

  it('zeigt bei leerem Warenkorb einen leeren Zustand statt eines Formulars', () => {
    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.checkout__empty')).toBeTruthy();
    expect(element.querySelector('form')).toBeNull();
  });

  it('sendet beim Absenden die Warenkorbpositionen und Liefer-/Kontaktdaten, leert den Warenkorb und navigiert zur Bestellbestätigung', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    ordersApiMock.createOrder.mockReturnValue(of(order));
    const navigateSpy = vi.spyOn(router, 'navigate');

    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    fillDeliveryForm(element);
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));

    expect(ordersApiMock.createOrder).toHaveBeenCalledWith({
      items: [{ productId: 'P1', supplierId: 'S1', quantity: 2 }],
      delivery: {
        name: 'Mira Muster',
        street: 'Musterstraße 1',
        postalCode: '12345',
        city: 'Musterstadt',
        country: 'Deutschland',
        email: 'mira@example.com',
        phone: '',
      },
    });
    expect(cartService.items()).toEqual([]);
    expect(navigateSpy).toHaveBeenCalledWith(['/orders', 'ORD-1']);
  });

  it('zeigt bei einer Fehlerantwort vom Backend eine Rückmeldung und behält den Warenkorb bei', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    ordersApiMock.createOrder.mockReturnValue(throwError(() => ({ status: 400 })));

    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    fillDeliveryForm(element);
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(element.querySelector('.checkout__error')).toBeTruthy();
    expect(cartService.items().length).toBe(1);
  });

  it('zeigt bei einer Ablehnungsantwort mit detail den exakten Backend-Text an', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    ordersApiMock.createOrder.mockReturnValue(
      throwError(() => ({ status: 400, error: { detail: 'Ihre Bestellung enthält ungültige Positionen.' } })),
    );

    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    fillDeliveryForm(element);
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(element.querySelector('.checkout__error')?.textContent).toContain('Ihre Bestellung enthält ungültige Positionen.');
  });

  it('zeigt bei einer Ablehnungsantwort mit invalidLines die betroffene Position an und behält den Warenkorb bei', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    ordersApiMock.createOrder.mockReturnValue(
      throwError(() => ({
        status: 400,
        error: {
          detail: 'Ihre Bestellung enthält ungültige Positionen.',
          invalidLines: [{ productId: 'P1', supplierId: 'S1', reason: 'OFFER_NOT_FOUND' }],
        },
      })),
    );

    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    fillDeliveryForm(element);
    fixture.detectChanges();

    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    const invalidLinesText = element.querySelector('.checkout__invalid-lines')?.textContent ?? '';
    expect(invalidLinesText).toContain('Produkt 1');
    expect(invalidLinesText).toContain('Dieses Angebot ist nicht mehr verfügbar.');
    expect(cartService.items().length).toBe(1);
  });
});
