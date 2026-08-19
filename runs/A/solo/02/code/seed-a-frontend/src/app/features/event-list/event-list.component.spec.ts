import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventListComponent } from './event-list.component';

describe('EventListComponent', () => {
  let fixture: ComponentFixture<EventListComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventListComponent],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(EventListComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('loads venues and events on init', () => {
    fixture.detectChanges();

    const venuesReq = httpMock.expectOne('/api/venues');
    venuesReq.flush([{ id: 'V1', name: 'Stadthalle Nordpark', rooms: [] }]);

    const eventsReq = httpMock.expectOne((r) => r.url === '/api/events');
    eventsReq.flush([
      { id: 'E1', title: 'Kammerkonzert', venueId: 'V1', venueName: 'Stadthalle Nordpark', startsAt: '2026-09-05T19:30:00' }
    ]);

    expect(fixture.componentInstance['events']().length).toBe(1);
    expect(fixture.componentInstance['venues']().length).toBe(1);
  });

  it('sends filter parameters when applying filters', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/venues').flush([]);
    httpMock.expectOne((r) => r.url === '/api/events').flush([]);

    const instance = fixture.componentInstance as unknown as {
      from: string;
      to: string;
      venueId: string;
      applyFilters: () => void;
    };
    instance.from = '2026-09-01';
    instance.venueId = 'V1';
    instance.applyFilters();

    const req = httpMock.expectOne(
      (r) => r.url === '/api/events' && r.params.get('from') === '2026-09-01' && r.params.get('venueId') === 'V1'
    );
    req.flush([]);
  });

  it('shows an error message when loading events fails', () => {
    fixture.detectChanges();
    httpMock.expectOne('/api/venues').flush([]);
    httpMock.expectOne((r) => r.url === '/api/events').flush('error', { status: 500, statusText: 'Server Error' });

    expect(fixture.componentInstance['error']()).toBeTruthy();
  });
});
