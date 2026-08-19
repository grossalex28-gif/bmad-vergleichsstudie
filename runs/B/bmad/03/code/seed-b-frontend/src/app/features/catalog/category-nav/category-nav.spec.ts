import { TestBed } from '@angular/core/testing';
import { of, throwError } from 'rxjs';

import { CategoryNavItem, CategorySelection } from '../../../core/models/category.model';
import { CategoryService } from '../../../core/services/category.service';
import { CategoryNav } from './category-nav';

describe('CategoryNav', () => {
  const categories: CategoryNavItem[] = [
    {
      id: 'K1',
      name: 'Kategorie 1',
      subcategories: [
        { id: 'K1a', name: 'Unterkategorie 1a' },
        { id: 'K1b', name: 'Unterkategorie 1b' }
      ]
    },
    {
      id: 'K2',
      name: 'Kategorie 2',
      subcategories: [{ id: 'K2a', name: 'Unterkategorie 2a' }]
    }
  ];

  let getCategories: ReturnType<typeof vi.fn>;

  beforeEach(async () => {
    getCategories = vi.fn(() => of(categories));

    await TestBed.configureTestingModule({
      imports: [CategoryNav],
      providers: [{ provide: CategoryService, useValue: { getCategories } }]
    }).compileComponents();
  });

  it('renders categories with their nested subcategories from the service response', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Kategorie 1');
    expect(compiled.textContent).toContain('Unterkategorie 1a');
    expect(compiled.textContent).toContain('Unterkategorie 1b');
    expect(compiled.textContent).toContain('Kategorie 2');
    expect(compiled.textContent).toContain('Unterkategorie 2a');
  });

  it('emits { categoryId, subcategoryId } when a subcategory is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    let emitted: CategorySelection | undefined;
    fixture.componentInstance.selectionChange.subscribe((selection) => (emitted = selection));

    const compiled = fixture.nativeElement as HTMLElement;
    const button = Array.from(compiled.querySelectorAll('button')).find((b) =>
      b.textContent?.includes('Unterkategorie 1a')
    ) as HTMLButtonElement;
    button.click();

    expect(emitted).toEqual({ categoryId: 'K1', subcategoryId: 'K1a' });
  });

  it('emits { categoryId, subcategoryId: null } when a category is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    let emitted: CategorySelection | undefined;
    fixture.componentInstance.selectionChange.subscribe((selection) => (emitted = selection));

    const compiled = fixture.nativeElement as HTMLElement;
    const button = Array.from(compiled.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Kategorie 1'
    ) as HTMLButtonElement;
    button.click();

    expect(emitted).toEqual({ categoryId: 'K1', subcategoryId: null });
  });

  it('emits { categoryId: null, subcategoryId: null } when "Alle" is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    let emitted: CategorySelection | undefined;
    fixture.componentInstance.selectionChange.subscribe((selection) => (emitted = selection));

    const compiled = fixture.nativeElement as HTMLElement;
    const button = Array.from(compiled.querySelectorAll('button')).find(
      (b) => b.textContent?.trim() === 'Alle'
    ) as HTMLButtonElement;
    button.click();

    expect(emitted).toEqual({ categoryId: null, subcategoryId: null });
  });

  it('shows an error message when loading categories fails', async () => {
    getCategories.mockReturnValue(throwError(() => new Error('network error')));

    await TestBed.configureTestingModule({
      imports: [CategoryNav],
      providers: [{ provide: CategoryService, useValue: { getCategories } }]
    }).compileComponents();

    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Die Kategorien konnten nicht geladen werden.');
  });
});
