import { TestBed } from '@angular/core/testing';
import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';

import { EventsApiService } from './events-api.service';
import { EventSummary } from './event-summary';
import { EventDetail } from './event-detail';
import { SeatMap } from './seat-map';

describe('EventsApiService', () => {
  let service: EventsApiService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EventsApiService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('ruft GET /api/events auf und mapped die Antwort', () => {
    const erwarteteEvents: EventSummary[] = [
      { id: 1, titel: 'Kammerkonzert', spielstaette: 'Nord', zeitpunkt: '2026-09-05T19:30:00' }
    ];

    let ergebnis: EventSummary[] | undefined;
    service.getEvents().subscribe(events => (ergebnis = events));

    const req = httpMock.expectOne('/api/events');
    expect(req.request.method).toBe('GET');
    req.flush(erwarteteEvents);

    expect(ergebnis).toEqual(erwarteteEvents);
  });

  it('haengt von und bis als Query-Parameter an, wenn ein Filter uebergeben wird', () => {
    service.getEvents({ von: '2026-09-01', bis: '2026-09-30' }).subscribe();

    const req = httpMock.expectOne(
      request => request.url === '/api/events' && request.params.get('von') === '2026-09-01' && request.params.get('bis') === '2026-09-30'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('haengt venueId als Query-Parameter an, wenn ein Filter uebergeben wird', () => {
    service.getEvents({ venueId: 3 }).subscribe();

    const req = httpMock.expectOne(request => request.url === '/api/events' && request.params.get('venueId') === '3');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('haengt von, bis und venueId gemeinsam als Query-Parameter an, wenn alle drei Filter uebergeben werden', () => {
    service.getEvents({ von: '2026-09-01', bis: '2026-09-30', venueId: 3 }).subscribe();

    const req = httpMock.expectOne(
      request =>
        request.url === '/api/events' &&
        request.params.get('von') === '2026-09-01' &&
        request.params.get('bis') === '2026-09-30' &&
        request.params.get('venueId') === '3'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('ruft GET /api/events/:id auf und mapped die Antwort', () => {
    const erwarteteDetails: EventDetail = {
      id: 1,
      titel: 'Kammerkonzert Frühling',
      beschreibung: 'Ein intimes Konzert mit klassischen Stücken.',
      dauerMinuten: 90,
      altersfreigabe: 0,
      spielstaette: 'Spielstätte Nord',
      raum: 'Kleiner Saal',
      zeitpunkt: '2026-09-05T19:30:00',
      preiskategorien: [{ id: 1, name: 'Kategorie A', preis: 32 }]
    };

    let ergebnis: EventDetail | undefined;
    service.getEvent(1).subscribe(event => (ergebnis = event));

    const req = httpMock.expectOne('/api/events/1');
    expect(req.request.method).toBe('GET');
    req.flush(erwarteteDetails);

    expect(ergebnis).toEqual(erwarteteDetails);
  });

  it('ruft GET /api/events/:id/seatmap auf und mapped die Antwort', () => {
    const erwarteteSeatMap: SeatMap = {
      rowLabels: ['A', 'B'],
      columnCount: 4,
      aisleColumns: [3],
      rows: [
        {
          rowLabel: 'A',
          cells: [
            { columnNumber: 1, status: 'free' },
            { columnNumber: 2, status: 'occupied' },
            { columnNumber: 3, status: 'aisle' },
            { columnNumber: 4, status: 'free' }
          ]
        },
        {
          rowLabel: 'B',
          cells: [
            { columnNumber: 1, status: 'free' },
            { columnNumber: 2, status: 'free' },
            { columnNumber: 3, status: 'aisle' },
            { columnNumber: 4, status: 'occupied' }
          ]
        }
      ]
    };

    let ergebnis: SeatMap | undefined;
    service.getSeatMap(1).subscribe(seatMap => (ergebnis = seatMap));

    const req = httpMock.expectOne('/api/events/1/seatmap');
    expect(req.request.method).toBe('GET');
    req.flush(erwarteteSeatMap);

    expect(ergebnis).toEqual(erwarteteSeatMap);
  });
});
