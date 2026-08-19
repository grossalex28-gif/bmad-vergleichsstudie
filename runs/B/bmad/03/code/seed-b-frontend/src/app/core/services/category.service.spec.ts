import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';

import { CategoryNavItem } from '../models/category.model';
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

  it('sends GET /api/categories and passes the response through unchanged', () => {
    const mockResponse: CategoryNavItem[] = [
      {
        id: 'K1',
        name: 'Kategorie 1',
        subcategories: [{ id: 'K1a', name: 'Unterkategorie 1a' }]
      }
    ];

    let actual: CategoryNavItem[] | undefined;
    service.getCategories().subscribe((result) => (actual = result));

    const req = httpMock.expectOne('/api/categories');
    expect(req.request.method).toBe('GET');
    req.flush(mockResponse);

    expect(actual).toEqual(mockResponse);
  });
});
