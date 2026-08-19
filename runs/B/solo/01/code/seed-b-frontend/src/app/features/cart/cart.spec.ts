import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CartService } from '../../core/services/cart.service';
import { Cart } from './cart';

describe('Cart', () => {
  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({
      imports: [Cart],
      providers: [provideZonelessChangeDetection(), provideRouter([])]
    });
  });

  it('shows an empty-cart message when there are no items', async () => {
    const fixture = TestBed.createComponent(Cart);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty')).toBeTruthy();
    expect(compiled.querySelector('table')).toBeNull();
  });

  it('renders a row per cart position with quantity and totals', async () => {
    const cartService = TestBed.inject(CartService);
    cartService.addItem({
      produktId: 'P1',
      produktName: 'Ohrhörer Compact',
      lieferantId: 'L1',
      lieferantName: 'Lieferant A',
      preis: 10,
      menge: 2
    });

    const fixture = TestBed.createComponent(Cart);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    const rows = compiled.querySelectorAll('tbody tr');
    expect(rows.length).toBe(1);
    expect(rows[0].textContent).toContain('Ohrhörer Compact');
    expect(rows[0].textContent).toContain('Lieferant A');
  });

  it('removes a position when the remove button is clicked', async () => {
    const cartService = TestBed.inject(CartService);
    cartService.addItem({
      produktId: 'P1',
      produktName: 'Ohrhörer Compact',
      lieferantId: 'L1',
      lieferantName: 'Lieferant A',
      preis: 10,
      menge: 1
    });

    const fixture = TestBed.createComponent(Cart);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.remove') as HTMLButtonElement).click();
    await fixture.whenStable();

    expect(cartService.cartItems().length).toBe(0);
  });
});
