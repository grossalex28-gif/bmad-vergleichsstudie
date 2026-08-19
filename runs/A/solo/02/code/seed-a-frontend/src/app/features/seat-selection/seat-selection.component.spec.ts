import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { ActivatedRoute, convertToParamMap, provideRouter } from '@angular/router';
import { ComponentFixture, TestBed } from '@angular/core/testing';

import { SeatSelectionComponent } from './seat-selection.component';
import { Seat } from '../../core/models/seat-map.model';

const eventDetail = {
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
  priceCategories: [
    { id: 'E1-A', name: 'Kategorie A', price: 30 },
    { id: 'E1-B', name: 'Kategorie B', price: 20 }
  ]
};

const seatMap = {
  eventId: 'E1',
  roomId: 'R1',
  roomName: 'Kleiner Saal',
  rowLabels: ['A'],
  columns: 3,
  rows: [
    {
      row: 'A',
      seats: [
        { column: 1, type: 'seat', status: 'free' },
        { column: 2, type: 'aisle', status: 'aisle' },
        { column: 3, type: 'seat', status: 'occupied' }
      ]
    }
  ]
};

describe('SeatSelectionComponent', () => {
  let fixture: ComponentFixture<SeatSelectionComponent>;
  let httpMock: HttpTestingController;

  async function setup() {
    await TestBed.configureTestingModule({
      imports: [SeatSelectionComponent],
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

    fixture = TestBed.createComponent(SeatSelectionComponent);
    httpMock = TestBed.inject(HttpTestingController);
    fixture.detectChanges();

    httpMock.expectOne('/api/events/E1').flush(eventDetail);
    httpMock.expectOne('/api/events/E1/seatmap').flush(seatMap);
    await fixture.whenStable();
  }

  afterEach(() => httpMock.verify());

  it('selects a free seat on click and computes the total price', async () => {
    await setup();
    const instance = fixture.componentInstance as unknown as {
      onSeatClick: (seat: Seat, row: string) => void;
      selectedSeatList: () => { row: string; column: number }[];
      totalPrice: () => number;
    };

    instance.onSeatClick({ column: 1, type: 'seat', status: 'free' }, 'A');

    expect(instance.selectedSeatList().length).toBe(1);
    expect(instance.totalPrice()).toBe(30);
  });

  it('does not select an occupied seat', async () => {
    await setup();
    const instance = fixture.componentInstance as unknown as {
      onSeatClick: (seat: Seat, row: string) => void;
      selectedSeatList: () => unknown[];
    };

    instance.onSeatClick({ column: 3, type: 'seat', status: 'occupied' }, 'A');

    expect(instance.selectedSeatList().length).toBe(0);
  });

  it('toggles a seat off when clicked twice', async () => {
    await setup();
    const instance = fixture.componentInstance as unknown as {
      onSeatClick: (seat: Seat, row: string) => void;
      selectedSeatList: () => unknown[];
    };
    const seat: Seat = { column: 1, type: 'seat', status: 'free' };

    instance.onSeatClick(seat, 'A');
    instance.onSeatClick(seat, 'A');

    expect(instance.selectedSeatList().length).toBe(0);
  });

  it('marks conflicting seats occupied and drops them from the selection on a 409 response', async () => {
    await setup();
    const instance = fixture.componentInstance as unknown as {
      onSeatClick: (seat: Seat, row: string) => void;
      selectedSeatList: () => unknown[];
      submitBooking: () => void;
      submitError: () => string | null;
      seatMap: () => { rows: { row: string; seats: Seat[] }[] };
      customerName: string;
      customerEmail: string;
    };

    instance.onSeatClick({ column: 1, type: 'seat', status: 'free' }, 'A');
    instance.customerName = 'Ada Lovelace';
    instance.customerEmail = 'ada@example.com';
    instance.submitBooking();

    const req = httpMock.expectOne('/api/bookings');
    req.flush(
      { message: 'Sitzplatz belegt.', conflictingSeats: [{ row: 'A', column: 1 }] },
      { status: 409, statusText: 'Conflict' }
    );

    expect(instance.submitError()).toBeTruthy();
    expect(instance.selectedSeatList().length).toBe(0);
    const seatA1 = instance.seatMap().rows[0].seats.find((s) => s.column === 1);
    expect(seatA1?.status).toBe('occupied');
  });
});
