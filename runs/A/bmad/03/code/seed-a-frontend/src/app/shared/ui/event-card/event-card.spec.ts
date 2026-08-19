import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { Veranstaltung } from '../../models/veranstaltung';
import { EventCard } from './event-card';

describe('EventCard', () => {
  const veranstaltung: Veranstaltung = {
    id: 'E1',
    titel: 'Kammerkonzert Frühling',
    spielstaetteId: 'V1',
    spielstaetteName: 'Stadthalle Nordpark',
    zeitpunkt: '2026-09-05T19:30:00+02:00'
  };

  it('rendert die Karte als Link auf die Detailansicht der Veranstaltung', () => {
    TestBed.configureTestingModule({
      imports: [EventCard],
      providers: [provideRouter([])]
    });
    const fixture = TestBed.createComponent(EventCard);
    fixture.componentRef.setInput('veranstaltung', veranstaltung);
    fixture.detectChanges();

    const link: HTMLAnchorElement = fixture.nativeElement.querySelector('a.event-card');
    expect(link.getAttribute('href')).toBe('/veranstaltungen/E1');
  });
});
