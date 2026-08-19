import { TestBed } from '@angular/core/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { LOCALE_ID } from '@angular/core';
import { registerLocaleData } from '@angular/common';
import localeDe from '@angular/common/locales/de';
import { of, throwError } from 'rxjs';
import { HttpErrorResponse } from '@angular/common/http';

import { EventDetailPage } from './event-detail';
import { EventsApiService } from '../../core/api/events-api.service';
import { EventDetail } from '../../core/api/event-detail';

registerLocaleData(localeDe);

describe('EventDetailPage', () => {
  const event: EventDetail = {
    id: 1,
    titel: 'Kammerkonzert Frühling',
    beschreibung: 'Ein intimes Konzert mit klassischen Stuecken.',
    dauerMinuten: 90,
    altersfreigabe: 0,
    spielstaette: 'Spielstätte Nord',
    raum: 'Kleiner Saal',
    zeitpunkt: '2026-09-05T19:30:00',
    preiskategorien: [{ id: 1, name: 'Kategorie A', preis: 32 }]
  };

  function configure(eventsApiStub: Partial<EventsApiService>, id = '1') {
    TestBed.configureTestingModule({
      imports: [EventDetailPage],
      providers: [
        { provide: EventsApiService, useValue: eventsApiStub },
        { provide: LOCALE_ID, useValue: 'de-DE' },
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ id }) } } }
      ]
    });
  }

  it('rendert alle sechs Angaben nach erfolgreichem Laden', () => {
    configure({ getEvent: () => of(event) });
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain('Ein intimes Konzert');
    expect(text).toContain('90 Minuten');
    expect(text).toContain('Keine Altersbeschränkung');
    expect(text).toContain('Spielstätte Nord');
    expect(text).toContain('Kleiner Saal');
    expect(text).toContain('19:30');
  });

  it('zeigt "Veranstaltung nicht gefunden." bei Fehlercode EVENT_NOT_FOUND', () => {
    const error = new HttpErrorResponse({ status: 404, error: { code: 'EVENT_NOT_FOUND', message: 'Veranstaltung nicht gefunden.' } });
    configure({ getEvent: () => throwError(() => error) });
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Veranstaltung nicht gefunden.');
  });

  it('zeigt bei einem anderen Fehler eine generische Meldung, keinen technischen Fehlertext', () => {
    const error = new HttpErrorResponse({ status: 500 });
    configure({ getEvent: () => throwError(() => error) });
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    const text = (fixture.nativeElement as HTMLElement).textContent;
    expect(text).toContain('Bitte versuchen Sie es später erneut.');
    expect(text).not.toContain('500');
  });

  it('zeigt "Veranstaltung nicht gefunden." bei nicht-numerischer Id, ohne einen API-Aufruf auszulösen', () => {
    const getEvent = vi.fn();
    configure({ getEvent }, 'abc');
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    expect(getEvent).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Veranstaltung nicht gefunden.');
  });

  it('zeigt "Veranstaltung nicht gefunden." bei einer Id außerhalb des Int32-Bereichs, ohne einen API-Aufruf auszulösen', () => {
    const getEvent = vi.fn();
    configure({ getEvent }, '99999999999');
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    expect(getEvent).not.toHaveBeenCalled();
    expect((fixture.nativeElement as HTMLElement).textContent).toContain('Veranstaltung nicht gefunden.');
  });

  it('zeigt einen "Plätze wählen"-Button, der zur Buchungsstrecke dieser Veranstaltung führt', () => {
    configure({ getEvent: () => of(event) });
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.detectChanges();

    const link = (fixture.nativeElement as HTMLElement).querySelector('a.event-detail__book-button');
    expect(link).not.toBeNull();
    expect(link!.textContent).toContain('Plätze wählen');
    expect(link!.getAttribute('href')).toBe('/veranstaltungen/1/buchung');
  });
});
