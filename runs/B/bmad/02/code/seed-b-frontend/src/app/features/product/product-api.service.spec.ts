import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { ProductApiService } from './product-api.service';
import { ProductDetail, RatingSubmissionResult } from './product.model';

describe('ProductApiService', () => {
  let service: ProductApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });

    service = TestBed.inject(ProductApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests the product with the given id', () => {
    const response: ProductDetail = {
      id: 'PD1',
      name: 'Detailprodukt',
      description: 'Eine Beschreibung',
      category: { id: 'CD', name: 'Detailkategorie' },
      subcategory: { id: 'SD1', name: 'Detailunterkategorie' },
      properties: [],
      averageRating: null,
      ratingCount: 0,
      offers: []
    };

    service.getProduct('PD1').subscribe();

    const req = httpMock.expectOne('/api/products/PD1');
    expect(req.request.method).toBe('GET');
    req.flush(response);
  });

  it('submits a rating via POST with author name and score in the body', () => {
    const response: RatingSubmissionResult = { averageRating: 4, ratingCount: 1 };

    service.submitRating('PD1', { authorName: 'Anna', score: 4 }).subscribe();

    const req = httpMock.expectOne('/api/products/PD1/ratings');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ authorName: 'Anna', score: 4 });
    req.flush(response);
  });
});
