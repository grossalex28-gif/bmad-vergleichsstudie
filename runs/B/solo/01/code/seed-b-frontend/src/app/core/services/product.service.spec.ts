import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { TestBed } from '@angular/core/testing';

import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests products without params when the query is empty', () => {
    service.getProducts({}).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/products');
    expect(req.request.params.keys().length).toBe(0);
    req.flush({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 });
  });

  it('encodes search, category, sort and page as query params', () => {
    service
      .getProducts({ search: 'kopfhörer', categoryId: 'K1', subcategoryId: 'K1a', sort: 'price_desc', page: 2 })
      .subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('search') === 'kopfhörer' &&
        r.params.get('categoryId') === 'K1' &&
        r.params.get('subcategoryId') === 'K1a' &&
        r.params.get('sort') === 'price_desc' &&
        r.params.get('page') === '2'
    );
    req.flush({ items: [], page: 2, pageSize: 12, totalCount: 0, totalPages: 0 });
  });

  it('prefixes eigenschaften filters with eig. and omits empty values', () => {
    service.getProducts({ eigenschaften: { Bauform: 'In-Ear', Kabellos: '' } }).subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('eig.Bauform') === 'In-Ear' && !r.params.has('eig.Kabellos')
    );
    req.flush({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 });
  });

  it('fetches a single product by id', () => {
    service.getProduct('P1').subscribe();

    const req = httpMock.expectOne('/api/products/P1');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('posts a rating for a product', () => {
    service.addRating('P1', { autorName: 'Anna', wert: 5 }).subscribe();

    const req = httpMock.expectOne('/api/products/P1/ratings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ autorName: 'Anna', wert: 5 });
    req.flush({ durchschnittsBewertung: 5, anzahlBewertungen: 1 });
  });
});
