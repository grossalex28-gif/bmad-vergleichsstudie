import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap } from '@angular/router';

import { Order } from '../order.model';
import { OrderView } from './order-view';

function configureWithId(id: string | null): void {
  TestBed.configureTestingModule({
    imports: [OrderView],
    providers: [
      provideHttpClient(),
      provideHttpClientTesting(),
      { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap(id ? { id } : {}) } } }
    ]
  });
}

const fullOrder: Order = {
  orderId: 'O1',
  status: 'Neu',
  items: [
    { productId: 'P1', productName: 'Kopfhörer', supplierId: 'L1', supplierName: 'Lieferant Eins', quantity: 2, unitPrice: 10, lineTotal: 20 }
  ],
  totalPrice: 20
};

describe('OrderView', () => {
  let httpMock: HttpTestingController;

  afterEach(() => httpMock.verify());

  it('renders status, positions and total on successful load (AC 2)', () => {
    configureWithId('O1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderView);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/O1').flush(fullOrder);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Neu');
    expect(text).toContain('Kopfhörer');
    expect(text).toContain('Lieferant Eins');
    expect(text).toContain('20,00');
  });

  it('renders "Bestellung nicht gefunden." on an HTTP 404 response (AC 3)', () => {
    configureWithId('unknown');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderView);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/unknown').flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Bestellung nicht gefunden.');
  });

  it('renders "Bestellung nicht gefunden." without an HTTP request when the id route parameter is missing (AC 3, Edge Case)', () => {
    configureWithId(null);
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderView);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Bestellung nicht gefunden.');
  });

  it('renders a distinct error message (not "nicht gefunden") on a non-404 HTTP error (Edge Case)', () => {
    configureWithId('O1');
    httpMock = TestBed.inject(HttpTestingController);

    const fixture = TestBed.createComponent(OrderView);
    fixture.detectChanges();

    httpMock.expectOne('/api/orders/O1').flush(null, { status: 500, statusText: 'Internal Server Error' });
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Bestellung konnte nicht geladen werden.');
    expect(text).not.toContain('Bestellung nicht gefunden.');
  });
});
