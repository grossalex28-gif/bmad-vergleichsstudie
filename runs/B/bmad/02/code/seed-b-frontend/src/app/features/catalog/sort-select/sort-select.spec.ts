import { TestBed } from '@angular/core/testing';

import { SortSelect, SortSelection } from './sort-select';

describe('SortSelect', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SortSelect]
    });
  });

  it('renders all six sort option labels as text', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent ?? '';
    expect(text).toContain('Preis aufsteigend');
    expect(text).toContain('Preis absteigend');
    expect(text).toContain('Name aufsteigend');
    expect(text).toContain('Name absteigend');
    expect(text).toContain('Beliebtheit aufsteigend');
    expect(text).toContain('Beliebtheit absteigend');
  });

  it('emits sortChanged with { sortBy: "price", sortDirection: "asc" } when "price-asc" is selected', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    let emitted: SortSelection | null | undefined;
    fixture.componentInstance.sortChanged.subscribe((value) => (emitted = value));

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'price-asc';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual({ sortBy: 'price', sortDirection: 'asc' });
  });

  it('emits sortChanged with { sortBy: "name", sortDirection: "desc" } when "name-desc" is selected', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    let emitted: SortSelection | null | undefined;
    fixture.componentInstance.sortChanged.subscribe((value) => (emitted = value));

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'name-desc';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual({ sortBy: 'name', sortDirection: 'desc' });
  });

  it('emits sortChanged with { sortBy: "viewCount", sortDirection: "asc" } when "viewCount-asc" is selected', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    let emitted: SortSelection | null | undefined;
    fixture.componentInstance.sortChanged.subscribe((value) => (emitted = value));

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'viewCount-asc';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual({ sortBy: 'viewCount', sortDirection: 'asc' });
  });

  it('emits sortChanged with { sortBy: "viewCount", sortDirection: "desc" } when "viewCount-desc" is selected', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    let emitted: SortSelection | null | undefined;
    fixture.componentInstance.sortChanged.subscribe((value) => (emitted = value));

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = 'viewCount-desc';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toEqual({ sortBy: 'viewCount', sortDirection: 'desc' });
  });

  it('disables the select when disabled is set', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    expect(select.disabled).toBe(true);
  });

  it('emits sortChanged with null when the placeholder option is selected again', () => {
    const fixture = TestBed.createComponent(SortSelect);
    fixture.detectChanges();

    let emitted: SortSelection | null | undefined = { sortBy: 'price', sortDirection: 'asc' };
    fixture.componentInstance.sortChanged.subscribe((value) => (emitted = value));

    const select = (fixture.nativeElement as HTMLElement).querySelector('select') as HTMLSelectElement;
    select.value = '';
    select.dispatchEvent(new Event('change'));

    expect(emitted).toBeNull();
  });
});
