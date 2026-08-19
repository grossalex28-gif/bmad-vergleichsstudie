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
    expect(service.items()).toEqual([]);
    expect(service.itemCount()).toBe(0);
    expect(service.total()).toBe(0);
  });

  it('adds a new item', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 2,
    });

    expect(service.items().length).toBe(1);
    expect(service.itemCount()).toBe(2);
    expect(service.total()).toBeCloseTo(59.98);
  });

  it('merges quantities when the same product is added for the same supplier again', () => {
    const item = {
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    };
    service.addItem(item);
    service.addItem(item);

    expect(service.items().length).toBe(1);
    expect(service.items()[0].quantity).toBe(2);
  });

  it('treats the same product from a different supplier as a separate line item', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    });
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L2',
      supplierName: 'Blitzversand',
      unitPrice: 27.5,
      quantity: 1,
    });

    expect(service.items().length).toBe(2);
    expect(service.itemCount()).toBe(2);
  });

  it('updates the quantity of an existing line item', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    });

    service.updateQuantity('P1', 'L1', 5);

    expect(service.items()[0].quantity).toBe(5);
  });

  it('ignores quantity updates below one', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 3,
    });

    service.updateQuantity('P1', 'L1', 0);

    expect(service.items()[0].quantity).toBe(3);
  });

  it('removes a line item', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    });

    service.removeItem('P1', 'L1');

    expect(service.items()).toEqual([]);
  });

  it('clears the cart', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    });

    service.clear();

    expect(service.items()).toEqual([]);
    expect(service.total()).toBe(0);
  });

  it('persists across service instances via localStorage', () => {
    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer',
      supplierId: 'L1',
      supplierName: 'NordTech',
      unitPrice: 29.99,
      quantity: 1,
    });

    const secondInstance = new CartService();

    expect(secondInstance.items().length).toBe(1);
  });
});
