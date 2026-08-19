import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { App } from './app';
import { CartService } from './features/cart/cart.service';

describe('App', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [App],
      providers: [provideRouter([])],
    }).compileComponents();
  });

  it('should create the app', () => {
    const fixture = TestBed.createComponent(App);
    const app = fixture.componentInstance;
    expect(app).toBeTruthy();
  });

  it('zeigt im Kopfbereich einen Link zum Warenkorb mit Artikelzähler', () => {
    const fixture = TestBed.createComponent(App);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    const cartLink = element.querySelector('.app-header__cart-link') as HTMLAnchorElement;
    expect(cartLink).toBeTruthy();
    expect(cartLink.getAttribute('href')).toBe('/cart');
    expect(cartLink.textContent).toContain('0');
  });

  it('aktualisiert den Artikelzähler im Kopfbereich anhand der Warenkorb-Mengen', () => {
    const fixture = TestBed.createComponent(App);
    const cartService = TestBed.inject(CartService);
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
      quantity: 3,
    });
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;

    expect(element.querySelector('.app-header__cart-link')?.textContent).toContain('5');
  });
});
