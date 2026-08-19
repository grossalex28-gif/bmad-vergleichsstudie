import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { Order, OrdersApi } from '../../core/api/orders.api';
import { OrderConfirmation } from './order-confirmation';

describe('OrderConfirmation', () => {
  let ordersApiMock: { getOrder: ReturnType<typeof vi.fn> };

  const order: Order = {
    publicId: '3fa85f64-5717-4562-b3fc-2c963f66afa6',
    status: 'Received',
    delivery: {
      name: 'Mira Muster',
      street: 'Musterstraße 1',
      postalCode: '12345',
      city: 'Musterstadt',
      country: 'Deutschland',
      email: 'mira@example.com',
    },
    items: [
      { productId: 'P1', productName: 'Ohrhörer Modell Compact', supplierId: 'S1', supplierName: 'Lieferant 1', unitPrice: 27.5, quantity: 2, lineTotal: 55 },
    ],
    totalAmount: 55,
    createdAt: '2026-08-17T10:00:00Z',
  };

  beforeEach(async () => {
    ordersApiMock = { getOrder: vi.fn() };
    await TestBed.configureTestingModule({
      imports: [OrderConfirmation],
      providers: [{ provide: OrdersApi, useValue: ordersApiMock }],
    }).compileComponents();
  });

  it('lädt die Bestellung über die publicId und zeigt Status, Positionen und Gesamtsumme', () => {
    ordersApiMock.getOrder.mockReturnValue(of(order));

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.componentRef.setInput('publicId', order.publicId);
    fixture.detectChanges();

    expect(ordersApiMock.getOrder).toHaveBeenCalledWith(order.publicId);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain(order.publicId);
    expect(compiled.querySelector('.order-confirmation__status')?.textContent).toContain('Eingegangen');
    expect(compiled.querySelectorAll('.order-confirmation__item').length).toBe(1);
    const itemText = compiled.querySelector('.order-confirmation__item')?.textContent;
    expect(itemText).toContain('Ohrhörer Modell Compact');
    expect(itemText).toContain('Lieferant 1');
    expect(itemText).toContain('27.50');
    expect(itemText).toContain('55.00');
    expect(compiled.querySelector('.order-confirmation__total')?.textContent).toContain('55');
  });

  it('zeigt den Ladezustand, solange die Anfrage noch offen ist', () => {
    const response = new Subject<Order>();
    ordersApiMock.getOrder.mockReturnValue(response);

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.componentRef.setInput('publicId', order.publicId);
    fixture.detectChanges();

    let compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-confirmation__loading')).not.toBeNull();
    expect(compiled.querySelector('.order-confirmation')).toBeNull();

    response.next(order);
    fixture.detectChanges();

    compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-confirmation__loading')).toBeNull();
    expect(compiled.querySelector('.order-confirmation')).not.toBeNull();
  });

  it('ignoriert eine veraltete Antwort, wenn sich die publicId vor Abschluss der Anfrage ändert', () => {
    const firstResponse = new Subject<Order>();
    const secondOrder: Order = { ...order, publicId: 'anderer-publicId' };
    const secondResponse = new Subject<Order>();
    ordersApiMock.getOrder.mockImplementation((publicId: string) =>
      publicId === order.publicId ? firstResponse : secondResponse,
    );

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.componentRef.setInput('publicId', order.publicId);
    fixture.detectChanges();

    fixture.componentRef.setInput('publicId', secondOrder.publicId);
    fixture.detectChanges();

    secondResponse.next(secondOrder);
    firstResponse.next(order);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain(secondOrder.publicId);
    expect(compiled.textContent).not.toContain(order.publicId);
  });

  it('zeigt einen Nicht-gefunden-Zustand bei 404', () => {
    ordersApiMock.getOrder.mockReturnValue(throwError(() => ({ status: 404 })));

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.componentRef.setInput('publicId', 'unbekannt');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-confirmation__not-found')).not.toBeNull();
    expect(compiled.querySelector('.order-confirmation__error')).toBeNull();
  });

  it('zeigt einen generischen Fehlertext bei einem sonstigen Fehlerstatus', () => {
    ordersApiMock.getOrder.mockReturnValue(throwError(() => ({ status: 500 })));

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.componentRef.setInput('publicId', order.publicId);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.order-confirmation__error')).not.toBeNull();
    expect(compiled.querySelector('.order-confirmation__not-found')).toBeNull();
  });
});
