import { TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';

import { EventCard } from './event-card';

describe('EventCard', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [EventCard],
      providers: [provideRouter([])]
    }).compileComponents();
  });

  it('renders startsAt as the unshifted wall-clock time from the backend, without timezone conversion', () => {
    const fixture = TestBed.createComponent(EventCard);
    fixture.componentRef.setInput('id', '1');
    fixture.componentRef.setInput('title', 'Kammerkonzert Frühling');
    fixture.componentRef.setInput('venueName', 'Stadthalle Nordpark');
    fixture.componentRef.setInput('startsAt', '2026-09-05T19:30:00');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const meta = compiled.querySelector('.event-card__meta')?.textContent ?? '';
    expect(meta).toContain('05.09.2026, 19:30');
  });
});
