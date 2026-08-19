import { CartService } from './cart.service';

describe('CartService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  function makeItem(overrides: Partial<Parameters<CartService['addItem']>[0]> = {}) {
    return {
      produktId: 'P1',
      produktName: 'Ohrhörer Compact',
      lieferantId: 'L1',
      lieferantName: 'Lieferant A',
      preis: 10,
      menge: 1,
      ...overrides
    };
  }

  it('starts empty', () => {
    const service = new CartService();
    expect(service.cartItems()).toEqual([]);
    expect(service.itemCount()).toBe(0);
    expect(service.total()).toBe(0);
  });

  it('adds a new item as its own position', () => {
    const service = new CartService();
    service.addItem(makeItem());

    expect(service.cartItems().length).toBe(1);
    expect(service.itemCount()).toBe(1);
    expect(service.total()).toBe(10);
  });

  it('merges quantities when the same product and supplier are added again', () => {
    const service = new CartService();
    service.addItem(makeItem({ menge: 2 }));
    service.addItem(makeItem({ menge: 3 }));

    expect(service.cartItems().length).toBe(1);
    expect(service.cartItems()[0].menge).toBe(5);
  });

  it('keeps the same product from different suppliers as separate positions', () => {
    const service = new CartService();
    service.addItem(makeItem({ lieferantId: 'L1' }));
    service.addItem(makeItem({ lieferantId: 'L2', lieferantName: 'Lieferant B', preis: 12 }));

    expect(service.cartItems().length).toBe(2);
    expect(service.total()).toBe(22);
  });

  it('updates the quantity of a specific position', () => {
    const service = new CartService();
    service.addItem(makeItem({ menge: 1 }));
    service.updateQuantity('P1', 'L1', 4);

    expect(service.cartItems()[0].menge).toBe(4);
    expect(service.total()).toBe(40);
  });

  it('removes a position', () => {
    const service = new CartService();
    service.addItem(makeItem({ lieferantId: 'L1' }));
    service.addItem(makeItem({ lieferantId: 'L2', lieferantName: 'Lieferant B' }));
    service.removeItem('P1', 'L1');

    expect(service.cartItems().length).toBe(1);
    expect(service.cartItems()[0].lieferantId).toBe('L2');
  });

  it('clears all positions', () => {
    const service = new CartService();
    service.addItem(makeItem());
    service.clear();

    expect(service.cartItems()).toEqual([]);
  });

  it('persists positions across instances via localStorage', () => {
    const first = new CartService();
    first.addItem(makeItem());

    const second = new CartService();
    expect(second.cartItems().length).toBe(1);
    expect(second.cartItems()[0].produktId).toBe('P1');
  });
});
