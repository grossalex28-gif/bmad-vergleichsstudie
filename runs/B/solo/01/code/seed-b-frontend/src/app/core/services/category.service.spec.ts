import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CategoryService } from './category.service';

describe('CategoryService', () => {
  let service: CategoryService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(CategoryService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('fetches categories from the API', () => {
    service.getCategories().subscribe((categories) => {
      expect(categories.length).toBe(1);
    });

    const req = httpMock.expectOne('/api/categories');
    req.flush([{ id: 'K1', name: 'Elektronik', unterkategorien: [] }]);
  });

  it('caches the result so a second subscriber does not trigger another request', () => {
    service.getCategories().subscribe();
    service.getCategories().subscribe();

    httpMock.expectOne('/api/categories').flush([]);
    httpMock.verify();
  });
});
