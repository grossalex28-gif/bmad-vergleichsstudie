import { TestBed } from '@angular/core/testing';
import { of, Subject, throwError } from 'rxjs';
import { vi } from 'vitest';

import { PropertyFilterOption, SubcategoriesApi } from '../../../core/api/subcategories.api';
import { PropertyFilter } from './property-filter';

describe('PropertyFilter', () => {
  let subcategoriesApiMock: { getProperties: ReturnType<typeof vi.fn> };

  const options: PropertyFilterOption[] = [
    { name: 'Farbe', values: ['Blau', 'Rot'] },
    { name: 'Material', values: ['Holz', 'Metall'] },
  ];

  beforeEach(async () => {
    subcategoriesApiMock = { getProperties: vi.fn().mockReturnValue(of(options)) };

    await TestBed.configureTestingModule({
      imports: [PropertyFilter],
      providers: [{ provide: SubcategoriesApi, useValue: subcategoriesApiMock }],
    }).compileComponents();
  });

  it('renders the options delivered by the API as checkboxes per property/value', () => {
    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const legends = Array.from(compiled.querySelectorAll('legend')).map((el) => el.textContent);
    const checkboxes = compiled.querySelectorAll('input[type="checkbox"]');

    expect(subcategoriesApiMock.getProperties).toHaveBeenCalledWith('K1a');
    expect(legends).toEqual(['Farbe', 'Material']);
    expect(checkboxes.length).toBe(4);
  });

  it('calls getProperties again when subcategoryId changes', () => {
    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.detectChanges();

    fixture.componentRef.setInput('subcategoryId', 'K1b');
    fixture.detectChanges();

    expect(subcategoriesApiMock.getProperties).toHaveBeenCalledTimes(2);
    expect(subcategoriesApiMock.getProperties).toHaveBeenLastCalledWith('K1b');
  });

  it('does not call getProperties and shows no options when subcategoryId is null', () => {
    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;

    expect(subcategoriesApiMock.getProperties).not.toHaveBeenCalled();
    expect(compiled.querySelectorAll('input[type="checkbox"]').length).toBe(0);
  });

  it('emits selectionChange with the value added when a checkbox is checked', () => {
    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.detectChanges();

    const emitted: unknown[] = [];
    fixture.componentInstance.selectionChange.subscribe((value: unknown) => emitted.push(value));

    const compiled = fixture.nativeElement as HTMLElement;
    const checkbox = compiled.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    checkbox.checked = true;
    checkbox.dispatchEvent(new Event('change'));

    expect(emitted).toEqual([{ Farbe: ['Blau'] }]);
  });

  it('emits a selection map without the property key when the last checkbox of a property is unchecked', () => {
    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.componentRef.setInput('selection', { Farbe: ['Blau'] });
    fixture.detectChanges();

    const emitted: unknown[] = [];
    fixture.componentInstance.selectionChange.subscribe((value: unknown) => emitted.push(value));

    const compiled = fixture.nativeElement as HTMLElement;
    const checkbox = compiled.querySelector<HTMLInputElement>('input[type="checkbox"]')!;
    checkbox.checked = false;
    checkbox.dispatchEvent(new Event('change'));

    expect(emitted).toEqual([{}]);
  });

  it('ignores a stale response for a previously selected subcategory that resolves after a newer one', () => {
    const k1aResponse = new Subject<PropertyFilterOption[]>();
    const k1bResponse = new Subject<PropertyFilterOption[]>();
    subcategoriesApiMock.getProperties.mockImplementation((id: string) => (id === 'K1a' ? k1aResponse : k1bResponse));

    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.detectChanges();

    fixture.componentRef.setInput('subcategoryId', 'K1b');
    fixture.detectChanges();

    k1bResponse.next([{ name: 'Speicherkapazitaet', values: ['64', '128'] }]);
    k1aResponse.next(options);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const legends = Array.from(compiled.querySelectorAll('legend')).map((el) => el.textContent);
    expect(legends).toEqual(['Speicherkapazitaet']);
  });

  it('clears stale options when the request for the current subcategory fails', () => {
    subcategoriesApiMock.getProperties.mockReturnValueOnce(of(options)).mockReturnValueOnce(throwError(() => new Error('boom')));

    const fixture = TestBed.createComponent(PropertyFilter);
    fixture.componentRef.setInput('subcategoryId', 'K1a');
    fixture.detectChanges();

    fixture.componentRef.setInput('subcategoryId', 'K1b');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('input[type="checkbox"]').length).toBe(0);
    expect(compiled.querySelector('.property-filter__error')).not.toBeNull();
  });
});
