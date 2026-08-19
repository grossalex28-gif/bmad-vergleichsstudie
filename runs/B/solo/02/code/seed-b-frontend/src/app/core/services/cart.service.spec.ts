import { TestBed } from '@angular/core/testing';
import { CartService } from './cart.service';

describe('CartService', () => {
  let service: CartService;

  beforeEach(() => {
    localStorage.clear();
    TestBed.configureTestingModule({});
    service = TestBed.inject(CartService);
  });

  it('starts empty', () => {
    expect(service.cartItems()).toEqual([]);
    expect(service.itemCount()).toBe(0);
    expect(service.total()).toBe(0);
  });

  it('adds a new item', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      2,
    );

    expect(service.cartItems().length).toBe(1);
    expect(service.itemCount()).toBe(2);
    expect(service.total()).toBeCloseTo(59.98);
  });

  it('merges quantity when the same product and supplier are added again', () => {
    const item = { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 };
    service.addItem(item, 1);
    service.addItem(item, 2);

    expect(service.cartItems().length).toBe(1);
    expect(service.cartItems()[0].quantity).toBe(3);
  });

  it('keeps the same product from different suppliers as separate positions', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      1,
    );
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 2, supplierName: 'Blitzversand', unitPrice: 27.5 },
      1,
    );

    expect(service.cartItems().length).toBe(2);
  });

  it('updates the quantity of an existing position', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      1,
    );

    service.updateQuantity(1, 1, 5);

    expect(service.cartItems()[0].quantity).toBe(5);
  });

  it('removes a position when its quantity is set below one', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      1,
    );

    service.updateQuantity(1, 1, 0);

    expect(service.cartItems()).toEqual([]);
  });

  it('removes a position explicitly', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      1,
    );

    service.removeItem(1, 1);

    expect(service.cartItems()).toEqual([]);
  });

  it('clears the cart', () => {
    service.addItem(
      { productId: 1, productName: 'Ohrhörer', subcategoryId: 3, supplierId: 1, supplierName: 'NordTech', unitPrice: 29.99 },
      1,
    );

    service.clear();

    expect(service.cartItems()).toEqual([]);
  });
});
