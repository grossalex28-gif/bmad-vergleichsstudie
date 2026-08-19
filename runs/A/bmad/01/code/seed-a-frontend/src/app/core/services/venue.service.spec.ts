import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';

import { VenueService } from './venue.service';
import { VenueListItem } from '../models/venue.model';

describe('VenueService', () => {
  let service: VenueService;
  let httpMock: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [provideHttpClient(), provideHttpClientTesting()]
    });
    service = TestBed.inject(VenueService);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('requests GET /api/venues and maps the response', () => {
    const mockVenues: VenueListItem[] = [
      { id: 'v1', name: 'Stadthalle Nordpark' },
      { id: 'v2', name: 'Kulturhaus Südtor' }
    ];

    let result: VenueListItem[] | undefined;
    service.getVenues().subscribe((venues) => (result = venues));

    const req = httpMock.expectOne('/api/venues');
    expect(req.request.method).toBe('GET');
    req.flush(mockVenues);

    expect(result).toEqual(mockVenues);
  });
});
