import { TestBed } from '@angular/core/testing';

import { FilterBar } from './filter-bar';
import { VenueListItem } from '../../../core/models/venue.model';

describe('FilterBar', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [FilterBar]
    }).compileComponents();
  });

  it('updates the von model when the Von-input changes', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.von()).toBe('2026-09-01');
  });

  it('updates the bis model when the Bis-input changes', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const bisInput = compiled.querySelector<HTMLInputElement>('#filter-bar-bis')!;
    bisInput.value = '2026-09-07';
    bisInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.bis()).toBe('2026-09-07');
  });

  it('resets the von model to null when the Von-input is cleared', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.componentRef.setInput('von', '2026-09-01');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.von()).toBeNull();
  });

  const mockVenues: VenueListItem[] = [
    { id: 'v1', name: 'Stadthalle Nordpark' },
    { id: 'v2', name: 'Kulturhaus Südtor' }
  ];

  it('renders an option for each venue passed via the venues input', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.componentRef.setInput('venues', mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const options = compiled.querySelectorAll<HTMLOptionElement>('#filter-bar-venue option');
    expect(options.length).toBe(3);
    expect(options[0].textContent).toContain('Alle Spielstätten');
    expect(options[1].textContent).toContain('Stadthalle Nordpark');
    expect(options[2].textContent).toContain('Kulturhaus Südtor');
  });

  it('updates the venueId model when a venue is selected', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.componentRef.setInput('venues', mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = 'v1';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.venueId()).toBe('v1');
  });

  it('resets the venueId model to null when "Alle Spielstätten" is selected', () => {
    const fixture = TestBed.createComponent(FilterBar);
    fixture.componentRef.setInput('venues', mockVenues);
    fixture.componentRef.setInput('venueId', 'v1');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = '';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    expect(fixture.componentInstance.venueId()).toBeNull();
  });
});
