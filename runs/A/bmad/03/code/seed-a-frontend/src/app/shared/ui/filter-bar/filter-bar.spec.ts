import { TestBed } from '@angular/core/testing';
import { FilterBar } from './filter-bar';

describe('FilterBar', () => {
  function createFixture() {
    TestBed.configureTestingModule({ imports: [FilterBar] });
    return TestBed.createComponent(FilterBar);
  }

  it('rendert zwei Datumsfelder mit sichtbaren Labels', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    const inputs: HTMLInputElement[] = fixture.nativeElement.querySelectorAll('input[type="date"]');
    expect(inputs.length).toBe(2);

    const labels: HTMLLabelElement[] = fixture.nativeElement.querySelectorAll('label');
    expect(labels.length).toBe(3);
    expect(fixture.nativeElement.textContent).toContain('Von');
    expect(fixture.nativeElement.textContent).toContain('Bis');
  });

  it('aktualisiert das von-Signal, wenn das Von-Feld geändert wird', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    const vonInput: HTMLInputElement = fixture.nativeElement.querySelector('#filter-bar-von');
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.von()).toBe('2026-09-01');
  });

  it('aktualisiert das bis-Signal, wenn das Bis-Feld geändert wird', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    const bisInput: HTMLInputElement = fixture.nativeElement.querySelector('#filter-bar-bis');
    bisInput.value = '2026-09-30';
    bisInput.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.bis()).toBe('2026-09-30');
  });

  it('zeigt den "Zurücksetzen"-Link nur, wenn von oder bis gesetzt ist', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen')).toBeNull();

    fixture.componentInstance.von.set('2026-09-01');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen')).not.toBeNull();
  });

  it('rendert "Alle" plus eine Option je Eintrag im spielstaetten-Input', () => {
    const fixture = createFixture();
    fixture.componentRef.setInput('spielstaetten', [
      { id: 'V1', name: 'Stadthalle Nordpark' },
      { id: 'V2', name: 'Kulturhaus Südtor' }
    ]);
    fixture.detectChanges();

    const optionen: HTMLOptionElement[] = fixture.nativeElement.querySelectorAll(
      '#filter-bar-spielstaette option'
    );
    expect(optionen.length).toBe(3);
    expect(optionen[0].textContent?.trim()).toBe('Alle');
    expect(optionen[1].textContent?.trim()).toBe('Stadthalle Nordpark');
    expect(optionen[2].textContent?.trim()).toBe('Kulturhaus Südtor');
  });

  it('aktualisiert das spielstaetteId-Signal, wenn eine Option ausgewählt wird', () => {
    const fixture = createFixture();
    fixture.componentRef.setInput('spielstaetten', [{ id: 'V1', name: 'Stadthalle Nordpark' }]);
    fixture.detectChanges();

    const select: HTMLSelectElement = fixture.nativeElement.querySelector('#filter-bar-spielstaette');
    select.value = 'V1';
    select.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.spielstaetteId()).toBe('V1');
  });

  it('zeigt den "Zurücksetzen"-Link auch, wenn nur spielstaetteId gesetzt ist', () => {
    const fixture = createFixture();
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen')).toBeNull();

    fixture.componentInstance.spielstaetteId.set('V1');
    fixture.detectChanges();

    expect(fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen')).not.toBeNull();
  });

  it('setzt beim Klick auf "Zurücksetzen" alle drei Signale auf null', () => {
    const fixture = createFixture();
    fixture.componentInstance.von.set('2026-09-01');
    fixture.componentInstance.bis.set('2026-09-30');
    fixture.componentInstance.spielstaetteId.set('V1');
    fixture.detectChanges();

    const zuruecksetzenButton: HTMLButtonElement = fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen');
    zuruecksetzenButton.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.von()).toBeNull();
    expect(fixture.componentInstance.bis()).toBeNull();
    expect(fixture.componentInstance.spielstaetteId()).toBeNull();
    expect(fixture.nativeElement.querySelector('.filter-bar__zuruecksetzen')).toBeNull();
  });
});
