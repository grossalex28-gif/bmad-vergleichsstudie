import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { PagedResult, Product, ProductListItem, RatingSummary } from '../models/product.model';
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

  it('sends GET /api/products?page=1 and passes the response through unchanged', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [{ id: 'P1', name: 'Produkt 1', subcategoryName: 'Sub 1', minPrice: 9.99 }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    };

    let actual: PagedResult<ProductListItem> | undefined;
    service.getProducts(1).subscribe((result) => (actual = result));

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.keys().length === 1 && r.params.get('page') === '1'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });

  it('sends GET /api/products?page=1&categoryId=K1&subcategoryId=K1a when category and subcategory are given', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service.getProducts(1, 'K1', 'K1a').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('page') === '1' &&
        r.params.get('categoryId') === 'K1' &&
        r.params.get('subcategoryId') === 'K1a'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sends GET /api/products?page=1&sortBy=price&sortDirection=desc when sorting is given without category/subcategory', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service.getProducts(1, null, null, 'price', 'desc').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.keys().length === 3 &&
        r.params.get('page') === '1' &&
        r.params.get('sortBy') === 'price' &&
        r.params.get('sortDirection') === 'desc'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sends GET /api/products?page=1&search=kabellos when a search term is given without other filters', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service.getProducts(1, null, null, undefined, undefined, 'kabellos').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.keys().length === 2 &&
        r.params.get('page') === '1' &&
        r.params.get('search') === 'kabellos'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('omits the search param when the search term is only whitespace', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service.getProducts(1, null, null, undefined, undefined, '   ').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.keys().length === 1 && r.params.get('page') === '1'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sends GET /api/products?page=1&attr=Bauform:In-Ear when one attribute filter is given', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service.getProducts(1, null, null, undefined, undefined, undefined, [{ name: 'Bauform', value: 'In-Ear' }]).subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.keys().length === 2 &&
        r.params.get('page') === '1' &&
        (r.params.getAll('attr') ?? []).length === 1 &&
        (r.params.getAll('attr') ?? [])[0] === 'Bauform:In-Ear'
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sends two attr query values when two attribute filters are given, neither overwriting the other', () => {
    const mockResponse: PagedResult<ProductListItem> = {
      items: [],
      page: 1,
      pageSize: 20,
      totalCount: 0,
      totalPages: 0
    };

    service
      .getProducts(1, null, null, undefined, undefined, undefined, [
        { name: 'Bauform', value: 'In-Ear' },
        { name: 'Kabellos', value: 'true' }
      ])
      .subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        (r.params.getAll('attr') ?? []).length === 2 &&
        (r.params.getAll('attr') ?? []).includes('Bauform:In-Ear') &&
        (r.params.getAll('attr') ?? []).includes('Kabellos:true')
    );
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);
  });

  it('sends GET /api/products/P1 and passes the response through unchanged', () => {
    const mockResponse: Product = {
      id: 'P1',
      name: 'Produkt 1',
      description: 'Beschreibung',
      categoryId: 'C1',
      categoryName: 'Kategorie 1',
      subcategoryId: 'S1',
      subcategoryName: 'Unterkategorie 1',
      attributes: [{ name: 'Bauform', value: 'In-Ear' }],
      averageRating: 4.3,
      ratingCount: 3,
      offers: [{ supplierId: 'L1', supplierName: 'Lieferant 1', price: 9.99 }]
    };

    let actual: Product | undefined;
    service.getProduct('P1').subscribe((result) => (actual = result));

    const req = httpMock.expectOne('/api/products/P1');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });

  it('sends POST /api/products/P1/ratings with authorName and value in the body and passes the response through unchanged', () => {
    const mockResponse: RatingSummary = { averageRating: 5, ratingCount: 1 };

    let actual: RatingSummary | undefined;
    service.submitRating('P1', 'Jonas', 5).subscribe((result) => (actual = result));

    const req = httpMock.expectOne('/api/products/P1/ratings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ authorName: 'Jonas', value: 5 });
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });

  it.each([[[]], [undefined], [null]])(
    'omits the attr param when attributeFilters is %p',
    (attributeFilters) => {
      const mockResponse: PagedResult<ProductListItem> = {
        items: [],
        page: 1,
        pageSize: 20,
        totalCount: 0,
        totalPages: 0
      };

      service.getProducts(1, null, null, undefined, undefined, undefined, attributeFilters).subscribe();

      const req = httpMock.expectOne(
        (r) => r.url === '/api/products' && r.params.keys().length === 1 && r.params.get('page') === '1'
      );
      expect(req.request.method).toBe('GET');
      req.flush(mockResponse);
    }
  );
});
