import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { EventDetailComponent } from './event-detail.component';

describe('EventDetailComponent', () => {
  let fixture: ComponentFixture<EventDetailComponent>;
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventDetailComponent],
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        provideRouter([]),
        {
          provide: ActivatedRoute,
          useValue: { snapshot: { paramMap: convertToParamMap({ id: 'E1' }) } }
        }
      ]
    }).compileComponents();

    fixture = TestBed.createComponent(EventDetailComponent);
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpMock.verify());

  it('loads the event by id from the route', () => {
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/E1');
    req.flush({
      id: 'E1',
      title: 'Kammerkonzert',
      description: 'Ein Konzert.',
      durationMinutes: 90,
      ageRating: 0,
      venueId: 'V1',
      venueName: 'Stadthalle Nordpark',
      roomId: 'R1',
      roomName: 'Kleiner Saal',
      startsAt: '2026-09-05T19:30:00',
      priceCategories: [{ id: 'E1-A', name: 'Kategorie A', price: 32 }]
    });

    expect(fixture.componentInstance['event']()?.title).toBe('Kammerkonzert');
    expect(fixture.componentInstance['loading']()).toBe(false);
  });

  it('sets an error when the event cannot be found', () => {
    fixture.detectChanges();

    httpMock.expectOne('/api/events/E1').flush('not found', { status: 404, statusText: 'Not Found' });

    expect(fixture.componentInstance['error']()).toBeTruthy();
  });
});
