import { HttpErrorResponse } from '@angular/common/http';
import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Router, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';

import { CartItem } from '../../core/models/cart.model';
import { OrderCreated } from '../../core/models/order.model';
import { OrderService } from '../../core/services/order.service';
import { CartService } from '../cart/cart.service';
import { Checkout } from './checkout';

describe('Checkout', () => {
  let createOrder: ReturnType<typeof vi.fn>;
  let clear: ReturnType<typeof vi.fn>;

  function configure(items: CartItem[]): void {
    TestBed.configureTestingModule({
      imports: [Checkout],
      providers: [
        provideRouter([{ path: 'orders/:id', children: [] }]),
        { provide: OrderService, useValue: { createOrder } },
        { provide: CartService, useValue: { items: signal(items), clear } }
      ]
    });
  }

  function fillForm(compiled: HTMLElement): void {
    const inputs = compiled.querySelectorAll('input');
    const values = ['Jonas', 'Hauptstr. 1', '12345', 'Berlin', 'DE', 'j@example.com'];
    inputs.forEach((input, index) => {
      (input as HTMLInputElement).value = values[index];
      input.dispatchEvent(new Event('input'));
    });
  }

  function submit(compiled: HTMLElement): void {
    const form = compiled.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
  }

  beforeEach(() => {
    createOrder = vi.fn(() => of({ orderId: 'ORDER-1' } as OrderCreated));
    clear = vi.fn();
  });

  it('submits the form with trimmed field values and the current cart items', async () => {
    const items: CartItem[] = [{ productId: 'P1', supplierId: 'L1', quantity: 2 }];
    configure(items);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const inputs = compiled.querySelectorAll('input');
    const values = ['  Jonas  ', '  Hauptstr. 1  ', '  12345  ', '  Berlin  ', '  DE  ', '  j@example.com  '];
    inputs.forEach((input, index) => {
      (input as HTMLInputElement).value = values[index];
      input.dispatchEvent(new Event('input'));
    });
    fixture.detectChanges();
    submit(compiled);
    fixture.detectChanges();

    expect(createOrder).toHaveBeenCalledWith({
      name: 'Jonas',
      street: 'Hauptstr. 1',
      postalCode: '12345',
      city: 'Berlin',
      country: 'DE',
      email: 'j@example.com',
      items
    });
  });

  it('clears the cart and navigates to the order confirmation on a successful response', async () => {
    configure([{ productId: 'P1', supplierId: 'L1', quantity: 1 }]);
    await TestBed.compileComponents();
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    fillForm(compiled);
    fixture.detectChanges();
    submit(compiled);
    fixture.detectChanges();

    expect(clear).toHaveBeenCalled();
    expect(navigateSpy).toHaveBeenCalledWith(['/orders', 'ORDER-1']);
  });

  it('shows the offer-unavailable error message without clearing the cart, keeping the form visible', async () => {
    createOrder = vi.fn(() =>
      throwError(() => new HttpErrorResponse({ status: 400, error: { reason: 'offer-unavailable' } }))
    );
    configure([{ productId: 'P1', supplierId: 'L1', quantity: 1 }]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    fillForm(compiled);
    fixture.detectChanges();
    submit(compiled);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Ein Angebot im Warenkorb ist nicht mehr verfügbar.');
    expect(clear).not.toHaveBeenCalled();
    expect(compiled.querySelector('form')).not.toBeNull();
  });

  it('disables the submit button and does not call createOrder for an empty cart', async () => {
    configure([]);
    await TestBed.compileComponents();
    const fixture = TestBed.createComponent(Checkout);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const button = compiled.querySelector('button[type="submit"]') as HTMLButtonElement;
    expect(button.disabled).toBe(true);

    submit(compiled);
    fixture.detectChanges();

    expect(createOrder).not.toHaveBeenCalled();
  });
});
