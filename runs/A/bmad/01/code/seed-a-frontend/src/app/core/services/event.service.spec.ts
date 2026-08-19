import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { EventService } from './event.service';
import { EventDetail, EventListItem } from '../models/event.model';

describe('EventService', () => {
  let service: EventService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(EventService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests GET /api/events and maps the response', () => {
    const mockEvents: EventListItem[] = [
      { id: '1', title: 'Kammerkonzert Frühling', venueName: 'Stadthalle Nordpark', startsAt: '2026-09-05T19:30:00' }
    ];

    let result: EventListItem[] | undefined;
    service.getEvents().subscribe((events) => (result = events));

    const req = httpMock.expectOne('/api/events');
    expect(req.request.method).toBe('GET');
    req.flush(mockEvents);

    expect(result).toEqual(mockEvents);
  });

  it('requests GET /api/events with from/to query parameters when a filter is given', () => {
    service.getEvents({ from: '2026-09-01', to: '2026-09-07' }).subscribe();

    const req = httpMock.expectOne(
      (request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01' && request.params.get('to') === '2026-09-07'
    );
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('requests GET /api/events with a venueId query parameter when a filter is given', () => {
    service.getEvents({ venueId: 'v1' }).subscribe();

    const req = httpMock.expectOne((request) => request.url === '/api/events' && request.params.get('venueId') === 'v1');
    expect(req.request.method).toBe('GET');
    req.flush([]);
  });

  it('requests GET /api/events/:id and passes the response through', () => {
    const mockEvent: EventDetail = {
      id: '1',
      title: 'Kammerkonzert Frühling',
      description: 'Ein stimmungsvolles Kammerkonzert.',
      durationMinutes: 90,
      ageRating: 0,
      venueName: 'Stadthalle Nordpark',
      roomName: 'Kleiner Saal',
      startsAt: '2026-09-05T19:30:00'
    };

    let result: EventDetail | undefined;
    service.getEvent('1').subscribe((event) => (result = event));

    const req = httpMock.expectOne('/api/events/1');
    expect(req.request.method).toBe('GET');
    req.flush(mockEvent);

    expect(result).toEqual(mockEvent);
  });
});
