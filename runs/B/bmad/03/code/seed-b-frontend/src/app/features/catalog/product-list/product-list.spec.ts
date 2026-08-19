import { By } from '@angular/platform-browser';
import { TestBed } from '@angular/core/testing';
import { provideRouter, RouterLink } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';

import { CategoryNavItem } from '../../../core/models/category.model';
import { PagedResult, ProductListItem } from '../../../core/models/product.model';
import { CategoryService } from '../../../core/services/category.service';
import { ProductService } from '../../../core/services/product.service';
import { CategoryNav } from '../category-nav/category-nav';
import { ProductList } from './product-list';

describe('ProductList', () => {
  const pageOneResult: PagedResult<ProductListItem> = {
    items: [{ id: 'P1', name: 'Produkt 1', subcategoryName: 'Sub 1', minPrice: 9.99 }],
    page: 1,
    pageSize: 20,
    totalCount: 21,
    totalPages: 2
  };

  const pageTwoResult: PagedResult<ProductListItem> = {
    items: [{ id: 'P21', name: 'Produkt 21', subcategoryName: 'Sub 1', minPrice: 4.5 }],
    page: 2,
    pageSize: 20,
    totalCount: 21,
    totalPages: 2
  };

  const noCategories: CategoryNavItem[] = [];

  function findDirectionSelect(compiled: HTMLElement): HTMLSelectElement | null {
    const directionLabel = Array.from(compiled.querySelectorAll('label')).find((label) =>
      label.textContent?.includes('Richtung')
    );
    return (directionLabel?.querySelector('select') as HTMLSelectElement | undefined) ?? null;
  }

  let getProducts: ReturnType<typeof vi.fn>;
  let getCategories: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getProducts = vi.fn((page: number) => of(page === 2 ? pageTwoResult : pageOneResult));
    getCategories = vi.fn(() => of(noCategories));

    await TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    }).compileComponents();
  });

  it('renders the items returned by the service on init', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'asc', '', []);
    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Produkt 1');
  });

  it('renders the product name as a link to /products/:id', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const linkDebugElement = fixture.debugElement.query(By.directive(RouterLink));
    const routerLink = linkDebugElement.injector.get(RouterLink);

    expect(routerLink.href).toBe('/products/P1');
  });

  it('disables the "Vorherige" button on page 1', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(compiled.querySelectorAll('button'));
    const prevButton = buttons.find((b) => b.textContent?.includes('Vorherige')) as HTMLButtonElement;

    expect(prevButton.disabled).toBe(true);
  });

  it('calls getProducts(2) when "Nächste" is clicked', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = Array.from(compiled.querySelectorAll('button'));
    const nextButton = buttons.find((b) => b.textContent?.includes('Nächste')) as HTMLButtonElement;
    nextButton.click();
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(2, null, null, 'name', 'asc', '', []);
  });

  it('disables the "Nächste" button on the last page', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const nextButtonOnPageOne = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Nächste')
    ) as HTMLButtonElement;
    nextButtonOnPageOne.click();
    fixture.detectChanges();

    const nextButtonOnPageTwo = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Nächste')
    ) as HTMLButtonElement;

    expect(nextButtonOnPageTwo.disabled).toBe(true);
  });

  it('shows an error message and does not apply a stale response when the request fails', () => {
    getProducts = vi.fn(() => throwError(() => new Error('network error')));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        expect(compiled.textContent).toContain('Die Produktliste konnte nicht geladen werden.');
      });
  });

  it('reloads page 1 with the new category/subcategory when CategoryNav emits a selectionChange', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    // Simulate having navigated to page 2 before changing the category.
    fixture.componentInstance.loadPage(2);
    fixture.detectChanges();
    getProducts.mockClear();

    const categoryNavDebugElement = fixture.debugElement.query(By.directive(CategoryNav));
    const categoryNav = categoryNavDebugElement.componentInstance as CategoryNav;
    categoryNav.selectionChange.emit({ categoryId: 'K1', subcategoryId: 'K1a' });
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, 'K1', 'K1a', 'name', 'asc', '', []);
  });

  it('ignores a stale page-1 response from an earlier category selection that resolves after a later one', () => {
    const resultForFirstSelection: PagedResult<ProductListItem> = {
      items: [{ id: 'PK1', name: 'Produkt K1', subcategoryName: 'Sub K1', minPrice: 1 }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    };
    const resultForSecondSelection: PagedResult<ProductListItem> = {
      items: [{ id: 'PK2', name: 'Produkt K2', subcategoryName: 'Sub K2', minPrice: 2 }],
      page: 1,
      pageSize: 20,
      totalCount: 1,
      totalPages: 1
    };
    const firstSelectionResponse = new Subject<PagedResult<ProductListItem>>();
    const secondSelectionResponse = new Subject<PagedResult<ProductListItem>>();
    const responses = [firstSelectionResponse, secondSelectionResponse];
    getProducts = vi.fn(() => of(pageOneResult));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();
        getProducts.mockImplementation(() => responses.shift()!.asObservable());

        const categoryNavDebugElement = fixture.debugElement.query(By.directive(CategoryNav));
        const categoryNav = categoryNavDebugElement.componentInstance as CategoryNav;

        // Both selections resolve to a page-1 request; the responses arrive out of order,
        // i.e. the first selection's response resolves only after the second one's.
        categoryNav.selectionChange.emit({ categoryId: 'K1', subcategoryId: null });
        categoryNav.selectionChange.emit({ categoryId: 'K2', subcategoryId: null });

        secondSelectionResponse.next(resultForSecondSelection);
        firstSelectionResponse.next(resultForFirstSelection);

        expect(fixture.componentInstance.result()).toEqual(resultForSecondSelection);
      });
  });

  it('selecting "Preis" in the sort-by select triggers getProducts with sortBy price, unchanged direction and page 1', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    // Simulate having navigated to page 2 before changing the sort field.
    fixture.componentInstance.loadPage(2);
    fixture.detectChanges();
    getProducts.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const selects = Array.from(compiled.querySelectorAll('select'));
    const sortBySelect = selects[0] as HTMLSelectElement;
    sortBySelect.value = 'price';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'price', 'asc', '', []);
  });

  it('selecting "Beliebtheit" in the sort-by select triggers getProducts with sortBy views, unchanged direction and page 1', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    // Simulate having navigated to page 2 before changing the sort field.
    fixture.componentInstance.loadPage(2);
    fixture.detectChanges();
    getProducts.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const selects = Array.from(compiled.querySelectorAll('select'));
    const sortBySelect = selects[0] as HTMLSelectElement;
    sortBySelect.value = 'views';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'views', 'asc', '', []);
  });

  it('hides the direction select while sortBy is views', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(findDirectionSelect(compiled)).not.toBeNull();

    const sortBySelect = compiled.querySelectorAll('select')[0] as HTMLSelectElement;
    sortBySelect.value = 'views';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(findDirectionSelect(compiled)).toBeNull();
  });

  it('shows the direction select again after switching back from views to name', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const sortBySelect = compiled.querySelectorAll('select')[0] as HTMLSelectElement;
    sortBySelect.value = 'views';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    expect(findDirectionSelect(compiled)).toBeNull();

    sortBySelect.value = 'name';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(findDirectionSelect(compiled)).not.toBeNull();
  });

  it('selecting "Absteigend" in the direction select triggers getProducts with sortDirection desc and page 1', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    getProducts.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const selects = Array.from(compiled.querySelectorAll('select'));
    const directionSelect = selects[1] as HTMLSelectElement;
    directionSelect.value = 'desc';
    directionSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'desc', '', []);
  });

  it('uses sortBy name and sortDirection asc on the initial load', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'asc', '', []);
  });

  it('entering a search term and submitting the form triggers getProducts with that search term and page 1', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    // Simulate having navigated to page 2 before searching.
    fixture.componentInstance.loadPage(2);
    fixture.detectChanges();
    getProducts.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector('input[type="text"]') as HTMLInputElement;
    searchInput.value = 'kabellos';
    searchInput.dispatchEvent(new Event('input'));
    const form = compiled.querySelector('form.product-list__search') as HTMLFormElement;
    form.dispatchEvent(new Event('submit', { cancelable: true }));
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'asc', 'kabellos', []);
  });

  it('typing in the search field without submitting does not trigger getProducts', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();
    getProducts.mockClear();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector('input[type="text"]') as HTMLInputElement;
    searchInput.value = 'kabellos';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(getProducts).not.toHaveBeenCalled();
  });

  it('changing the sort field does not apply a search term that was typed but never submitted', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const searchInput = compiled.querySelector('input[type="text"]') as HTMLInputElement;
    searchInput.value = 'kabellos';
    searchInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();
    getProducts.mockClear();

    const selects = Array.from(compiled.querySelectorAll('select'));
    const sortBySelect = selects[0] as HTMLSelectElement;
    sortBySelect.value = 'price';
    sortBySelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(getProducts).toHaveBeenCalledWith(1, null, null, 'price', 'asc', '', []);
  });

  it('renders a select per attribute when the response includes availableAttributeFilters', () => {
    const resultWithFilters: PagedResult<ProductListItem> = {
      ...pageOneResult,
      availableAttributeFilters: [
        { name: 'Bauform', values: ['In-Ear', 'Over-Ear'] },
        { name: 'Kabellos', values: ['true', 'false'] }
      ]
    };
    getProducts = vi.fn(() => of(resultWithFilters));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const filterContainer = compiled.querySelector('.product-list__attribute-filters');
        expect(filterContainer).not.toBeNull();
        const options = Array.from(filterContainer!.querySelectorAll('option')).map((o) => o.textContent);
        expect(options).toEqual(['Alle', 'In-Ear', 'Over-Ear', 'Alle', 'true', 'false']);
      });
  });

  it('renders no attribute-filters element when availableAttributeFilters is absent or empty', () => {
    const fixture = TestBed.createComponent(ProductList);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.product-list__attribute-filters')).toBeNull();
  });

  it('selecting a value in an attribute select triggers getProducts with that filter and page 1', () => {
    const resultWithFilters: PagedResult<ProductListItem> = {
      ...pageOneResult,
      availableAttributeFilters: [{ name: 'Bauform', values: ['In-Ear', 'Over-Ear'] }]
    };
    getProducts = vi.fn(() => of(resultWithFilters));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();

        // Simulate having navigated to page 2 before applying the attribute filter.
        fixture.componentInstance.loadPage(2);
        fixture.detectChanges();
        getProducts.mockClear();

        const compiled = fixture.nativeElement as HTMLElement;
        const attributeSelect = compiled.querySelector(
          '.product-list__attribute-filters select'
        ) as HTMLSelectElement;
        attributeSelect.value = 'In-Ear';
        attributeSelect.dispatchEvent(new Event('change'));
        fixture.detectChanges();

        expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'asc', '', [
          { name: 'Bauform', value: 'In-Ear' }
        ]);
      });
  });

  it('selecting values in two different attribute selects accumulates both filters (AND) in the last call', () => {
    const resultWithFilters: PagedResult<ProductListItem> = {
      ...pageOneResult,
      availableAttributeFilters: [
        { name: 'Bauform', values: ['In-Ear', 'Over-Ear'] },
        { name: 'Kabellos', values: ['true', 'false'] }
      ]
    };
    getProducts = vi.fn(() => of(resultWithFilters));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();
        getProducts.mockClear();

        const compiled = fixture.nativeElement as HTMLElement;
        const attributeSelects = Array.from(
          compiled.querySelectorAll('.product-list__attribute-filters select')
        ) as HTMLSelectElement[];

        attributeSelects[0].value = 'In-Ear';
        attributeSelects[0].dispatchEvent(new Event('change'));
        fixture.detectChanges();

        attributeSelects[1].value = 'true';
        attributeSelects[1].dispatchEvent(new Event('change'));
        fixture.detectChanges();

        expect(getProducts).toHaveBeenLastCalledWith(1, null, null, 'name', 'asc', '', [
          { name: 'Bauform', value: 'In-Ear' },
          { name: 'Kabellos', value: 'true' }
        ]);
      });
  });

  it('resetting an attribute select to "Alle" removes only that filter, a second filter remains', () => {
    const resultWithFilters: PagedResult<ProductListItem> = {
      ...pageOneResult,
      availableAttributeFilters: [
        { name: 'Bauform', values: ['In-Ear', 'Over-Ear'] },
        { name: 'Kabellos', values: ['true', 'false'] }
      ]
    };
    getProducts = vi.fn(() => of(resultWithFilters));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const attributeSelects = Array.from(
          compiled.querySelectorAll('.product-list__attribute-filters select')
        ) as HTMLSelectElement[];

        attributeSelects[0].value = 'In-Ear';
        attributeSelects[0].dispatchEvent(new Event('change'));
        fixture.detectChanges();

        attributeSelects[1].value = 'true';
        attributeSelects[1].dispatchEvent(new Event('change'));
        fixture.detectChanges();

        getProducts.mockClear();

        attributeSelects[0].value = '';
        attributeSelects[0].dispatchEvent(new Event('change'));
        fixture.detectChanges();

        expect(getProducts).toHaveBeenCalledWith(1, null, null, 'name', 'asc', '', [
          { name: 'Kabellos', value: 'true' }
        ]);
      });
  });

  it('a category/subcategory selection after a previously set attribute filter triggers getProducts with an empty seventh argument', () => {
    const resultWithFilters: PagedResult<ProductListItem> = {
      ...pageOneResult,
      availableAttributeFilters: [{ name: 'Bauform', values: ['In-Ear', 'Over-Ear'] }]
    };
    getProducts = vi.fn(() => of(resultWithFilters));

    return TestBed.configureTestingModule({
      imports: [ProductList],
      providers: [
        provideRouter([]),
        { provide: ProductService, useValue: { getProducts } },
        { provide: CategoryService, useValue: { getCategories } }
      ]
    })
      .compileComponents()
      .then(() => {
        const fixture = TestBed.createComponent(ProductList);
        fixture.detectChanges();

        const compiled = fixture.nativeElement as HTMLElement;
        const attributeSelect = compiled.querySelector(
          '.product-list__attribute-filters select'
        ) as HTMLSelectElement;
        attributeSelect.value = 'In-Ear';
        attributeSelect.dispatchEvent(new Event('change'));
        fixture.detectChanges();
        getProducts.mockClear();

        const categoryNavDebugElement = fixture.debugElement.query(By.directive(CategoryNav));
        const categoryNav = categoryNavDebugElement.componentInstance as CategoryNav;
        categoryNav.selectionChange.emit({ categoryId: 'K1', subcategoryId: 'K1a' });
        fixture.detectChanges();

        expect(getProducts).toHaveBeenCalledWith(1, 'K1', 'K1a', 'name', 'asc', '', []);
      });
  });
});
