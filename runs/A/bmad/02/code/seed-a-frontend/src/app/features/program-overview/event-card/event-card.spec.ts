import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';

import { EventCard } from './event-card';
import { EventSummary } from '../../../core/api/event-summary';

registerLocaleData(localeDe);

describe('EventCard', () => {
  const event: EventSummary = {
    id: 42,
    titel: 'Kammerkonzert Frühling',
    spielstaette: 'Spielstätte Nord',
    zeitpunkt: '2026-09-05T19:30:00'
  };

  beforeEach(() => {
    TestBed.configureTestingModule({
      imports: [EventCard],
      providers: [provideRouter([]), { provide: LOCALE_ID, useValue: 'de-DE' }]
    });
  });

  it('rendert Titel, Spielstätte und Datum/Uhrzeit', () => {
    const fixture = TestBed.createComponent(EventCard);
    fixture.componentRef.setInput('event', event);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Kammerkonzert Frühling');
    expect(compiled.textContent).toContain('Spielstätte Nord');
    expect(compiled.textContent).toContain('19:30');
    expect(compiled.textContent).toContain('Samstag');
  });

  it('hat als Wurzelelement ein einzelnes fokussierbares/klickbares <a>', () => {
    const fixture = TestBed.createComponent(EventCard);
    fixture.componentRef.setInput('event', event);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const links = compiled.querySelectorAll('a');
    expect(links.length).toBe(1);
    expect(links[0].getAttribute('href')).toContain('/veranstaltungen/42');
    expect(links[0].textContent).toContain('Kammerkonzert Frühling');
    expect(links[0].textContent).toContain('Spielstätte Nord');
  });
});
