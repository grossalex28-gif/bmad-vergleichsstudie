import { TestBed } from '@angular/core/testing';

import { PropertyFilters } from './property-filters';

describe('PropertyFilters', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [PropertyFilters]
    });
  });

  it('renders one labelled input per property and a button labelled "Filtern"', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform', 'Kabellos']);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('input').length).toBe(2);
    expect(element.querySelector('input[name="Bauform"]')).toBeTruthy();
    expect(element.querySelector('input[name="Kabellos"]')).toBeTruthy();
    expect(element.querySelector('button')?.textContent).toContain('Filtern');
  });

  it('emits filtersChanged with all filled-in values when the form is submitted', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform', 'Kabellos']);
    fixture.detectChanges();

    let emitted: Record<string, string> | null | undefined;
    fixture.componentInstance.filtersChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const kabellosInput = element.querySelector('input[name="Kabellos"]') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    kabellosInput.value = 'true';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toEqual({ Bauform: 'In-Ear', Kabellos: 'true' });
  });

  it('emits filtersChanged with only the filled-in field when the other field is left empty', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform', 'Kabellos']);
    fixture.detectChanges();

    let emitted: Record<string, string> | null | undefined;
    fixture.componentInstance.filtersChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    bauformInput.value = 'In-Ear';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toEqual({ Bauform: 'In-Ear' });
  });

  it('emits filtersChanged with null when all fields are left empty', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform', 'Kabellos']);
    fixture.detectChanges();

    let emitted: Record<string, string> | null | undefined = { unset: 'value' };
    fixture.componentInstance.filtersChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const form = element.querySelector('form') as HTMLFormElement;
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toBeNull();
  });

  it('treats a whitespace-only field as empty', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform']);
    fixture.detectChanges();

    let emitted: Record<string, string> | null | undefined = { unset: 'value' };
    fixture.componentInstance.filtersChanged.subscribe((value) => (emitted = value));

    const element = fixture.nativeElement as HTMLElement;
    const bauformInput = element.querySelector('input[name="Bauform"]') as HTMLInputElement;
    const form = element.querySelector('form') as HTMLFormElement;
    bauformInput.value = '   ';
    form.dispatchEvent(new Event('submit'));

    expect(emitted).toBeNull();
  });

  it('disables all inputs and the button when disabled is set', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Bauform', 'Kabellos']);
    fixture.componentRef.setInput('disabled', true);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const inputs = element.querySelectorAll('input');
    const button = element.querySelector('button') as HTMLButtonElement;
    inputs.forEach((input) => expect((input as HTMLInputElement).disabled).toBe(true));
    expect(button.disabled).toBe(true);
  });

  it('renders no inputs when properties is empty', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', []);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.querySelectorAll('input').length).toBe(0);
  });

  it('clears an unsubmitted value in a reused input when properties changes to a set sharing a name', () => {
    const fixture = TestBed.createComponent(PropertyFilters);
    fixture.componentRef.setInput('properties', ['Farbe', 'Bauform']);
    fixture.detectChanges();

    const element = fixture.nativeElement as HTMLElement;
    const farbeInput = element.querySelector('input[name="Farbe"]') as HTMLInputElement;
    farbeInput.value = 'Rot';

    fixture.componentRef.setInput('properties', ['Farbe', 'Speicherkapazitaet']);
    fixture.detectChanges();

    const farbeInputAfterSwitch = element.querySelector('input[name="Farbe"]') as HTMLInputElement;
    expect(farbeInputAfterSwitch.value).toBe('');
  });
});
