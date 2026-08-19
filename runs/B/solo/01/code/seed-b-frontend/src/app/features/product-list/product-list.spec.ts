import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideZonelessChangeDetection } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { BehaviorSubject } from 'rxjs';

import { ProductList } from './product-list';

describe('ProductList', () => {
  let httpMock: HttpTestingController;
  let router: Router;
  let queryParamMap$: BehaviorSubject<ReturnType<typeof convertToParamMap>>;

  const categories = [
    {
      id: 'K1',
      name: 'Elektronik',
      unterkategorien: [{ id: 'K1a', name: 'Kopfhörer', eigenschaften: ['Bauform', 'Kabellos'] }]
    }
  ];

  const products = {
    items: [
      {
        id: 'P1',
        name: 'Ohrhörer Compact',
        beschreibung: 'Kompakte In-Ear-Kopfhörer.',
        kategorieId: 'K1',
        kategorieName: 'Elektronik',
        unterkategorieId: 'K1a',
        unterkategorieName: 'Kopfhörer',
        eigenschaften: { Bauform: 'In-Ear', Kabellos: true },
        minPreis: 27.5,
        durchschnittsBewertung: 4.5,
        anzahlBewertungen: 2,
        aufrufe: 10
      }
    ],
    page: 1,
    pageSize: 12,
    totalCount: 1,
    totalPages: 1
  };

  beforeEach(() => {
    queryParamMap$ = new BehaviorSubject(convertToParamMap({}));

    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideZonelessChangeDetection(),
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: {
            queryParamMap: queryParamMap$,
            snapshot: { queryParams: {} }
          }
        }
      ]
    });

    httpMock = TestBed.inject(HttpTestingController);
    router = TestBed.inject(Router);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders products returned for the current filters', async () => {
    const fixture = TestBed.createComponent(ProductList);
    await fixture.whenStable();

    httpMock.expectOne('/api/categories').flush(categories);
    httpMock.expectOne((r) => r.url === '/api/products').flush(products);
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.product-card').length).toBe(1);
    expect(compiled.textContent).toContain('Ohrhörer Compact');
  });

  it('shows an empty state when no products match', async () => {
    const fixture = TestBed.createComponent(ProductList);
    await fixture.whenStable();

    httpMock.expectOne('/api/categories').flush(categories);
    httpMock.expectOne((r) => r.url === '/api/products').flush({ items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 });
    await fixture.whenStable();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.empty')?.textContent).toContain('Keine Produkte gefunden');
  });

  it('navigates with the subcategory and resets the page when a category tree entry is clicked', async () => {
    const fixture = TestBed.createComponent(ProductList);
    await fixture.whenStable();

    httpMock.expectOne('/api/categories').flush(categories);
    httpMock.expectOne((r) => r.url === '/api/products').flush(products);
    await fixture.whenStable();

    const navigateSpy = vi.spyOn(router, 'navigate');
    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(compiled.querySelectorAll('.tree-link'));
    const subcategoryButton = buttons.find((b) => b.textContent?.trim() === 'Kopfhörer') as HTMLButtonElement;
    subcategoryButton.click();

    expect(navigateSpy).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ categoryId: 'K1', subcategoryId: 'K1a' }) })
    );
  });

  it('navigates with a search term when the search box is submitted', async () => {
    const fixture = TestBed.createComponent(ProductList);
    await fixture.whenStable();

    httpMock.expectOne('/api/categories').flush(categories);
    httpMock.expectOne((r) => r.url === '/api/products').flush(products);
    await fixture.whenStable();

    const navigateSpy = vi.spyOn(router, 'navigate');
    const compiled = fixture.nativeElement as HTMLElement;
    const input = compiled.querySelector('input[type="search"]') as HTMLInputElement;
    input.value = 'kopfhörer';
    input.dispatchEvent(new Event('change'));

    expect(navigateSpy).toHaveBeenCalledWith(
      [],
      expect.objectContaining({ queryParams: expect.objectContaining({ search: 'kopfhörer' }) })
    );
  });
});
