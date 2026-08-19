import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { Order } from './order.model';
import { OrderApiService } from './order-api.service';

describe('OrderApiService', () => {
  let service: OrderApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({ providers: [provideHttpClient(), provideHttpClientTesting()] });
    service = TestBed.inject(OrderApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('places an order via POST with the X-Cart-Id header and body fields', () => {
    const response: Order = { orderId: 'O1', status: 'Neu', items: [], totalPrice: 0 };

    service.placeOrder('C1', 'Mara Muster', 'Musterstraße 1', 'mara@example.com').subscribe();

    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    expect(req.request.headers.get('X-Cart-Id')).toBe('C1');
    expect(req.request.body).toEqual({
      customerName: 'Mara Muster',
      deliveryAddress: 'Musterstraße 1',
      email: 'mara@example.com'
    });
    req.flush(response);
  });

  it('fetches an order via GET by id', () => {
    const response: Order = { orderId: 'O1', status: 'Neu', items: [], totalPrice: 0 };

    service.getOrder('O1').subscribe();

    const req = httpMock.expectOne('/api/orders/O1');
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });
});
