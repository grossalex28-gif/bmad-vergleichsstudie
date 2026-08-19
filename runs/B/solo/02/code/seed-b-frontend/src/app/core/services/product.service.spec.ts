import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { ProductService } from './product.service';

describe('ProductService', () => {
  let service: ProductService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(ProductService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('builds query params for category, sort and property filters', () => {
    service
      .getProducts({
        categoryId: 1,
        search: 'Kopfhörer',
        sortBy: 'price',
        sortDir: 'desc',
        page: 2,
        properties: { Bauform: 'Over-Ear', Kabellos: '' },
      })
      .subscribe();

    const req = httpMock.expectOne(
      (r) =>
        r.url === '/api/products' &&
        r.params.get('categoryId') === '1' &&
        r.params.get('search') === 'Kopfhörer' &&
        r.params.get('sortBy') === 'price' &&
        r.params.get('sortDir') === 'desc' &&
        r.params.get('page') === '2' &&
        r.params.get('Bauform') === 'Over-Ear' &&
        !r.params.has('Kabellos'),
    );
    req.flush({ items: [], totalCount: 0, page: 2, pageSize: 12 });
  });

  it('requests a single product by id', () => {
    service.getProduct(5).subscribe();

    const req = httpMock.expectOne('/api/products/5');
    expect(req.request.method).toBe('GET');
    req.flush({});
  });

  it('posts a review to the product endpoint', () => {
    service.addReview(5, { authorName: 'Max', rating: 4 }).subscribe();

    const req = httpMock.expectOne('/api/products/5/reviews');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ authorName: 'Max', rating: 4 });
    req.flush({ averageRating: 4, ratingCount: 1 });
  });
});
