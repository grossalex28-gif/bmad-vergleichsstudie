import { TestBed } from '@angular/core/testing';

import { EventFilterBar } from './event-filter-bar';
import { Venue } from '../../../core/api/venue';

describe('EventFilterBar', () => {
  it('rendert weder Chips noch Reset-Button, wenn beide Filter null sind', () => {
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.event-filter-bar__chip').length).toBe(0);
    expect(compiled.querySelector('.button-secondary')).toBeNull();
  });

  it('rendert genau einen Chip pro gesetztem Filter', () => {
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('von', '2026-09-01');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const chips = compiled.querySelectorAll('.event-filter-bar__chip');
    expect(chips.length).toBe(1);
    expect(chips[0].textContent).toContain('01.09.2026');
  });

  it('zeigt den Reset-Button nur bei mindestens einem aktiven Filter und setzt bei Klick beide Filter zurueck', () => {
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('von', '2026-09-01');
    fixture.componentRef.setInput('bis', '2026-09-30');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const resetButton = compiled.querySelector<HTMLButtonElement>('.button-secondary');
    expect(resetButton).not.toBeNull();
    resetButton!.click();

    expect(fixture.componentInstance.von()).toBeNull();
    expect(fixture.componentInstance.bis()).toBeNull();
  });

  it('entfernt beim Klick auf das Chip-x nur den zugehoerigen Filter, der andere bleibt erhalten', () => {
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('von', '2026-09-01');
    fixture.componentRef.setInput('bis', '2026-09-30');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonEntfernenButton = compiled.querySelector<HTMLButtonElement>('button[aria-label="Von-Filter entfernen"]');
    expect(vonEntfernenButton).not.toBeNull();
    vonEntfernenButton!.click();
    fixture.detectChanges();

    expect(fixture.componentInstance.von()).toBeNull();
    expect(fixture.componentInstance.bis()).toBe('2026-09-30');
  });

  it('rendert eine Option je Eintrag in venues() plus die feste "Alle Spielstaetten"-Option', () => {
    const venues: Venue[] = [
      { id: 1, name: 'Spielstätte Nord' },
      { id: 2, name: 'Spielstätte Süd' }
    ];
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('venues', venues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const options = compiled.querySelectorAll<HTMLOptionElement>('#spielstaette option');
    expect(options.length).toBe(3);
    expect(options[0].textContent).toContain('Alle Spielstätten');
    expect(options[1].textContent).toContain('Spielstätte Nord');
    expect(options[2].textContent).toContain('Spielstätte Süd');
  });

  it('setzt venueId() auf die gewaehlte Id, wenn eine Option ausgewaehlt wird', () => {
    const venues: Venue[] = [
      { id: 1, name: 'Spielstätte Nord' },
      { id: 2, name: 'Spielstätte Süd' }
    ];
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('venues', venues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const select = compiled.querySelector<HTMLSelectElement>('#spielstaette')!;
    select.value = '2';
    select.dispatchEvent(new Event('change'));

    expect(fixture.componentInstance.venueId()).toBe(2);
  });

  it('zeigt einen Chip mit dem Venue-Namen bei gesetztem venueId', () => {
    const venues: Venue[] = [{ id: 1, name: 'Spielstätte Nord' }];
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('venues', venues);
    fixture.componentRef.setInput('venueId', 1);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const chips = compiled.querySelectorAll('.event-filter-bar__chip');
    expect(chips.length).toBe(1);
    expect(chips[0].textContent).toContain('Spielstätte Nord');
  });

  it('setzt beim Klick auf den Reset-Button auch venueId zurueck', () => {
    const venues: Venue[] = [{ id: 1, name: 'Spielstätte Nord' }];
    const fixture = TestBed.createComponent(EventFilterBar);
    fixture.componentRef.setInput('venues', venues);
    fixture.componentRef.setInput('venueId', 1);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const resetButton = compiled.querySelector<HTMLButtonElement>('.button-secondary');
    expect(resetButton).not.toBeNull();
    resetButton!.click();

    expect(fixture.componentInstance.venueId()).toBeNull();
  });
});
