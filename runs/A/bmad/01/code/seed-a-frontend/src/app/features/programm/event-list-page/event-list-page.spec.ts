import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { EventListPage } from './event-list-page';
import { EventListItem } from '../../../core/models/event.model';
import { VenueListItem } from '../../../core/models/venue.model';

describe('EventListPage', () => {
  let httpMock: HttpTestingController;

  const mockVenues: VenueListItem[] = [
    { id: 'v1', name: 'Stadthalle Nordpark' },
    { id: 'v2', name: 'Kulturhaus Südtor' }
  ];

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventListPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();

    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  it('shows placeholder cards before the response arrives', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events');
    httpMock.expectOne('/api/venues').flush([]);

    const compiled = fixture.nativeElement as HTMLElement;
    const placeholders = compiled.querySelectorAll('.event-list-page__placeholder-card');
    expect(placeholders.length).toBeGreaterThanOrEqual(4);
    expect(placeholders.length).toBeLessThanOrEqual(6);
    expect(compiled.querySelectorAll('app-event-card').length).toBe(0);
  });

  it('shows event cards with the correct content after the response arrives', () => {
    const mockEvents: EventListItem[] = [
      { id: '1', title: 'Kammerkonzert Frühling', venueName: 'Stadthalle Nordpark', startsAt: '2026-09-05T19:30:00' },
      { id: '2', title: 'Comedy-Abend', venueName: 'Stadthalle Nordpark', startsAt: '2026-09-20T21:00:00' }
    ];

    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events');
    req.flush(mockEvents);
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const cards = compiled.querySelectorAll('app-event-card');
    expect(cards.length).toBe(2);
    expect(compiled.querySelectorAll('.event-list-page__placeholder-card').length).toBe(0);
    expect(compiled.textContent).toContain('Kammerkonzert Frühling');
    expect(compiled.textContent).toContain('Comedy-Abend');
  });

  it('shows an error message when the request fails', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events');
    req.flush('Serverfehler', { status: 500, statusText: 'Internal Server Error' });
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.event-list-page__error')?.textContent).toContain(
      'Veranstaltungen konnten nicht geladen werden.'
    );
    expect(compiled.querySelectorAll('.event-list-page__placeholder-card').length).toBe(0);
    expect(compiled.querySelectorAll('app-event-card').length).toBe(0);
  });

  it('requests events with from/to query parameters when the filter bar changes', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const req = httpMock.expectOne(
      (request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01'
    );
    req.flush([]);
  });

  it('requests all events without query parameters again when the filter is cleared', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    httpMock
      .expectOne((request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01')
      .flush([]);
    fixture.detectChanges();

    vonInput.value = '';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const req = httpMock.expectOne(
      (request) => request.url === '/api/events' && request.params.keys().length === 0
    );
    req.flush([]);
  });

  it('shows the second (more recent) response when two filter changes race, ignoring the stale first response', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;

    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const firstReq = httpMock.expectOne(
      (request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01'
    );

    vonInput.value = '2026-09-02';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    const secondReq = httpMock.expectOne(
      (request) => request.url === '/api/events' && request.params.get('from') === '2026-09-02'
    );

    const freshEvent: EventListItem = {
      id: '2',
      title: 'Aktuell',
      venueName: 'Stadthalle Nordpark',
      startsAt: '2026-09-02T19:30:00'
    };

    expect(firstReq.cancelled).toBe(true);

    secondReq.flush([freshEvent]);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Aktuell');
  });

  it('requests events with a venueId query parameter when a venue is selected in the filter', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush(mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = 'v1';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const req = httpMock.expectOne((request) => request.url === '/api/events' && request.params.get('venueId') === 'v1');
    req.flush([]);
  });

  it('combines date and venue filters into a single request with from, to and venueId set', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush(mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    httpMock
      .expectOne((request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01')
      .flush([]);
    fixture.detectChanges();

    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = 'v1';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    const req = httpMock.expectOne(
      (request) =>
        request.url === '/api/events' &&
        request.params.get('from') === '2026-09-01' &&
        request.params.get('venueId') === 'v1'
    );
    req.flush([]);
  });

  it('shows a chip for each active filter and clicking a chip resets only that filter', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush(mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const vonInput = compiled.querySelector<HTMLInputElement>('#filter-bar-von')!;
    vonInput.value = '2026-09-01';
    vonInput.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    httpMock
      .expectOne((request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01')
      .flush([]);
    fixture.detectChanges();

    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = 'v1';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();
    httpMock
      .expectOne(
        (request) =>
          request.url === '/api/events' &&
          request.params.get('from') === '2026-09-01' &&
          request.params.get('venueId') === 'v1'
      )
      .flush([]);
    fixture.detectChanges();

    const chips = compiled.querySelectorAll('.event-list-page__chip');
    expect(chips.length).toBe(2);
    expect(compiled.textContent).toContain('Von 01.09.2026');
    expect(compiled.textContent).toContain('Stadthalle Nordpark');

    const venueChip = Array.from(chips).find((chip) => chip.textContent?.includes('Stadthalle Nordpark'))!;
    (venueChip as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock
      .expectOne((request) => request.url === '/api/events' && request.params.get('from') === '2026-09-01' && !request.params.has('venueId'))
      .flush([]);
    fixture.detectChanges();

    expect(venueSelect.value).toBe('');
    expect(vonInput.value).toBe('2026-09-01');
  });

  it('shows the empty-state text and a reset link when a filtered request returns no results, and reset clears every filter', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush(mockVenues);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const venueSelect = compiled.querySelector<HTMLSelectElement>('#filter-bar-venue')!;
    venueSelect.value = 'v1';
    venueSelect.dispatchEvent(new Event('change'));
    fixture.detectChanges();

    httpMock.expectOne((request) => request.url === '/api/events' && request.params.get('venueId') === 'v1').flush([]);
    fixture.detectChanges();

    expect(compiled.textContent).toContain('Keine Veranstaltungen im gewählten Zeitraum.');
    const resetButton = compiled.querySelector<HTMLButtonElement>('.event-list-page__reset')!;
    expect(resetButton).toBeTruthy();

    resetButton.click();
    fixture.detectChanges();

    const req = httpMock.expectOne((request) => request.url === '/api/events' && request.params.keys().length === 0);
    req.flush([]);
  });

  it('shows no hint text when the result is empty without any active filter', () => {
    const fixture = TestBed.createComponent(EventListPage);
    fixture.detectChanges();

    httpMock.expectOne('/api/events').flush([]);
    httpMock.expectOne('/api/venues').flush([]);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.event-list-page__empty')).toBeNull();
    expect(compiled.querySelectorAll('.event-list-page__grid').length).toBe(1);
  });
});
