import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { Cart } from './cart';
import { CartService } from './cart.service';

describe('Cart', () => {
  let cartService: CartService;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Cart],
      providers: [provideRouter([])],
    }).compileComponents();
    cartService = TestBed.inject(CartService);
  });

  it('zeigt bei leerem Warenkorb einen leeren Zustand statt eines Fehlers (AC #5)', () => {
    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    expect(element.querySelector('.cart__empty')).toBeTruthy();
    expect(element.querySelectorAll('.cart__item').length).toBe(0);
  });

  it('zeigt für jede Position Lieferant, Einzelpreis, Menge, Positions- und Gesamtsumme (AC #1)', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    cartService.addItem({
      productId: 'P2',
      productName: 'Produkt 2',
      supplierId: 'S2',
      supplierName: 'Lieferant 2',
      unitPrice: 5.5,
      quantity: 3,
    });

    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();

    const element: HTMLElement = fixture.nativeElement;
    const items = element.querySelectorAll('.cart__item');
    expect(items.length).toBe(2);

    expect(items[0].querySelector('.cart__item-supplier')?.textContent).toContain('Lieferant 1');
    expect(items[0].querySelector('.cart__item-unit-price')?.textContent).toContain('10.00');
    expect((items[0].querySelector('.cart__item-quantity-input') as HTMLInputElement).value).toBe('2');
    expect(items[0].querySelector('.cart__item-line-total')?.textContent).toContain('20.00');

    expect(items[1].querySelector('.cart__item-supplier')?.textContent).toContain('Lieferant 2');
    expect(items[1].querySelector('.cart__item-line-total')?.textContent).toContain('16.50');

    expect(element.querySelector('.cart__total')?.textContent).toContain('36.50');
  });

  it('aktualisiert Positions- und Gesamtsumme sofort nach einer gültigen Mengenänderung (AC #2)', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });

    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    const input = element.querySelector('.cart__item-quantity-input') as HTMLInputElement;
    input.value = '5';
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(element.querySelector('.cart__item-line-total')?.textContent).toContain('50.00');
    expect(element.querySelector('.cart__total')?.textContent).toContain('50.00');
  });

  it('lehnt eine unzulässige Mengenänderung ab, zeigt eine Rückmeldung und behält die vorherige Menge (AC #3)', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });

    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    const input = element.querySelector('.cart__item-quantity-input') as HTMLInputElement;
    input.value = '0';
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(element.querySelector('.cart__item-error')).toBeTruthy();
    expect(element.querySelector('.cart__item-line-total')?.textContent).toContain('20.00');
    expect((element.querySelector('.cart__item-quantity-input') as HTMLInputElement).value).toBe('2');
  });

  it('lehnt eine dezimale Mengenänderung ab, zeigt eine Rückmeldung und behält die vorherige Menge (AC #3)', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });

    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    const input = element.querySelector('.cart__item-quantity-input') as HTMLInputElement;
    input.value = '2.5';
    input.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(element.querySelector('.cart__item-error')).toBeTruthy();
    expect(element.querySelector('.cart__item-line-total')?.textContent).toContain('20.00');
    expect((element.querySelector('.cart__item-quantity-input') as HTMLInputElement).value).toBe('2');
  });

  it('entfernt eine Position und aktualisiert die Gesamtsumme sofort (AC #4)', () => {
    cartService.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 10,
      quantity: 2,
    });
    cartService.addItem({
      productId: 'P2',
      productName: 'Produkt 2',
      supplierId: 'S2',
      supplierName: 'Lieferant 2',
      unitPrice: 5,
      quantity: 1,
    });

    const fixture = TestBed.createComponent(Cart);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    const removeButton = element.querySelector('.cart__item-remove') as HTMLButtonElement;
    removeButton.click();
    fixture.detectChanges();

    expect(element.querySelectorAll('.cart__item').length).toBe(1);
    expect(element.querySelector('.cart__total')?.textContent).toContain('5.00');
  });
});
