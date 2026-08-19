import { CartService } from './cart.service';

describe('CartService', () => {
  it('legt bei leerem Warenkorb eine neue Position an', () => {
    const service = new CartService();

    service.addItem({
      productId: 'P1',
      productName: 'Ohrhörer Modell Compact',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 27.5,
      quantity: 2,
    });

    expect(service.items()).toEqual([
      {
        productId: 'P1',
        productName: 'Ohrhörer Modell Compact',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      },
    ]);
  });

  it('legt für dasselbe Produkt bei unterschiedlichen Lieferanten zwei getrennte Positionen an', () => {
    const service = new CartService();

    service.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 27.5,
      quantity: 1,
    });
    service.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S2',
      supplierName: 'Lieferant 2',
      unitPrice: 29.9,
      quantity: 1,
    });

    expect(service.items().length).toBe(2);
    expect(service.items()[0].supplierId).toBe('S1');
    expect(service.items()[1].supplierId).toBe('S2');
  });

  it('erhöht bei gleichem Produkt und gleichem Lieferant die Menge der bestehenden Position', () => {
    const service = new CartService();

    service.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 27.5,
      quantity: 2,
    });
    service.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 27.5,
      quantity: 3,
    });

    expect(service.items().length).toBe(1);
    expect(service.items()[0].quantity).toBe(5);
  });

  it('gibt über items() den aktuellen Stand zurück, ohne dass eine neue Instanz nötig ist', () => {
    const service = new CartService();

    expect(service.items()).toEqual([]);

    service.addItem({
      productId: 'P1',
      productName: 'Produkt 1',
      supplierId: 'S1',
      supplierName: 'Lieferant 1',
      unitPrice: 27.5,
      quantity: 1,
    });

    expect(service.items().length).toBe(1);
  });

  describe('updateQuantity', () => {
    it('übernimmt eine gültige neue Menge und gibt true zurück', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });

      const result = service.updateQuantity('P1', 'S1', 5);

      expect(result).toBe(true);
      expect(service.items()[0].quantity).toBe(5);
    });

    it.each([0, -1, 2.5])('lehnt eine unzulässige Menge (%s) ab und lässt die vorherige Menge bestehen', (invalidQuantity) => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });

      const result = service.updateQuantity('P1', 'S1', invalidQuantity);

      expect(result).toBe(false);
      expect(service.items()[0].quantity).toBe(2);
    });

    it('gibt false zurück und verändert nichts, wenn die Kombination aus Produkt und Lieferant nicht existiert', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });

      const result = service.updateQuantity('P1', 'S2', 5);

      expect(result).toBe(false);
      expect(service.items()).toEqual([
        {
          productId: 'P1',
          productName: 'Produkt 1',
          supplierId: 'S1',
          supplierName: 'Lieferant 1',
          unitPrice: 27.5,
          quantity: 2,
        },
      ]);
    });
  });

  describe('removeItem', () => {
    it('entfernt genau die passende Position und lässt andere Positionen bestehen', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });
      service.addItem({
        productId: 'P2',
        productName: 'Produkt 2',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 10,
        quantity: 1,
      });

      service.removeItem('P1', 'S1');

      expect(service.items().length).toBe(1);
      expect(service.items()[0].productId).toBe('P2');
    });

    it('verändert den Warenkorb nicht, wenn die Kombination nicht existiert', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });

      service.removeItem('P1', 'S2');

      expect(service.items().length).toBe(1);
    });
  });

  describe('clear', () => {
    it('leert einen gefüllten Warenkorb vollständig', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 27.5,
        quantity: 2,
      });
      service.addItem({
        productId: 'P2',
        productName: 'Produkt 2',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 10,
        quantity: 1,
      });

      service.clear();

      expect(service.items()).toEqual([]);
    });
  });

  describe('totalAmount', () => {
    it('ist 0 bei leerem Warenkorb', () => {
      const service = new CartService();

      expect(service.totalAmount()).toBe(0);
    });

    it('summiert unitPrice × quantity über mehrere Positionen', () => {
      const service = new CartService();
      service.addItem({
        productId: 'P1',
        productName: 'Produkt 1',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 10,
        quantity: 2,
      });
      service.addItem({
        productId: 'P2',
        productName: 'Produkt 2',
        supplierId: 'S1',
        supplierName: 'Lieferant 1',
        unitPrice: 5.5,
        quantity: 3,
      });

      expect(service.totalAmount()).toBeCloseTo(10 * 2 + 5.5 * 3);
    });
  });
});
