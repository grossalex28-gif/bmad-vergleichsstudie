import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CreateOrderRequest, OrderCreated, OrderDetail } from '../models/order.model';
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

  it('sends POST /api/orders with the given request unchanged and passes the response through unchanged', () => {
    const request: CreateOrderRequest = {
      name: 'Jonas',
      street: 'Hauptstr. 1',
      postalCode: '12345',
      city: 'Berlin',
      country: 'DE',
      email: 'j@example.com',
      items: [{ productId: 'P1', supplierId: 'L1', quantity: 2 }]
    };
    const mockResponse: OrderCreated = { orderId: 'ORDER-1' };

    let actual: OrderCreated | undefined;
    service.createOrder(request).subscribe((result) => (actual = result));

    const req = httpMock.expectOne('/api/orders');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual(request);
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });

  it('sends GET /api/orders/{id} and passes the response through unchanged', () => {
    const mockResponse: OrderDetail = {
      id: 'ORDER-1',
      status: 'Eingegangen',
      items: [{ productName: 'Produkt 1', supplierName: 'Lieferant 1', unitPrice: 10, quantity: 2 }],
      totalAmount: 20
    };

    let actual: OrderDetail | undefined;
    service.getOrder('ORDER-1').subscribe((result) => (actual = result));

    const req = httpMock.expectOne('/api/orders/ORDER-1');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });
});
