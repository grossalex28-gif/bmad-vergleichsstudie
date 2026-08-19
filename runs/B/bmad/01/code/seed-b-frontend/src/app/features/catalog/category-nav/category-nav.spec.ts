import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { vi } from 'vitest';

import { Category, CategoriesApi } from '../../../core/api/categories.api';
import { CategoryNav } from './category-nav';

describe('CategoryNav', () => {
  let categoriesApiMock: { getCategories: ReturnType<typeof vi.fn> };

  const categories: Category[] = [
    {
      id: 'K1',
      name: 'Elektronik',
      subcategories: [
        { id: 'K1a', name: 'Kopfhörer' },
        { id: 'K1b', name: 'Smartphones & Zubehör' },
      ],
    },
    {
      id: 'K2',
      name: 'Haushalt',
      subcategories: [{ id: 'K2a', name: 'Küchengeräte' }],
    },
  ];

  beforeEach(async () => {
    categoriesApiMock = { getCategories: vi.fn() };
    categoriesApiMock.getCategories.mockReturnValue(of(categories));

    await TestBed.configureTestingModule({
      imports: [CategoryNav],
      providers: [{ provide: CategoriesApi, useValue: categoriesApiMock }],
    }).compileComponents();
  });

  it('renders categories with their subcategories', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const categoryNames = Array.from(compiled.querySelectorAll('.category-nav__category-name')).map(
      (el) => el.textContent?.trim(),
    );
    const subcategoryNames = Array.from(compiled.querySelectorAll('.category-nav__subcategory-name')).map(
      (el) => el.textContent?.trim(),
    );

    expect(categoryNames).toEqual(['Elektronik', 'Haushalt']);
    expect(subcategoryNames).toEqual(['Kopfhörer', 'Smartphones & Zubehör', 'Küchengeräte']);
  });

  it('emits categoryId only when a category is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    const emitted: unknown[] = [];
    fixture.componentInstance.selectionChange.subscribe((value: unknown) => emitted.push(value));

    const compiled = fixture.nativeElement as HTMLElement;
    const categoryButton = compiled.querySelector<HTMLButtonElement>('.category-nav__category-name')!;
    categoryButton.click();

    expect(emitted).toEqual([{ categoryId: 'K1', subcategoryId: null }]);
  });

  it('emits categoryId and subcategoryId when a subcategory is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.detectChanges();

    const emitted: unknown[] = [];
    fixture.componentInstance.selectionChange.subscribe((value: unknown) => emitted.push(value));

    const compiled = fixture.nativeElement as HTMLElement;
    const subcategoryButton = compiled.querySelector<HTMLButtonElement>('.category-nav__subcategory-name')!;
    subcategoryButton.click();

    expect(emitted).toEqual([{ categoryId: 'K1', subcategoryId: 'K1a' }]);
  });

  it('marks the currently active selection', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('selection', { categoryId: 'K1', subcategoryId: 'K1a' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const activeSubcategory = compiled.querySelector('.category-nav__subcategory--active');
    expect(activeSubcategory?.textContent?.trim()).toBe('Kopfhörer');
  });

  it('disables all selection buttons while the disabled input is true', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const buttons = compiled.querySelectorAll<HTMLButtonElement>(
      '.category-nav__all, .category-nav__category-name, .category-nav__subcategory-name',
    );

    expect(buttons.length).toBeGreaterThan(0);
    buttons.forEach((button) => expect(button.disabled).toBe(true));
  });
});
