import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { CategorySummary, ProductListResponse } from '../product.model';
import { ProductList } from './product-list';

function expectProductsRequest(
  httpMock: HttpTestingController,
  page: number,
  response: ProductListResponse,
  categoryId?: string,
  subcategoryId?: string,
  sortBy?: string,
  sortDirection?: string,
  search?: string,
  propertyFilters?: Record<string, string>
) {
  const req = httpMock.expectOne((r) => {
    if (
      r.url !== '/api/products' ||
      r.params.get('page') !== String(page) ||
      r.params.get('categoryId') !== (categoryId ?? null) ||
      r.params.get('subcategoryId') !== (subcategoryId ?? null) ||
      r.params.get('sortBy') !== (sortBy ?? null) ||
      r.params.get('sortDirection') !== (sortDirection ?? null) ||
      r.params.get('search') !== (search ?? null)
    ) {
      return false;
    }
    const actualPropertyKeys = r.params.keys().filter((key) => key.startsWith('properties['));
    const expectedEntries = Object.entries(propertyFilters ?? {});
    if (actualPropertyKeys.length !== expectedEntries.length) {
      return false;
    }
    return expectedEntries.every(([name, value]) => r.params.get(`properties[${name}]`) === value);
  });
  req.flush(response);
}

function expectCategoriesRequest(httpMock: HttpTestingController, response: CategorySummary[] = []) {
  const req = httpMock.expectOne((r) => r.url === '/api/categories');
  req.flush(response);
}

const categories: CategorySummary[] = [
  {
    id: 'K1',
    name: 'Elektronik',
    subcategories: [{ id: 'K1a', name: 'Smartphones', properties: ['Bauform', 'Kabellos'] }]
  }
];

describe('ProductList', () => {
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    });

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('renders the names of the returned items', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, {
      items: [
        { id: 'T01', name: 'Produkt Eins' },
        { id: 'T02', name: 'Produkt Zwei' }
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2
    });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Produkt Eins');
    expect(text).toContain('Produkt Zwei');
  });

  it('disables "Zurück" and enables "Weiter" on the first page when more pages exist', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 25 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const backButton = Array.from(buttons).find((b) => b.textContent?.includes('Zurück')) as HTMLButtonElement;
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;

    expect(backButton.disabled).toBe(true);
    expect(nextButton.disabled).toBe(false);
  });

  it('disables "Weiter" on the last page', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 20 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;

    expect(nextButton.disabled).toBe(true);
  });

  it('requests the next page when "Weiter" is clicked', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 25 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;
    nextButton.click();

    expectProductsRequest(httpMock, 2, { items: [], page: 2, pageSize: 20, totalCount: 25 });
  });

  it('requests filtered products and updates the list when a category in CategoryNav is clicked', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(
      httpMock,
      1,
      { items: [{ id: 'P1', name: 'Elektronikprodukt' }], page: 1, pageSize: 20, totalCount: 1 },
      'K1'
    );
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Elektronikprodukt');
  });

  it('requests filtered products and updates the list when a subcategory in CategoryNav is clicked (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const subcategoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Smartphones')) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(
      httpMock,
      1,
      { items: [{ id: 'P1', name: 'Smartphone X' }], page: 1, pageSize: 20, totalCount: 1 },
      undefined,
      'K1a'
    );
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Smartphone X');
  });

  it('keeps the categoryId parameter when paging after selecting a category (AC 4)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    let buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 25 }, 'K1');
    fixture.detectChanges();

    buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;
    nextButton.click();

    expectProductsRequest(httpMock, 2, { items: [], page: 2, pageSize: 20, totalCount: 25 }, 'K1');
  });

  it('requests products without categoryId/subcategoryId when "Alle Kategorien" is clicked after a selection', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    let buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 25 }, 'K1');
    fixture.detectChanges();

    buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const clearButton = Array.from(buttons).find((b) => b.textContent?.includes('Alle Kategorien')) as HTMLButtonElement;
    clearButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 25 });
  });

  it('requests sorted products when "Preis aufsteigend" is selected in SortSelect', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, undefined, 'price', 'asc');
  });

  it('keeps sortBy/sortDirection when paging after selecting a sort order (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 25 },
      undefined,
      undefined,
      'price',
      'asc'
    );
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;
    nextButton.click();

    expectProductsRequest(
      httpMock,
      2,
      { items: [], page: 2, pageSize: 20, totalCount: 25 },
      undefined,
      undefined,
      'price',
      'asc'
    );
  });

  it('keeps sortBy/sortDirection when a category in CategoryNav is selected after sorting (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      undefined,
      'price',
      'asc'
    );
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, 'K1', undefined, 'price', 'asc');
  });

  it('requests searched products when a search term is submitted in SearchBox', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const searchInput = element.querySelector('input[type="search"]') as HTMLInputElement;
    const searchForm = element.querySelector('form') as HTMLFormElement;
    searchInput.value = 'Kaffee';
    searchForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
  });

  it('keeps the search parameter when paging after a search (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const searchInput = element.querySelector('input[type="search"]') as HTMLInputElement;
    const searchForm = element.querySelector('form') as HTMLFormElement;
    searchInput.value = 'Kaffee';
    searchForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 25 },
      undefined,
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
    fixture.detectChanges();

    const buttons = element.querySelectorAll('button');
    const nextButton = Array.from(buttons).find((b) => b.textContent?.includes('Weiter')) as HTMLButtonElement;
    nextButton.click();

    expectProductsRequest(
      httpMock,
      2,
      { items: [], page: 2, pageSize: 20, totalCount: 25 },
      undefined,
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
  });

  it('keeps the search parameter when a category is selected after a search (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const searchInput = element.querySelector('input[type="search"]') as HTMLInputElement;
    const searchForm = element.querySelector('form') as HTMLFormElement;
    searchInput.value = 'Kaffee';
    searchForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
    fixture.detectChanges();

    const buttons = element.querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      'K1',
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
  });

  it('keeps the search parameter when a sort order is selected after a search (AC 3)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const searchInput = element.querySelector('input[type="search"]') as HTMLInputElement;
    const searchForm = element.querySelector('form') as HTMLFormElement;
    searchInput.value = 'Kaffee';
    searchForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      undefined,
      undefined,
      undefined,
      'Kaffee'
    );
    fixture.detectChanges();

    const select = element.querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      undefined,
      'price',
      'asc',
      'Kaffee'
    );
  });

  it('renders app-property-filters with the selected subcategorys properties when a subcategory is selected', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const subcategoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Smartphones')) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('input[name="Bauform"]')).toBeTruthy();
    expect(element.querySelector('input[name="Kabellos"]')).toBeTruthy();
  });

  it('does not render app-property-filters when no subcategory is selected (AC 7)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('input[name="Bauform"]')).toBeFalsy();

    const buttons = element.querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, 'K1');
    fixture.detectChanges();

    expect(element.querySelector('input[name="Bauform"]')).toBeFalsy();
  });

  it('requests products with a property filter when the PropertyFilters form is submitted', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const subcategoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Smartphones')
    ) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const filterForm = element.querySelectorAll('form')[1] as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    filterForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
  });

  it('keeps the property filter when paging after setting it (AC 5)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const subcategoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Smartphones')
    ) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const filterForm = element.querySelectorAll('form')[1] as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    filterForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 25 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
    fixture.detectChanges();

    const nextButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Weiter')
    ) as HTMLButtonElement;
    nextButton.click();

    expectProductsRequest(
      httpMock,
      2,
      { items: [], page: 2, pageSize: 20, totalCount: 25 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
  });

  it('keeps the property filter when a sort order is selected after setting it (AC 5)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const subcategoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Smartphones')
    ) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const filterForm = element.querySelectorAll('form')[1] as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    filterForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
    fixture.detectChanges();

    const select = element.querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      'price',
      'asc',
      undefined,
      { Bauform: 'In-Ear' }
    );
  });

  it('combines the property filter with a search term (AC 5)', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const subcategoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Smartphones')
    ) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const filterForm = element.querySelectorAll('form')[1] as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    filterForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
    fixture.detectChanges();

    const searchInput = element.querySelector('input[type="search"]') as HTMLInputElement;
    const searchForm = element.querySelectorAll('form')[0] as HTMLFormElement;
    searchInput.value = 'Kaffee';
    searchForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      undefined,
      undefined,
      'Kaffee',
      { Bauform: 'In-Ear' }
    );
  });

  it('resets the property filter when the subcategory selection changes', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 });
    expectCategoriesRequest(httpMock, categories);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const subcategoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Smartphones')
    ) as HTMLButtonElement;
    subcategoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, undefined, 'K1a');
    fixture.detectChanges();

    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const filterForm = element.querySelectorAll('form')[1] as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    filterForm.dispatchEvent(new Event('submit'));

    expectProductsRequest(
      httpMock,
      1,
      { items: [], page: 1, pageSize: 20, totalCount: 0 },
      undefined,
      'K1a',
      undefined,
      undefined,
      undefined,
      { Bauform: 'In-Ear' }
    );
    fixture.detectChanges();

    const categoryButton = Array.from(element.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Elektronik')
    ) as HTMLButtonElement;
    categoryButton.click();

    expectProductsRequest(httpMock, 1, { items: [], page: 1, pageSize: 20, totalCount: 0 }, 'K1');
  });

  it('links each rendered product to its detail page', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expectProductsRequest(httpMock, 1, {
      items: [
        { id: 'T01', name: 'Produkt Eins' },
        { id: 'T02', name: 'Produkt Zwei' }
      ],
      page: 1,
      pageSize: 20,
      totalCount: 2
    });
    expectCategoriesRequest(httpMock);
    fixture.detectChanges();

    const links = (fixture.nativeElement as HTMLElement).querySelectorAll('a');
    expect(links.length).toBe(2);
    expect(links[0].getAttribute('href')).toBe('/products/T01');
    expect(links[1].getAttribute('href')).toBe('/products/T02');
  });
});
