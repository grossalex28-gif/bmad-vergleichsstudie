import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CatalogApiService } from './catalog-api.service';
import { CategorySummary, ProductListResponse } from './product.model';

describe('CatalogApiService', () => {
  let service: CatalogApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(CatalogApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests products for the given page', () => {
    const response: ProductListResponse = { items: [], page: 2, pageSize: 20, totalCount: 0 };

    service.getProducts(2).subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/products' && r.params.get('page') === '2');
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('requests categories', () => {
    const response: CategorySummary[] = [];

    service.getCategories().subscribe();

    const req = httpMock.expectOne((r) => r.url === '/api/categories');
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('requests products with a categoryId and without a subcategoryId', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, 'K1').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('page') === '1' && r.params.get('categoryId') === 'K1'
    );
    expect(req.request.params.has('subcategoryId')).toBe(false);
    req.flush(response);
  });

  it('requests products with a subcategoryId and without a categoryId', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, undefined, 'K1a').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('page') === '1' && r.params.get('subcategoryId') === 'K1a'
    );
    expect(req.request.params.has('categoryId')).toBe(false);
    req.flush(response);
  });

  it('prefers subcategoryId over categoryId when both are provided', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, 'K1', 'K1a').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('page') === '1' && r.params.get('subcategoryId') === 'K1a'
    );
    expect(req.request.params.has('categoryId')).toBe(false);
    req.flush(response);
  });

  it('requests products with sortBy and sortDirection but without categoryId/subcategoryId', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, undefined, undefined, 'price', 'asc').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('page') === '1' &&
        r.params.get('sortBy') === 'price' &&
        r.params.get('sortDirection') === 'asc'
    );
    expect(req.request.params.has('categoryId')).toBe(false);
    expect(req.request.params.has('subcategoryId')).toBe(false);
    req.flush(response);
  });

  it('requests products with categoryId, sortBy and sortDirection combined', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, 'K1', undefined, 'name', 'desc').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('page') === '1' &&
        r.params.get('categoryId') === 'K1' &&
        r.params.get('sortBy') === 'name' &&
        r.params.get('sortDirection') === 'desc'
    );
    req.flush(response);
  });

  it('requests products with a search term but without categoryId/subcategoryId/sortBy/sortDirection', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, undefined, undefined, undefined, undefined, 'Kaffee').subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('page') === '1' && r.params.get('search') === 'Kaffee'
    );
    expect(req.request.params.has('categoryId')).toBe(false);
    expect(req.request.params.has('subcategoryId')).toBe(false);
    expect(req.request.params.has('sortBy')).toBe(false);
    expect(req.request.params.has('sortDirection')).toBe(false);
    req.flush(response);
  });

  it('requests products with categoryId, sortBy, sortDirection and search combined', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, 'K1', undefined, 'name', 'desc', 'Kaffee').subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('page') === '1' &&
        r.params.get('categoryId') === 'K1' &&
        r.params.get('sortBy') === 'name' &&
        r.params.get('sortDirection') === 'desc' &&
        r.params.get('search') === 'Kaffee'
    );
    req.flush(response);
  });

  it('requests products with a single property filter', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service.getProducts(1, undefined, undefined, undefined, undefined, undefined, { Bauform: 'In-Ear' }).subscribe();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/products' && r.params.get('page') === '1' && r.params.get('properties[Bauform]') === 'In-Ear'
    );
    req.flush(response);
  });

  it('requests products with multiple property filters combined with subcategoryId', () => {
    const response: ProductListResponse = { items: [], page: 1, pageSize: 20, totalCount: 0 };

    service
      .getProducts(1, undefined, 'SF1', undefined, undefined, undefined, { Bauform: 'In-Ear', Kabellos: 'true' })
      .subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('page') === '1' &&
        r.params.get('subcategoryId') === 'SF1' &&
        r.params.get('properties[Bauform]') === 'In-Ear' &&
        r.params.get('properties[Kabellos]') === 'true'
    );
    req.flush(response);
  });
});
