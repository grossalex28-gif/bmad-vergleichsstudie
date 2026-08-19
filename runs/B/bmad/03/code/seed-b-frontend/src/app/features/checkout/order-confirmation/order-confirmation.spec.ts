import { HttpErrorResponse } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, ParamMap, convertToParamMap } from '@angular/router';
import { Subject, of, throwError } from 'rxjs';

import { OrderDetail } from '../../../core/models/order.model';
import { OrderService } from '../../../core/services/order.service';
import { OrderConfirmation } from './order-confirmation';

describe('OrderConfirmation', () => {
  const orderDetail: OrderDetail = {
    id: 'ORDER-1',
    status: 'Eingegangen',
    items: [
      { productName: 'Produkt 1', supplierName: 'Lieferant 1', unitPrice: 10, quantity: 2 },
      { productName: 'Produkt 2', supplierName: 'Lieferant 1', unitPrice: 5, quantity: 3 }
    ],
    totalAmount: 35
  };

  let getOrder: ReturnType<typeof vi.fn>;
  let paramMap: Subject<ParamMap>;

  beforeEach(() => {
    getOrder = vi.fn(() => of(orderDetail));
    paramMap = new Subject<ParamMap>();

    TestBed.configureTestingModule({
      imports: [OrderConfirmation],
      providers: [
        { provide: OrderService, useValue: { getOrder } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } }
      ]
    });
  });

  it('shows status, all items with supplier/price/quantity and the total for the routed order id', () => {
    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'ORDER-1' }));
    fixture.detectChanges();

    expect(getOrder).toHaveBeenCalledWith('ORDER-1');
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Eingegangen');
    expect(compiled.textContent).toContain('Produkt 1');
    expect(compiled.textContent).toContain('Lieferant 1');
    expect(compiled.textContent).toContain('Produkt 2');
  });

  it('shows a "nicht gefunden" message on a 404 response', () => {
    getOrder = vi.fn(() => throwError(() => new HttpErrorResponse({ status: 404 })));
    TestBed.configureTestingModule({
      imports: [OrderConfirmation],
      providers: [
        { provide: OrderService, useValue: { getOrder } },
        { provide: ActivatedRoute, useValue: { paramMap: paramMap.asObservable() } }
      ]
    });

    const fixture = TestBed.createComponent(OrderConfirmation);
    fixture.detectChanges();
    paramMap.next(convertToParamMap({ id: 'UNKNOWN' }));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('nicht gefunden');
  });
});
