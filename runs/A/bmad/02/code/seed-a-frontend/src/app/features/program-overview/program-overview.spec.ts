import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, Subject, throwError } from 'rxjs';

import { ProgramOverview } from './program-overview';
import { EventsApiService, EventsFilter } from '../../core/api/events-api.service';
import { EventSummary } from '../../core/api/event-summary';
import { VenuesApiService } from '../../core/api/venues-api.service';
import { Venue } from '../../core/api/venue';

describe('ProgramOverview', () => {
  const events: EventSummary[] = [
    { id: 1, titel: 'Kammerkonzert', spielstaette: 'Nord', zeitpunkt: '2026-09-05T19:30:00' },
    { id: 2, titel: 'Sinfoniekonzert', spielstaette: 'Süd', zeitpunkt: '2026-09-12T20:00:00' }
  ];

  function configure(eventsApiStub: Partial<EventsApiService>, venuesApiStub: Partial<VenuesApiService> = { getVenues: () => of([]) }) {
    TestBed.configureTestingModule({
      imports: [ProgramOverview],
      providers: [
        provideRouter([]),
        { provide: EventsApiService, useValue: eventsApiStub },
        { provide: VenuesApiService, useValue: venuesApiStub }
      ]
    });
  }

  it('rendert eine event-card je Veranstaltung aus dem Service', () => {
    configure({ getEvents: () => of(events) });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('app-event-card');
    expect(cards.length).toBe(2);
    expect(compiled.textContent).toContain('Kammerkonzert');
    expect(compiled.textContent).toContain('Sinfoniekonzert');
  });

  it('zeigt den Leer-Zustand-Text, wenn der Service eine leere Liste liefert', () => {
    configure({ getEvents: () => of([]) });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('app-event-card').length).toBe(0);
    expect(compiled.textContent).toContain('Keine Veranstaltungen im gewählten Zeitraum/Spielstätte gefunden.');
  });

  it('zeigt während des Ladens weder den Leer-Zustand noch eine Fehlermeldung', () => {
    configure({ getEvents: () => new Subject<EventSummary[]>() });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('app-event-card').length).toBe(0);
    expect(compiled.textContent).not.toContain('Keine Veranstaltungen im gewählten Zeitraum/Spielstätte gefunden.');
    expect(compiled.textContent).not.toContain('konnten nicht geladen werden');
  });

  it('zeigt eine Fehlermeldung statt des Leer-Zustands, wenn der Service fehlschlägt', () => {
    configure({ getEvents: () => throwError(() => new Error('Netzwerkfehler')) });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Veranstaltungen konnten nicht geladen werden.');
    expect(compiled.textContent).not.toContain('Keine Veranstaltungen im gewählten Zeitraum/Spielstätte gefunden.');
  });

  it('loest bei Aenderung von von/bis einen erneuten getEvents-Aufruf mit den Filter-Parametern aus', () => {
    const aufrufe: (EventsFilter | undefined)[] = [];
    configure({
      getEvents: filter => {
        aufrufe.push(filter);
        return of(events);
      }
    });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    expect(aufrufe).toEqual([{ von: null, bis: null, venueId: null }]);

    fixture.componentInstance.von.set('2026-09-01');
    fixture.detectChanges();

    expect(aufrufe).toEqual([
      { von: null, bis: null, venueId: null },
      { von: '2026-09-01', bis: null, venueId: null }
    ]);
  });

  it('loest bei Aenderung von venueId einen erneuten getEvents-Aufruf mit dem neuen Wert aus', () => {
    const aufrufe: (EventsFilter | undefined)[] = [];
    configure({
      getEvents: filter => {
        aufrufe.push(filter);
        return of(events);
      }
    });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    expect(aufrufe).toEqual([{ von: null, bis: null, venueId: null }]);

    fixture.componentInstance.venueId.set(2);
    fixture.detectChanges();

    expect(aufrufe).toEqual([
      { von: null, bis: null, venueId: null },
      { von: null, bis: null, venueId: 2 }
    ]);
  });

  it('befuellt venues() beim Erstellen aus dem venuesApi-Stub und ruft es bei Filteraenderungen nicht erneut ab', () => {
    const venues: Venue[] = [{ id: 1, name: 'Spielstätte Nord' }];
    let aufrufAnzahl = 0;
    configure(
      { getEvents: () => of(events) },
      {
        getVenues: () => {
          aufrufAnzahl++;
          return of(venues);
        }
      }
    );

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    expect(fixture.componentInstance.venues()).toEqual(venues);
    expect(aufrufAnzahl).toBe(1);

    fixture.componentInstance.von.set('2026-09-01');
    fixture.detectChanges();

    expect(aufrufAnzahl).toBe(1);
  });

  it('ignoriert eine spaet eintreffende Antwort einer durch Filteraenderung veralteten Anfrage', () => {
    const ersteAnfrage = new Subject<EventSummary[]>();
    const zweiteAnfrage = new Subject<EventSummary[]>();
    let aufrufNummer = 0;
    configure({
      getEvents: () => (++aufrufNummer === 1 ? ersteAnfrage : zweiteAnfrage)
    });

    const fixture = TestBed.createComponent(ProgramOverview);
    fixture.detectChanges();

    fixture.componentInstance.von.set('2026-09-01');
    fixture.detectChanges();

    zweiteAnfrage.next([events[0]]);
    ersteAnfrage.next([events[1]]);

    expect(fixture.componentInstance.events()).toEqual([events[0]]);
  });
});
