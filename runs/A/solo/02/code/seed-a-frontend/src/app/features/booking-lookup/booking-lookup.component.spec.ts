import { Router, provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { BookingLookupComponent } from './booking-lookup.component';

describe('BookingLookupComponent', () => {
  let fixture: ComponentFixture<BookingLookupComponent>;
  let router: Router;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [BookingLookupComponent],
      providers: [provideRouter([])]
    }).compileComponents();

    fixture = TestBed.createComponent(BookingLookupComponent);
    router = TestBed.inject(Router);
  });

  it('navigates to the booking detail page with a normalized reference', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const instance = fixture.componentInstance as unknown as { reference: string; lookup: () => void };

    instance.reference = '  ab3d9f2k  ';
    instance.lookup();

    expect(navigateSpy).toHaveBeenCalledWith(['/bookings', 'AB3D9F2K']);
  });

  it('shows an error and does not navigate when the reference is empty', () => {
    const navigateSpy = vi.spyOn(router, 'navigate').mockResolvedValue(true);
    const instance = fixture.componentInstance as unknown as {
      reference: string;
      lookup: () => void;
      error: () => string | null;
    };

    instance.reference = '   ';
    instance.lookup();

    expect(navigateSpy).not.toHaveBeenCalled();
    expect(instance.error()).toBeTruthy();
  });
});
