import { TestBed } from '@angular/core/testing';

import { BookingForm } from './booking-form';
import { BookingSelectionService } from '../booking-selection.service';

describe('BookingForm', () => {
  function createFixture() {
    TestBed.configureTestingModule({
      imports: [BookingForm],
      providers: [BookingSelectionService]
    });
    const fixture = TestBed.createComponent(BookingForm);
    const selection = TestBed.inject(BookingSelectionService);
    return { fixture, selection };
  }

  function submitButton(fixture: ReturnType<typeof TestBed.createComponent<BookingForm>>): HTMLButtonElement {
    return (fixture.nativeElement as HTMLElement).querySelector('button[type="submit"]') as HTMLButtonElement;
  }

  function setInputValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  it('deaktiviert den Submit-Button ohne vollständige Sitzplatz-/Kategorie-Auswahl', () => {
    const { fixture } = createFixture();
    fixture.detectChanges();

    expect(submitButton(fixture).disabled).toBe(true);
  });

  it('deaktiviert den Submit-Button bei leerem Namen', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.assignCategory('A', 1, 10);
    fixture.detectChanges();

    const emailInput = (fixture.nativeElement as HTMLElement).querySelector('#buchung-email') as HTMLInputElement;
    setInputValue(emailInput, 'erika@example.com');
    fixture.detectChanges();

    expect(submitButton(fixture).disabled).toBe(true);
  });

  it('deaktiviert den Submit-Button bei ungueltiger E-Mail', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.assignCategory('A', 1, 10);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    setInputValue(root.querySelector('#buchung-name') as HTMLInputElement, 'Erika Musterfrau');
    setInputValue(root.querySelector('#buchung-email') as HTMLInputElement, 'keine-email');
    fixture.detectChanges();

    expect(submitButton(fixture).disabled).toBe(true);
  });

  it('zeigt nach Verlassen des Feldes Inline-Fehler mit aria-describedby-Verknuepfung', () => {
    const { fixture } = createFixture();
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    const nameInput = root.querySelector('#buchung-name') as HTMLInputElement;
    expect(root.querySelector('#buchung-name-fehler')).toBeNull();

    nameInput.dispatchEvent(new Event('blur'));
    fixture.detectChanges();

    const fehler = root.querySelector('#buchung-name-fehler');
    expect(fehler).not.toBeNull();
    expect(nameInput.getAttribute('aria-describedby')).toBe('buchung-name-fehler');
  });

  it('deaktiviert den Button und zeigt den Ladetext, wenn submitting() true ist', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.assignCategory('A', 1, 10);
    fixture.componentRef.setInput('submitting', true);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    setInputValue(root.querySelector('#buchung-name') as HTMLInputElement, 'Erika Musterfrau');
    setInputValue(root.querySelector('#buchung-email') as HTMLInputElement, 'erika@example.com');
    fixture.detectChanges();

    expect(submitButton(fixture).disabled).toBe(true);
    expect(submitButton(fixture).textContent).toContain('Buchung wird geprüft');
  });

  it('emittiert submitted mit getrimmtem Namen und E-Mail bei gueltigen Eingaben und vollstaendiger Auswahl', () => {
    const { fixture, selection } = createFixture();
    selection.toggle('A', 1);
    selection.assignCategory('A', 1, 10);
    fixture.detectChanges();

    const root = fixture.nativeElement as HTMLElement;
    setInputValue(root.querySelector('#buchung-name') as HTMLInputElement, '  Erika Musterfrau  ');
    setInputValue(root.querySelector('#buchung-email') as HTMLInputElement, 'erika@example.com');
    fixture.detectChanges();

    const emitted = vi.fn();
    fixture.componentInstance.submitted.subscribe(emitted);

    submitButton(fixture).click();

    expect(emitted).toHaveBeenCalledWith({ name: 'Erika Musterfrau', email: 'erika@example.com' });
  });
});
