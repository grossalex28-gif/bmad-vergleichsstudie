import { TestBed } from '@angular/core/testing';
import { vi } from 'vitest';
import { SeatTile } from './seat-tile';

describe('SeatTile', () => {
  function createFixture(
    reihe: string,
    spalte: number,
    status: 'Frei' | 'Belegt',
    optionen?: { ausgewaehlt?: boolean; fokussierbar?: boolean },
  ) {
    TestBed.configureTestingModule({ imports: [SeatTile] });
    const fixture = TestBed.createComponent(SeatTile);
    fixture.componentRef.setInput('reihe', reihe);
    fixture.componentRef.setInput('spalte', spalte);
    fixture.componentRef.setInput('status', status);
    if (optionen?.ausgewaehlt !== undefined) {
      fixture.componentRef.setInput('ausgewaehlt', optionen.ausgewaehlt);
    }
    if (optionen?.fokussierbar !== undefined) {
      fixture.componentRef.setInput('fokussierbar', optionen.fokussierbar);
    }
    fixture.detectChanges();
    return fixture;
  }

  it('trägt für einen belegten Platz das exakte aria-label', () => {
    const fixture = createFixture('B', 3, 'Belegt');

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    expect(kachel.getAttribute('aria-label')).toBe('Reihe B, Platz 3, belegt');
  });

  it('trägt für einen freien Platz das exakte aria-label und zeigt die Code-Bezeichnung sichtbar', () => {
    const fixture = createFixture('B', 3, 'Frei');

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    expect(kachel.getAttribute('aria-label')).toBe('Reihe B, Platz 3, frei');
    expect(fixture.nativeElement.textContent).toContain('B3');
  });

  it('trägt für einen belegten Platz die seat-tile--belegt-Klasse', () => {
    const belegtFixture = createFixture('B', 3, 'Belegt');
    const belegtKachel: HTMLElement = belegtFixture.nativeElement.querySelector('.seat-tile');
    expect(belegtKachel.classList.contains('seat-tile--belegt')).toBe(true);
  });

  it('trägt für einen freien Platz die seat-tile--belegt-Klasse nicht', () => {
    const freiFixture = createFixture('B', 3, 'Frei');
    const freiKachel: HTMLElement = freiFixture.nativeElement.querySelector('.seat-tile');
    expect(freiKachel.classList.contains('seat-tile--belegt')).toBe(false);
  });

  it('emittiert auswahlUmschalten genau einmal bei Klick auf einen freien Platz', () => {
    const fixture = createFixture('B', 3, 'Frei');
    const handler = vi.fn();
    fixture.componentInstance.auswahlUmschalten.subscribe(handler);

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    kachel.click();

    expect(handler).toHaveBeenCalledTimes(1);
  });

  it('emittiert auswahlUmschalten nicht bei Klick auf einen belegten Platz', () => {
    const fixture = createFixture('B', 3, 'Belegt');
    const handler = vi.fn();
    fixture.componentInstance.auswahlUmschalten.subscribe(handler);

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    kachel.click();

    expect(handler).not.toHaveBeenCalled();
  });

  it('zeigt bei ausgewaehlt=true und status=Frei das Häkchen-Icon, die Auswahlklasse und das passende aria-label', () => {
    const fixture = createFixture('B', 3, 'Frei', { ausgewaehlt: true });

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    expect(kachel.classList.contains('seat-tile--ausgewaehlt')).toBe(true);
    expect(fixture.nativeElement.textContent).toContain('✓');
    expect(kachel.getAttribute('aria-label')).toBe('Reihe B, Platz 3, ausgewählt');
  });

  it('entfernt Häkchen, Klasse und aria-label nach Rücknahme der Auswahl', () => {
    const fixture = createFixture('B', 3, 'Frei', { ausgewaehlt: true });

    fixture.componentRef.setInput('ausgewaehlt', false);
    fixture.detectChanges();

    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    expect(kachel.classList.contains('seat-tile--ausgewaehlt')).toBe(false);
    expect(fixture.nativeElement.textContent).not.toContain('✓');
    expect(kachel.getAttribute('aria-label')).toBe('Reihe B, Platz 3, frei');
  });

  it('setzt tabindex="0" bei fokussierbar=true und tabindex="-1" bei fokussierbar=false', () => {
    const fixture = createFixture('B', 3, 'Frei', { fokussierbar: true });
    const kachel: HTMLElement = fixture.nativeElement.querySelector('.seat-tile');
    expect(kachel.getAttribute('tabindex')).toBe('0');

    fixture.componentRef.setInput('fokussierbar', false);
    fixture.detectChanges();
    expect(kachel.getAttribute('tabindex')).toBe('-1');
  });
});
