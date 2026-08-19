import { TestBed } from '@angular/core/testing';

import { SearchBox } from './search-box';

describe('SearchBox', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [SearchBox]
    });
  });

  it('renders a search input and a button labelled "Suchen"', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelector('input[type="search"]')).toBeTruthy();
    expect(element.querySelector('button')?.textContent).toContain('Suchen');
  });

  it('emits searchChanged with the trimmed value when the form is submitted with text', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    let emitted: string | null | undefined;
    fixture.componentInstance.searchChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    input.value = 'Kaffee';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toBe('Kaffee');
  });

  it('emits searchChanged with null when the form is submitted without text', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    let emitted: string | null | undefined = 'not-null';
    fixture.componentInstance.searchChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    input.value = '';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toBeNull();
  });

  it('emits searchChanged with null when the form is submitted with whitespace only', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    let emitted: string | null | undefined = 'not-null';
    fixture.componentInstance.searchChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    input.value = '   ';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toBeNull();
  });

  it('resyncs the input value to the trimmed text after submit', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    input.value = '  Kaffee  ';
    form.dispatchEvent(new Event('submit'));

    expect(input.value).toBe('Kaffee');
  });

  it('emits searchChanged with null when the native search-input clear action empties the value', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    let emitted: string | null | undefined = 'not-null';
    fixture.componentInstance.searchChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    input.value = '';
    input.dispatchEvent(new Event('search'));

    expect(emitted).toBeNull();
  });

  it('does not emit on the native search event while the input still has text', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.detectChanges();

    let emitted: string | null | undefined = 'not-null';
    fixture.componentInstance.searchChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    input.value = 'Kaffee';
    input.dispatchEvent(new Event('search'));

    expect(emitted).toBe('not-null');
  });

  it('disables the input and the button when disabled is set', () => {
    const fixture = TestBed.createComponent(SearchBox);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const input = element.querySelector('input') as HTMLInputElement;
    const button = element.querySelector('button') as HTMLButtonElement;
    expect(input.disabled).toBe(true);
    expect(button.disabled).toBe(true);
  });
});
