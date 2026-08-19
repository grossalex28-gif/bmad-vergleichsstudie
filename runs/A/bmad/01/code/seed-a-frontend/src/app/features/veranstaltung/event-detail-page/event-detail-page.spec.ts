import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { provideRouter } from '@angular/router';

import { EventDetailPage } from './event-detail-page';
import { EventDetail } from '../../../core/models/event.model';

describe('EventDetailPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventDetailPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

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

  it('shows all detail fields after a successful GET /api/events/:id response', () => {
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1');
    expect(req.request.method).toBe('GET');
    req.flush(mockEvent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const text = compiled.textContent ?? '';
    expect(text).toContain('Kammerkonzert Frühling');
    expect(text).toContain('Stadthalle Nordpark');
    expect(text).toContain('Kleiner Saal');
    expect(text).toContain('05.09.2026, 19:30');
    expect(text).toContain('Ein stimmungsvolles Kammerkonzert.');
    expect(text).toContain('90 Minuten');
  });

  it('shows the loading text before the response arrives', () => {
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    httpMock.expectOne('/api/events/1');

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Wird geladen');
  });

  it('shows a non-zero age rating as "Ab X Jahren"', () => {
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1');
    req.flush({ ...mockEvent, ageRating: 16 });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.textContent).toContain('Ab 16 Jahren');
  });

  it('renders a "Plätze wählen" link to the Sitzplan route', () => {
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1');
    req.flush(mockEvent);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const link = compiled.querySelector('a[href="/veranstaltungen/1/sitzplan"]');
    expect(link).toBeTruthy();
    expect(link?.textContent).toContain('Plätze wählen');
  });

  it('shows an alert error state instead of empty fields when the event is not found (404)', () => {
    const fixture = TestBed.createComponent(EventDetailPage);
    fixture.componentRef.setInput('id', 'unknown');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/unknown');
    req.flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const alert = compiled.querySelector('[role="alert"]');
    expect(alert).toBeTruthy();
    expect(compiled.textContent).not.toContain(mockEvent.title);
  });
});
