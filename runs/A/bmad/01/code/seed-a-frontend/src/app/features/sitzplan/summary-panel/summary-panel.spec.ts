import { TestBed } from '@angular/core/testing';

import { SelectedSeatSummary, SummaryPanel } from './summary-panel';

describe('SummaryPanel', () => {
  const seats: SelectedSeatSummary[] = [
    { seatId: 's-a1', row: 'A', column: 1, categoryName: 'Kategorie A', price: 32 },
    { seatId: 's-b3', row: 'B', column: 3, categoryName: 'Kategorie B', price: 22 }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SummaryPanel]
    }).compileComponents();
  });

  it('renders exactly one .summary-panel__item per seat with row, column, category and formatted price', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', seats);
    fixture.componentRef.setInput('totalPrice', 54);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const items = compiled.querySelectorAll('.summary-panel__item');
    expect(items.length).toBe(2);
    expect(items[0].textContent).toContain('Reihe A, Platz 1');
    expect(items[0].textContent).toContain('Kategorie A');
    expect(items[0].textContent).toContain('32,00 €');
    expect(items[1].textContent).toContain('Reihe B, Platz 3');
    expect(items[1].textContent).toContain('Kategorie B');
    expect(items[1].textContent).toContain('22,00 €');
  });

  it('does not render a category span for a seat with categoryName null', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', [{ seatId: 's-a1', row: 'A', column: 1, categoryName: null, price: 0 }]);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.summary-panel__item-category')).toBeFalsy();
  });

  it('renders the sold-out message and no toggle/content when soldOut is true', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.componentRef.setInput('soldOut', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const soldOut = compiled.querySelector('.summary-panel__sold-out');
    expect(soldOut?.textContent?.trim()).toBe('Alle Plätze sind für diese Veranstaltung vergeben.');
    expect(compiled.querySelector('.summary-panel__toggle')).toBeFalsy();
    expect(compiled.querySelector('.summary-panel__content')).toBeFalsy();
  });

  it('renders the empty state instead of a list when no seats are selected', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.summary-panel__empty')?.textContent?.trim()).toBe('Noch keine Plätze ausgewählt.');
    expect(compiled.querySelector('.summary-panel__list')).toBeFalsy();
  });

  it('shows the formatted total price in .summary-panel__total-amount', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 54);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('54,00 €');
  });

  it('toggles the expanded class and aria-expanded on click of the toggle button', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const root = compiled.querySelector('.summary-panel') as HTMLElement;
    const toggle = compiled.querySelector('.summary-panel__toggle') as HTMLElement;

    expect(root.classList.contains('summary-panel--expanded')).toBe(false);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');

    toggle.click();
    fixture.detectChanges();

    expect(root.classList.contains('summary-panel--expanded')).toBe(true);
    expect(toggle.getAttribute('aria-expanded')).toBe('true');

    toggle.click();
    fixture.detectChanges();

    expect(root.classList.contains('summary-panel--expanded')).toBe(false);
    expect(toggle.getAttribute('aria-expanded')).toBe('false');
  });

  it('renders the name and email model values in the form fields', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.componentInstance.name.set('Max Mustermann');
    fixture.componentInstance.email.set('max@example.com');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const nameInput = compiled.querySelector('#summary-panel-name') as HTMLInputElement;
    const emailInput = compiled.querySelector('#summary-panel-email') as HTMLInputElement;

    expect(nameInput.value).toBe('Max Mustermann');
    expect(emailInput.value).toBe('max@example.com');
  });

  it('updates the name and email model signals when the user types into the inputs', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const nameInput = compiled.querySelector('#summary-panel-name') as HTMLInputElement;
    nameInput.value = 'Erika Mustermann';
    nameInput.dispatchEvent(new Event('input'));
    fixture.detectChanges();

    expect(fixture.componentInstance.name()).toBe('Erika Mustermann');
  });

  it('emits submitBooking on click of "Buchung abschließen" when not submitting', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.detectChanges();

    let emitted = false;
    fixture.componentInstance.submitBooking.subscribe(() => (emitted = true));

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    expect(emitted).toBe(true);
  });

  it('disables the submit button and shows "Wird gebucht …" when submitting is true', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.componentRef.setInput('submitting', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const submit = compiled.querySelector('.summary-panel__submit') as HTMLButtonElement;

    expect(submit.disabled).toBe(true);
    expect(submit.textContent?.trim()).toBe('Wird gebucht …');
  });

  it('shows the field error and invalid class under the name field when fieldErrors contains "name"', () => {
    const fixture = TestBed.createComponent(SummaryPanel);
    fixture.componentRef.setInput('seats', []);
    fixture.componentRef.setInput('totalPrice', 0);
    fixture.componentRef.setInput('fieldErrors', new Set(['name']));
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const nameField = compiled.querySelector('.summary-panel__field') as HTMLElement;

    expect(nameField.classList.contains('summary-panel__field--invalid')).toBe(true);
    expect(nameField.querySelector('.summary-panel__field-error')?.textContent?.trim()).toBe(
      'Bitte geben Sie Ihren Namen ein.'
    );
  });
});
