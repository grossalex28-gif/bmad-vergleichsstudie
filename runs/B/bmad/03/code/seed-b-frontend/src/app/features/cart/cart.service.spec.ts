import { CartService } from './cart.service';

describe('CartService', () => {
  beforeEach(() => {
    localStorage.clear();
  });

  it('adds a cart item on an empty cart', () => {
    const service = new CartService();

    service.addToCart('P1', 'L1', 2);

    expect(service.items()).toEqual([{ productId: 'P1', supplierId: 'L1', quantity: 2 }]);
  });

  it('creates a separate item per supplier for the same product', () => {
    const service = new CartService();

    service.addToCart('P1', 'L1', 1);
    service.addToCart('P1', 'L2', 1);

    expect(service.items()).toEqual([
      { productId: 'P1', supplierId: 'L1', quantity: 1 },
      { productId: 'P1', supplierId: 'L2', quantity: 1 }
    ]);
  });

  it('increases the quantity of the existing item instead of duplicating it', () => {
    const service = new CartService();

    service.addToCart('P1', 'L1', 2);
    service.addToCart('P1', 'L1', 3);

    expect(service.items()).toEqual([{ productId: 'P1', supplierId: 'L1', quantity: 5 }]);
  });

  it('persists the current items to localStorage after addToCart', () => {
    const service = new CartService();

    service.addToCart('P1', 'L1', 2);

    expect(JSON.parse(localStorage.getItem('cart')!)).toEqual(service.items());
  });

  it('starts with an empty cart instead of throwing when localStorage contains corrupt data', () => {
    localStorage.setItem('cart', 'not-json');

    const service = new CartService();

    expect(service.items()).toEqual([]);
  });

  it('updates the quantity of an existing item without affecting other items', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);
    service.addToCart('P2', 'L2', 7);

    service.updateQuantity('P1', 'L1', 5);

    expect(service.items()).toEqual([
      { productId: 'P1', supplierId: 'L1', quantity: 5 },
      { productId: 'P2', supplierId: 'L2', quantity: 7 }
    ]);
  });

  it('leaves the cart unchanged when updateQuantity targets a nonexistent item', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);

    service.updateQuantity('P9', 'L9', 5);

    expect(service.items()).toEqual([{ productId: 'P1', supplierId: 'L1', quantity: 2 }]);
  });

  it('removes only the matching item, keeping the same product with a different supplier', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 1);
    service.addToCart('P1', 'L2', 1);

    service.removeItem('P1', 'L1');

    expect(service.items()).toEqual([{ productId: 'P1', supplierId: 'L2', quantity: 1 }]);
  });

  it('persists the current items to localStorage after updateQuantity', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);

    service.updateQuantity('P1', 'L1', 9);

    expect(JSON.parse(localStorage.getItem('cart')!)).toEqual(service.items());
  });

  it('persists the current items to localStorage after removeItem', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);

    service.removeItem('P1', 'L1');

    expect(JSON.parse(localStorage.getItem('cart')!)).toEqual(service.items());
  });

  it('empties a cart with multiple items on clear', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);
    service.addToCart('P2', 'L2', 3);

    service.clear();

    expect(service.items()).toEqual([]);
  });

  it('persists the empty cart to localStorage after clear', () => {
    const service = new CartService();
    service.addToCart('P1', 'L1', 2);

    service.clear();

    expect(localStorage.getItem('cart')).toBe('[]');
  });
});
