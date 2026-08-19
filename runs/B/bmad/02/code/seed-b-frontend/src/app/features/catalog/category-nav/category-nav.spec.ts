import { TestBed } from '@angular/core/testing';

import { CategorySummary } from '../product.model';
import { CategoryNav } from './category-nav';

const categories: CategorySummary[] = [
  {
    id: 'K1',
    name: 'Elektronik',
    subcategories: [
      { id: 'K1a', name: 'Smartphones', properties: [] },
      { id: 'K1b', name: 'Laptops', properties: [] }
    ]
  },
  {
    id: 'K2',
    name: 'Haushalt',
    subcategories: [{ id: 'K2a', name: 'Küche', properties: [] }]
  }
];

describe('CategoryNav', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [CategoryNav]
    });
  });

  it('renders category and subcategory names as text', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Elektronik');
    expect(text).toContain('Smartphones');
    expect(text).toContain('Laptops');
    expect(text).toContain('Haushalt');
    expect(text).toContain('Küche');
  });

  it('emits categorySelected with the correct id when a category button is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    let emitted: string | undefined;
    fixture.componentInstance.categorySelected.subscribe((id) => (emitted = id));

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const categoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Elektronik')) as HTMLButtonElement;
    categoryButton.click();

    expect(emitted).toBe('K1');
  });

  it('emits subcategorySelected with the correct id when a subcategory button is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    let emitted: string | undefined;
    fixture.componentInstance.subcategorySelected.subscribe((id) => (emitted = id));

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const subcategoryButton = Array.from(buttons).find((b) => b.textContent?.includes('Smartphones')) as HTMLButtonElement;
    subcategoryButton.click();

    expect(emitted).toBe('K1a');
  });

  it('emits selectionCleared when "Alle Kategorien" is clicked', () => {
    const fixture = TestBed.createComponent(CategoryNav);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.selectionCleared.subscribe(() => (emitted = true));

    const buttons = (fixture.nativeElement as HTMLElement).querySelectorAll('button');
    const clearButton = Array.from(buttons).find((b) => b.textContent?.includes('Alle Kategorien')) as HTMLButtonElement;
    clearButton.click();

    expect(emitted).toBe(true);
  });
});
