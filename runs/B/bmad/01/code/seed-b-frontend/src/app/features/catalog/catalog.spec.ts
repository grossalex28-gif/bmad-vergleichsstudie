import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Category, CategoriesApi } from '../../core/api/categories.api';
import { PagedResult, ProductListItem, ProductsApi } from '../../core/api/products.api';
import { SubcategoriesApi } from '../../core/api/subcategories.api';
import { Catalog } from './catalog';

function pageResult(page: number, items: ProductListItem[], pageCount = 2): PagedResult<ProductListItem> {
  return { items, totalCount: 25, pageCount, page, pageSize: 20 };
}

describe('Catalog', () => {
  let productsApiMock: { getProducts: ReturnType<typeof vi.fn> };
  let categoriesApiMock: { getCategories: ReturnType<typeof vi.fn> };
  let subcategoriesApiMock: { getProperties: ReturnType<typeof vi.fn> };

  const categories: Category[] = [
    { id: 'K1', name: 'Elektronik', subcategories: [{ id: 'K1a', name: 'Kopfhörer' }] },
  ];

  beforeEach(async () => {
    productsApiMock = {
      getProducts: vi.fn(),
    };
    categoriesApiMock = {
      getCategories: vi.fn().mockReturnValue(of(categories)),
    };
    subcategoriesApiMock = {
      getProperties: vi.fn().mockReturnValue(of([])),
    };

    await TestBed.configureTestingModule({
      imports: [Catalog],
      providers: [
        provideRouter([]),
        { provide: ProductsApi, useValue: productsApiMock },
        { provide: CategoriesApi, useValue: categoriesApiMock },
        { provide: SubcategoriesApi, useValue: subcategoriesApiMock },
      ],
    }).compileComponents();
  });

  it('renders the products delivered by the API service', () => {
    const items: ProductListItem[] = [
      { id: 'P1', name: 'Ohrhörer Modell Compact', lowestPrice: 27.5 },
      { id: 'P2', name: 'Bügelkopfhörer Studio', lowestPrice: 79.0 },
    ];
    productsApiMock.getProducts.mockReturnValue(of(pageResult(1, items)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const names = Array.from(compiled.querySelectorAll('.product-list__name')).map((el) => el.textContent);

    expect(productsApiMock.getProducts).toHaveBeenCalledWith(1, undefined, undefined, undefined, undefined, undefined, undefined);
    expect(names).toEqual(['Ohrhörer Modell Compact', 'Bügelkopfhörer Studio']);
  });

  it('renders the product name as a link pointing to the product detail route', () => {
    const items: ProductListItem[] = [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }];
    productsApiMock.getProducts.mockReturnValue(of(pageResult(1, items)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector<HTMLAnchorElement>('.product-list__name')!;

    expect(link.tagName).toBe('A');
    expect(link.getAttribute('href')).toBe('/products/P1');
  });

  it('requests the next page when the pagination control is clicked', () => {
    const page1Items: ProductListItem[] = [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }];
    const page2Items: ProductListItem[] = [{ id: 'P21', name: 'Produkt 21', lowestPrice: 20 }];
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, page1Items)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(2, page2Items)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const nextButton = Array.from(compiled.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Weiter')!;
    nextButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenCalledWith(2, undefined, undefined, undefined, undefined, undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 21');
  });

  it('reloads page 1 filtered by categoryId when a category is selected in the navigation', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const categoryButton = compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!;
    categoryButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenCalledWith(1, 'K1', undefined, undefined, undefined, undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 2a');
  });

  it('reloads page 1 filtered by subcategoryId when a subcategory is selected in the navigation', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P1a', name: 'Produkt 1a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const subcategoryButton = compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!;
    subcategoryButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenCalledWith(1, 'K1', 'K1a', undefined, undefined, undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 1a');
  });

  it('resets the filter and reloads unfiltered products when "Alle Kategorien" is clicked', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!.click();
    fixture.detectChanges();

    compiled.querySelector<HTMLButtonElement>('.category-nav__all')!.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, undefined, undefined, undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 1');
  });

  it('keeps the active category filter when navigating to the next page', () => {
    const filteredPage1: ProductListItem[] = [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }];
    const filteredPage2: ProductListItem[] = [{ id: 'P2B', name: 'Produkt 2b', lowestPrice: 6 }];
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, filteredPage1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(2, filteredPage2)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!.click();
    fixture.detectChanges();

    const nextButton = Array.from(compiled.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Weiter')!;
    nextButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(2, 'K1', undefined, undefined, undefined, undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 2b');
  });

  it('reloads page 1 with the selected sort parameters when a sort option is chosen', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2', name: 'Produkt 2', lowestPrice: 3 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('select')!;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, 'price', 'asc', undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 2');
  });

  it('reloads page 1 sorted by view count when "Beliebtheit" is chosen', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2', name: 'Produkt 2', lowestPrice: 3 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('select')!;
    select.value = 'viewCount-desc';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, 'viewCount', 'desc', undefined, undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt 2');
  });

  it('keeps the active category filter when a sort option is chosen', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!.click();
    fixture.detectChanges();

    const select = compiled.querySelector<HTMLSelectElement>('select')!;
    select.value = 'name-desc';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, 'K1', undefined, 'name', 'desc', undefined, undefined);
  });

  it('keeps the active sort selection when a category is chosen afterwards', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2', name: 'Produkt 2', lowestPrice: 3 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('select')!;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, 'K1', undefined, 'price', 'asc', undefined, undefined);
  });

  it('reloads page 1 with the search term when a search is submitted', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2', name: 'Kopfhörer', lowestPrice: 3 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector<HTMLInputElement>('input[type="search"]')!;
    searchInput.value = 'Kopfhörer';
    const searchButton = Array.from(compiled.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Suchen')!;
    searchButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, undefined, undefined, 'Kopfhörer', undefined);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Kopfhörer');
  });

  it('keeps the active category filter and sort selection when a search is submitted', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P2A', name: 'Produkt 2a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!.click();
    fixture.detectChanges();

    const select = compiled.querySelector<HTMLSelectElement>('select')!;
    select.value = 'name-desc';
    select.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const searchInput = compiled.querySelector<HTMLInputElement>('input[type="search"]')!;
    searchInput.value = 'Produkt';
    const searchButton = Array.from(compiled.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Suchen')!;
    searchButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, 'K1', undefined, 'name', 'desc', 'Produkt', undefined);
  });

  it('sends q as undefined instead of an empty string when the search field is empty or whitespace-only', () => {
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector<HTMLInputElement>('input[type="search"]')!;
    searchInput.value = '   ';
    const searchButton = Array.from(compiled.querySelectorAll('button')).find((b) => b.textContent?.trim() === 'Suchen')!;
    searchButton.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, undefined, undefined, undefined, undefined);
  });

  it('selecting a property value filters the product list and resets to page 1', () => {
    subcategoriesApiMock.getProperties.mockReturnValue(of([{ name: 'Farbe', values: ['Blau', 'Rot'] }]));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'P1a', name: 'Produkt 1a', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );
    productsApiMock.getProducts.mockReturnValueOnce(
      of({ items: [{ id: 'PR', name: 'Produkt Rot', lowestPrice: 5 }], totalCount: 1, pageCount: 1, page: 1, pageSize: 20 }),
    );

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!.click();
    fixture.detectChanges();

    const checkbox = compiled.querySelector<HTMLInputElement>('.property-filter input[type="checkbox"]')!;
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, 'K1', 'K1a', undefined, undefined, undefined, ['Farbe:Blau']);
    expect(compiled.querySelector('.product-list__name')?.textContent).toBe('Produkt Rot');
  });

  it('selecting two values of the same property combines them in the property filter', () => {
    subcategoriesApiMock.getProperties.mockReturnValue(of([{ name: 'Farbe', values: ['Blau', 'Rot'] }]));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1a', name: 'Produkt 1a', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'PB', name: 'Produkt Blau', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'PBR', name: 'Produkt Blau/Rot', lowestPrice: 5 }], 1)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!.click();
    fixture.detectChanges();

    const checkboxes = compiled.querySelectorAll<HTMLInputElement>('.property-filter input[type="checkbox"]');
    checkboxes[0].checked = true;
    checkboxes[0].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    checkboxes[1].checked = true;
    checkboxes[1].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(
      1,
      'K1',
      'K1a',
      undefined,
      undefined,
      undefined,
      ['Farbe:Blau', 'Farbe:Rot'],
    );
  });

  it('selecting values of two different properties includes both entries in the property filter', () => {
    subcategoriesApiMock.getProperties.mockReturnValue(
      of([
        { name: 'Farbe', values: ['Rot'] },
        { name: 'Material', values: ['Holz'] },
      ]),
    );
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1a', name: 'Produkt 1a', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'PR', name: 'Produkt Rot', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'PRH', name: 'Produkt Rot/Holz', lowestPrice: 5 }], 1)));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!.click();
    fixture.detectChanges();

    const checkboxes = compiled.querySelectorAll<HTMLInputElement>('.property-filter input[type="checkbox"]');
    checkboxes[0].checked = true;
    checkboxes[0].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    checkboxes[1].checked = true;
    checkboxes[1].dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(
      1,
      'K1',
      'K1a',
      undefined,
      undefined,
      undefined,
      ['Farbe:Rot', 'Material:Holz'],
    );
  });

  it('resets the property filter when the category/subcategory selection changes afterwards', () => {
    subcategoriesApiMock.getProperties.mockReturnValue(of([{ name: 'Farbe', values: ['Rot'] }]));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1a', name: 'Produkt 1a', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'PR', name: 'Produkt Rot', lowestPrice: 5 }], 1)));
    productsApiMock.getProducts.mockReturnValueOnce(of(pageResult(1, [{ id: 'P1', name: 'Produkt 1', lowestPrice: 10 }])));

    const fixture = TestBed.createComponent(Catalog);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!.click();
    fixture.detectChanges();

    const checkbox = compiled.querySelector<HTMLInputElement>('.property-filter input[type="checkbox"]')!;
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    compiled.querySelector<HTMLButtonElement>('.category-nav__all')!.click();
    fixture.detectChanges();

    expect(productsApiMock.getProducts).toHaveBeenLastCalledWith(1, undefined, undefined, undefined, undefined, undefined, undefined);
  });
});
