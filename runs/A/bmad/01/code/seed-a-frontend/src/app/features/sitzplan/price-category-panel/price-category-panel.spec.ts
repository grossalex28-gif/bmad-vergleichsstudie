import { TestBed } from '@angular/core/testing';

import { PriceCategoryPanel } from './price-category-panel';
import { PriceCategory } from '../../../core/models/seat-map.model';

describe('PriceCategoryPanel', () => {
  const categories: PriceCategory[] = [
    { id: 'cat-a', name: 'Kategorie A', price: 32 },
    { id: 'cat-b', name: 'Kategorie B', price: 22 }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [PriceCategoryPanel]
    }).compileComponents();
  });

  it('renders one pill per category with the name and formatted price', () => {
    const fixture = TestBed.createComponent(PriceCategoryPanel);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const pills = Array.from(compiled.querySelectorAll('.price-category-panel__pill'));
    expect(pills.map((pill) => pill.textContent?.trim())).toEqual(['Kategorie A · 32,00 €', 'Kategorie B · 22,00 €']);
  });

  it('marks the pill matching selectedCategoryId as active with aria-pressed true and all others false', () => {
    const fixture = TestBed.createComponent(PriceCategoryPanel);
    fixture.componentRef.setInput('categories', categories);
    fixture.componentRef.setInput('selectedCategoryId', 'cat-b');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const pills = Array.from(compiled.querySelectorAll('.price-category-panel__pill'));

    expect(pills[0].classList.contains('price-category-panel__pill--active')).toBe(false);
    expect(pills[0].getAttribute('aria-pressed')).toBe('false');
    expect(pills[1].classList.contains('price-category-panel__pill--active')).toBe(true);
    expect(pills[1].getAttribute('aria-pressed')).toBe('true');
  });

  it('emits categorySelected with the clicked category id', () => {
    const fixture = TestBed.createComponent(PriceCategoryPanel);
    fixture.componentRef.setInput('categories', categories);
    fixture.detectChanges();

    const onCategorySelected = vi.fn();
    fixture.componentInstance.categorySelected.subscribe(onCategorySelected);

    const compiled = fixture.nativeElement as HTMLElement;
    const pills = compiled.querySelectorAll('.price-category-panel__pill');
    (pills[1] as HTMLElement).click();

    expect(onCategorySelected).toHaveBeenCalledWith('cat-b');
  });
});
