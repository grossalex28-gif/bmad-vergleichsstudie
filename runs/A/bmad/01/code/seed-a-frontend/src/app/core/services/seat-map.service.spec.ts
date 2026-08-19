import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { SeatMapService } from './seat-map.service';
import { SeatMap } from '../models/seat-map.model';

describe('SeatMapService', () => {
  let service: SeatMapService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(SeatMapService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests GET /api/events/:id/sitzplan and passes the response through', () => {
    const mockSeatMap: SeatMap = {
      eventId: '1',
      rows: ['A', 'B'],
      columns: 3,
      aisleColumns: [2],
      seats: [
        { seatId: 's1', row: 'A', column: 1, status: 'Free' },
        { seatId: 's2', row: 'A', column: 3, status: 'Free' }
      ],
      priceCategories: [{ id: 'pc-a', name: 'Kategorie A', price: 32 }]
    };

    let result: SeatMap | undefined;
    service.getSeatMap('1').subscribe((seatMap) => (result = seatMap));

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    expect(req.request.method).toBe('GET');
    req.flush(mockSeatMap);

    expect(result).toEqual(mockSeatMap);
  });

  it('URL-encodes the event id in the request path', () => {
    service.getSeatMap('a b').subscribe();

    const req = httpMock.expectOne('/api/events/a%20b/sitzplan');
    expect(req.request.method).toBe('GET');
    req.flush({ eventId: 'a b', rows: [], columns: 0, aisleColumns: [], seats: [] });
  });
});
