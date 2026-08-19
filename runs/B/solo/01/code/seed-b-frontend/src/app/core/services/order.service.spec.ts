import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { OrderService } from './order.service';

describe('OrderService', () => {
  let service: OrderService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(OrderService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('posts a new order to /api/orders', () => {
    const order = {
      lieferdaten: { name: 'Max', strasse: 'Str. 1', plz: '12345', ort: 'Berlin', land: 'DE' },
      kontakt: { email: 'max@example.com', telefon: '0123456789' },
      positionen: [{ produktId: 'P1', lieferantId: 'L1', menge: 2 }]
    };

    service.createOrder(order).subscribe();

    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(order);
    req.flush({});
  });

  it('fetches an order by id', () => {
    service.getOrder(42).subscribe();

    const req = httpMock.expectOne('/api/orders/42');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });
});
