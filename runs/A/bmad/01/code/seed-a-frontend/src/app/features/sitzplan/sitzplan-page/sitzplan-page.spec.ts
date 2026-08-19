import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { Router, provideRouter } from '@angular/router';

import { SitzplanPage } from './sitzplan-page';
import { SeatMap } from '../../../core/models/seat-map.model';

describe('SitzplanPage', () => {
  let httpMock: HttpTestingController;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [SitzplanPage],
      providers: [provideHttpClient(), provideHttpClientTesting(), provideRouter([])]
    }).compileComponents();
    httpMock = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpMock.verify();
  });

  const mockSeatMap: SeatMap = {
    eventId: '1',
    rows: ['A', 'B'],
    columns: 3,
    aisleColumns: [2],
    seats: [
      { seatId: 's-a1', row: 'A', column: 1, status: 'Free' },
      { seatId: 's-a3', row: 'A', column: 3, status: 'Free' },
      { seatId: 's-b1', row: 'B', column: 1, status: 'Free' },
      { seatId: 's-b3', row: 'B', column: 3, status: 'Free' }
    ],
    priceCategories: [
      { id: 'cat-a', name: 'Kategorie A', price: 32 },
      { id: 'cat-b', name: 'Kategorie B', price: 22 }
    ]
  };

  it('shows the placeholder grid before the response arrives', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    httpMock.expectOne('/api/events/1/sitzplan');

    const compiled = fixture.nativeElement as HTMLElement;
    const placeholder = compiled.querySelector('[aria-busy="true"]');
    expect(placeholder).toBeTruthy();
    expect(placeholder?.querySelectorAll('.sitzplan-page__placeholder-cell').length).toBe(80);
  });

  it('renders free seats and aisle cells and no app-seat at the aisle column after a successful response', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('app-seat').length).toBe(4);
    expect(compiled.querySelectorAll('.seat--free').length).toBe(4);
    expect(compiled.querySelectorAll('.seat--occupied').length).toBe(0);
    expect(compiled.querySelectorAll('.sitzplan-page__aisle').length).toBe(2);
  });

  it('renders row letters and column numbers as visible labels', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const rowLabels = Array.from(compiled.querySelectorAll('.sitzplan-page__row-label')).map((el) => el.textContent?.trim());
    const columnLabels = Array.from(compiled.querySelectorAll('.sitzplan-page__column-label')).map((el) => el.textContent?.trim());
    expect(rowLabels).toEqual(['A', 'B']);
    expect(columnLabels).toEqual(['1', '2', '3']);
  });

  it('treats a column in aisleColumns as an aisle even if the server also sent a seat there', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({
      ...mockSeatMap,
      seats: [...mockSeatMap.seats, { seatId: 's-a2', row: 'A', column: 2, status: 'Free' }]
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('app-seat').length).toBe(4);
    expect(compiled.querySelectorAll('.sitzplan-page__aisle').length).toBe(2);
  });

  it('pans by the raw pointer delta regardless of the current zoom level', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const viewport = compiled.querySelector('.sitzplan-page__viewport') as HTMLElement;
    const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;

    viewport.dispatchEvent(new WheelEvent('wheel', { deltaY: -100, bubbles: true, cancelable: true }));
    fixture.detectChanges();
    expect(grid.style.transform).toContain('scale(1.1)');

    viewport.dispatchEvent(new PointerEvent('pointerdown', { pointerId: 1, clientX: 0, clientY: 0, bubbles: true }));
    viewport.dispatchEvent(new PointerEvent('pointermove', { pointerId: 1, clientX: 10, clientY: 0, buttons: 1, bubbles: true }));
    fixture.detectChanges();

    expect(grid.style.transform).toBe('translate(10px, 0px) scale(1.1)');
  });

  it('renders all seats as occupied and none as free for a response with only occupied seats', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, seats: mockSeatMap.seats.map((seat) => ({ ...seat, status: 'Occupied' })) });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelectorAll('.seat--occupied').length).toBe(4);
    expect(compiled.querySelectorAll('.seat--free').length).toBe(0);
  });

  it('shows the error state instead of the grid when the request fails', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(null, { status: 404, statusText: 'Not Found' });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const alert = compiled.querySelector('[role="alert"]');
    expect(alert).toBeTruthy();
  });

  it('changes the grid transform scale on a wheel event over the viewport', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const viewport = compiled.querySelector('.sitzplan-page__viewport') as HTMLElement;
    const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;

    const transformBefore = grid.style.transform;
    viewport.dispatchEvent(new WheelEvent('wheel', { deltaY: -100, bubbles: true, cancelable: true }));
    fixture.detectChanges();

    expect(grid.style.transform).not.toBe(transformBefore);
  });

  it('selects a free seat on click and deselects it again on a second click', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeat = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;

    firstSeat.click();
    fixture.detectChanges();
    expect(firstSeat.classList.contains('seat--selected')).toBe(true);

    firstSeat.click();
    fixture.detectChanges();
    expect(firstSeat.classList.contains('seat--selected')).toBe(false);
  });

  it('keeps two independently clicked free seats selected at the same time', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seatEls = compiled.querySelectorAll('app-seat');
    const firstSeat = seatEls[0].querySelector('.seat') as HTMLElement;
    const secondSeat = seatEls[1].querySelector('.seat') as HTMLElement;

    firstSeat.click();
    secondSeat.click();
    fixture.detectChanges();

    expect(firstSeat.classList.contains('seat--selected')).toBe(true);
    expect(secondSeat.classList.contains('seat--selected')).toBe(true);
  });

  it('does not change selectedSeatIds when an occupied seat is clicked', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({
      ...mockSeatMap,
      seats: [{ ...mockSeatMap.seats[0], status: 'Occupied' }, ...mockSeatMap.seats.slice(1)]
    });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const occupiedSeat = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;

    occupiedSeat.click();
    fixture.detectChanges();

    expect(compiled.querySelectorAll('.seat--selected').length).toBe(0);
  });

  it('moves the keyboard focus between free seats with arrow keys and skips the aisle column', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    document.body.appendChild(compiled);

    try {
      const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;
      const seatByLabel = (label: string) => compiled.querySelector(`[aria-label="${label}"]`) as HTMLElement;

      grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
      fixture.detectChanges();
      expect(document.activeElement).toBe(seatByLabel('Reihe A, Platz 3, frei'));

      grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
      fixture.detectChanges();
      expect(document.activeElement).toBe(seatByLabel('Reihe B, Platz 3, frei'));

      grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
      fixture.detectChanges();
      expect(document.activeElement).toBe(seatByLabel('Reihe B, Platz 1, frei'));

      grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
      fixture.detectChanges();
      expect(document.activeElement).toBe(seatByLabel('Reihe A, Platz 1, frei'));
    } finally {
      compiled.remove();
    }
  });

  it('assigns tabindex 0 to the first free seat and tabindex -1 to all other free seats after loading', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seats = Array.from(compiled.querySelectorAll('app-seat')).map((el) => el.querySelector('.seat') as HTMLElement);

    expect(seats[0].getAttribute('tabindex')).toBe('0');
    expect(seats.slice(1).every((seat) => seat.getAttribute('tabindex') === '-1')).toBe(true);
  });

  it('has no seat with tabindex 0 when every seat is occupied', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, seats: mockSeatMap.seats.map((seat) => ({ ...seat, status: 'Occupied' })) });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('[tabindex="0"]')).toBeFalsy();
  });

  it('clears a previous selection when the event id changes', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    expect(compiled.querySelectorAll('.seat--selected').length).toBe(1);

    fixture.componentRef.setInput('id', 'anderes-event');
    fixture.detectChanges();

    const secondReq = httpMock.expectOne('/api/events/anderes-event/sitzplan');
    expect(compiled.querySelectorAll('.seat--selected').length).toBe(0);

    secondReq.flush({
      ...mockSeatMap,
      eventId: 'anderes-event',
      seats: [
        { seatId: 'x-a1', row: 'A', column: 1, status: 'Occupied' },
        { seatId: 'x-a3', row: 'A', column: 3, status: 'Free' },
        { seatId: 'x-b1', row: 'B', column: 1, status: 'Free' },
        { seatId: 'x-b3', row: 'B', column: 3, status: 'Free' }
      ]
    });
    fixture.detectChanges();

    const seats = Array.from(compiled.querySelectorAll('app-seat')).map((el) => el.querySelector('.seat') as HTMLElement);
    expect(seats[0].getAttribute('aria-label')).toBe('Reihe A, Platz 1, belegt');
    expect(seats[0].getAttribute('tabindex')).toBeNull();
    expect(seats[1].getAttribute('aria-label')).toBe('Reihe A, Platz 3, frei');
    expect(seats[1].getAttribute('tabindex')).toBe('0');
  });

  it('ignores non-arrow keys on the grid and leaves focus/selection state unchanged', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;
    const tabIndexesBefore = Array.from(compiled.querySelectorAll('app-seat .seat')).map((seat) =>
      seat.getAttribute('tabindex')
    );

    const tabEvent = new KeyboardEvent('keydown', { key: 'Tab', bubbles: true, cancelable: true });
    grid.dispatchEvent(tabEvent);
    fixture.detectChanges();

    expect(tabEvent.defaultPrevented).toBe(false);
    const tabIndexesAfter = Array.from(compiled.querySelectorAll('app-seat .seat')).map((seat) =>
      seat.getAttribute('tabindex')
    );
    expect(tabIndexesAfter).toEqual(tabIndexesBefore);
  });

  it('opens the price category panel with a pill per category in the row of the clicked seat when there are 2 categories', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeat = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;
    firstSeat.click();
    fixture.detectChanges();

    const panel = compiled.querySelector('app-price-category-panel');
    expect(panel).toBeTruthy();
    expect(panel?.querySelectorAll('.price-category-panel__pill').length).toBe(2);
  });

  it('sets the chosen category on the seat aria-label after clicking a pill in the panel', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeatWrapper = compiled.querySelectorAll('app-seat')[0];
    (firstSeatWrapper.querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();

    const pills = compiled.querySelectorAll('.price-category-panel__pill');
    (pills[0] as HTMLElement).click();
    fixture.detectChanges();

    expect((firstSeatWrapper.querySelector('.seat') as HTMLElement).getAttribute('aria-label')).toBe(
      'Reihe A, Platz 1, ausgewählt, Kategorie A, 32,00 €'
    );
  });

  it('changes the assigned category when a different pill is clicked for the same seat (AC4)', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeatWrapper = compiled.querySelectorAll('app-seat')[0];
    (firstSeatWrapper.querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();

    let pills = compiled.querySelectorAll('.price-category-panel__pill');
    (pills[0] as HTMLElement).click();
    fixture.detectChanges();

    pills = compiled.querySelectorAll('.price-category-panel__pill');
    (pills[1] as HTMLElement).click();
    fixture.detectChanges();

    expect((firstSeatWrapper.querySelector('.seat') as HTMLElement).getAttribute('aria-label')).toBe(
      'Reihe A, Platz 1, ausgewählt, Kategorie B, 22,00 €'
    );
  });

  it('removes the assigned category and the selection when the seat is deselected with a second click', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeatWrapper = compiled.querySelectorAll('app-seat')[0];
    const firstSeatEl = firstSeatWrapper.querySelector('.seat') as HTMLElement;
    firstSeatEl.click();
    fixture.detectChanges();

    const pills = compiled.querySelectorAll('.price-category-panel__pill');
    (pills[0] as HTMLElement).click();
    fixture.detectChanges();

    firstSeatEl.click();
    fixture.detectChanges();

    expect(firstSeatEl.classList.contains('seat--selected')).toBe(false);
    expect(firstSeatEl.getAttribute('aria-label')).toBe('Reihe A, Platz 1, frei');
  });

  it('closes the price category panel on Escape while keeping the seat selected', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;
    const firstSeatEl = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;
    firstSeatEl.click();
    fixture.detectChanges();

    expect(compiled.querySelector('app-price-category-panel')).toBeTruthy();

    grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(compiled.querySelector('app-price-category-panel')).toBeFalsy();
    expect(firstSeatEl.classList.contains('seat--selected')).toBe(true);
  });

  it('restores keyboard focus to the seat when Escape is pressed while a pill inside the panel is focused', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const grid = compiled.querySelector('.sitzplan-page__grid') as HTMLElement;
    const firstSeatEl = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;
    firstSeatEl.click();
    fixture.detectChanges();

    const pill = compiled.querySelector('.price-category-panel__pill') as HTMLElement;
    pill.focus();
    expect(document.activeElement).toBe(pill);

    grid.dispatchEvent(new KeyboardEvent('keydown', { key: 'Escape', bubbles: true }));
    fixture.detectChanges();

    expect(compiled.querySelector('app-price-category-panel')).toBeFalsy();
    expect(document.activeElement).toBe(firstSeatEl);
  });

  it('auto-assigns the only category without opening a panel when the event has exactly 1 price category (AC3)', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, priceCategories: [{ id: 'only', name: 'Einheitspreis', price: 20 }] });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const firstSeatEl = compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement;
    firstSeatEl.click();
    fixture.detectChanges();

    expect(firstSeatEl.classList.contains('seat--selected')).toBe(true);
    expect(compiled.querySelector('app-price-category-panel')).toBeFalsy();
    expect(firstSeatEl.getAttribute('aria-label')).toBe('Reihe A, Platz 1, ausgewählt, Einheitspreis, 20,00 €');
  });

  it('clears assignedCategoryId when the event id changes, leaving no category suffix on any seat', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    fixture.componentRef.setInput('id', 'anderes-event');
    fixture.detectChanges();

    const secondReq = httpMock.expectOne('/api/events/anderes-event/sitzplan');
    secondReq.flush({
      ...mockSeatMap,
      eventId: 'anderes-event',
      seats: [
        { seatId: 'x-a1', row: 'A', column: 1, status: 'Free' },
        { seatId: 'x-a3', row: 'A', column: 3, status: 'Free' },
        { seatId: 'x-b1', row: 'B', column: 1, status: 'Free' },
        { seatId: 'x-b3', row: 'B', column: 3, status: 'Free' }
      ]
    });
    fixture.detectChanges();

    const seats = Array.from(compiled.querySelectorAll('app-seat')).map((el) => el.querySelector('.seat') as HTMLElement);
    expect(seats.every((seat) => !seat.getAttribute('aria-label')?.includes('Kategorie'))).toBe(true);
  });

  it('shows one summary-panel item with row, column, category and price after selecting a seat and choosing a category', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    const items = summaryPanel.querySelectorAll('.summary-panel__item');
    expect(items.length).toBe(1);
    expect(items[0].textContent).toContain('Reihe A, Platz 1');
    expect(items[0].textContent).toContain('Kategorie A');
    expect(items[0].textContent).toContain('32,00 €');
  });

  it('shows two summary-panel items sorted by row/column with a total of 54,00 € for two categorized seats', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seatEls = compiled.querySelectorAll('app-seat');

    (seatEls[1].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[1] as HTMLElement).click();
    fixture.detectChanges();

    (seatEls[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    const items = summaryPanel.querySelectorAll('.summary-panel__item');
    expect(items.length).toBe(2);
    expect(items[0].textContent).toContain('Reihe A, Platz 1');
    expect(items[1].textContent).toContain('Reihe A, Platz 3');
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('54,00 €');
  });

  it('shows a selected seat without category suffix and 0,00 € before a pill is clicked, updating afterwards', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    let items = summaryPanel.querySelectorAll('.summary-panel__item');
    expect(items.length).toBe(1);
    expect(items[0].textContent).toContain('Reihe A, Platz 1');
    expect(items[0].querySelector('.summary-panel__item-category')).toBeFalsy();
    expect(items[0].textContent).toContain('0,00 €');
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('0,00 €');

    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    items = summaryPanel.querySelectorAll('.summary-panel__item');
    expect(items[0].textContent).toContain('Kategorie A');
    expect(items[0].textContent).toContain('32,00 €');
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('32,00 €');
  });

  it('removes the summary-panel item and reduces the total when a categorized seat is deselected', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seatEls = compiled.querySelectorAll('app-seat');
    const firstSeatEl = seatEls[0].querySelector('.seat') as HTMLElement;

    firstSeatEl.click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    (seatEls[1].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[1] as HTMLElement).click();
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('54,00 €');

    firstSeatEl.click();
    fixture.detectChanges();

    expect(summaryPanel.querySelectorAll('.summary-panel__item').length).toBe(1);
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('22,00 €');
  });

  it('shows a total of 40,00 € immediately for two seats with an auto-assigned single category', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, priceCategories: [{ id: 'only', name: 'Einheitspreis', price: 20 }] });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seatEls = compiled.querySelectorAll('app-seat');
    (seatEls[0].querySelector('.seat') as HTMLElement).click();
    (seatEls[1].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('40,00 €');
  });

  it('shows the sold-out message and no toggle/list in the summary panel when every seat is occupied', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, seats: mockSeatMap.seats.map((seat) => ({ ...seat, status: 'Occupied' })) });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    expect(summaryPanel.querySelector('.summary-panel__sold-out')?.textContent?.trim()).toBe(
      'Alle Plätze sind für diese Veranstaltung vergeben.'
    );
    expect(summaryPanel.querySelector('.summary-panel__toggle')).toBeFalsy();
    expect(summaryPanel.querySelector('.summary-panel__list')).toBeFalsy();
  });

  it('toggles the expanded class on app-summary-panel when its toggle button is clicked', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const summaryPanelRoot = compiled.querySelector('app-summary-panel .summary-panel') as HTMLElement;
    const toggle = summaryPanelRoot.querySelector('.summary-panel__toggle') as HTMLElement;

    expect(summaryPanelRoot.classList.contains('summary-panel--expanded')).toBe(false);
    toggle.click();
    fixture.detectChanges();
    expect(summaryPanelRoot.classList.contains('summary-panel--expanded')).toBe(true);
  });

  it('resets the summary panel total to 0,00 € and shows the empty list when the event id changes', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    fixture.componentRef.setInput('id', 'anderes-event');
    fixture.detectChanges();

    const secondReq = httpMock.expectOne('/api/events/anderes-event/sitzplan');
    secondReq.flush({
      ...mockSeatMap,
      eventId: 'anderes-event',
      seats: [
        { seatId: 'x-a1', row: 'A', column: 1, status: 'Free' },
        { seatId: 'x-a3', row: 'A', column: 3, status: 'Free' },
        { seatId: 'x-b1', row: 'B', column: 1, status: 'Free' },
        { seatId: 'x-b3', row: 'B', column: 3, status: 'Free' }
      ]
    });
    fixture.detectChanges();

    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    expect(summaryPanel.querySelector('.summary-panel__total-amount')?.textContent?.trim()).toBe('0,00 €');
    expect(summaryPanel.querySelector('.summary-panel__empty')).toBeTruthy();
  });

  it('does not show the sold-out message for a room with zero configured seats', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush({ ...mockSeatMap, seats: [] });
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const summaryPanel = compiled.querySelector('app-summary-panel') as HTMLElement;
    expect(summaryPanel.querySelector('.summary-panel__sold-out')).toBeFalsy();
    expect(summaryPanel.querySelector('.summary-panel__empty')?.textContent?.trim()).toBe('Noch keine Plätze ausgewählt.');
  });

  function setInputValue(input: HTMLInputElement, value: string): void {
    input.value = value;
    input.dispatchEvent(new Event('input'));
  }

  function selectFirstSeatWithCategory(compiled: HTMLElement, fixture: { detectChanges: () => void }): void {
    (compiled.querySelectorAll('app-seat')[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();
  }

  function fillNameAndEmail(compiled: HTMLElement, fixture: { detectChanges: () => void }): void {
    setInputValue(compiled.querySelector('#summary-panel-name') as HTMLInputElement, 'Max Mustermann');
    setInputValue(compiled.querySelector('#summary-panel-email') as HTMLInputElement, 'max@example.com');
    fixture.detectChanges();
  }

  it('submits POST /api/bookings with eventId, name, email and seats when the form is filled and submitted', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    selectFirstSeatWithCategory(compiled, fixture);
    fillNameAndEmail(compiled, fixture);

    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    const bookingReq = httpMock.expectOne('/api/bookings');
    expect(bookingReq.request.method).toBe('POST');
    expect(bookingReq.request.body).toEqual({
      eventId: '1',
      name: 'Max Mustermann',
      email: 'max@example.com',
      seats: [{ seatId: 's-a1', priceCategoryId: 'cat-a' }]
    });
  });

  it('navigates to /buchungen/:reference with the booking in state after a successful 201 response', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    const router = TestBed.inject(Router);
    const navigateSpy = vi.spyOn(router, 'navigate');

    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    selectFirstSeatWithCategory(compiled, fixture);
    fillNameAndEmail(compiled, fixture);
    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    const bookingResponse = {
      reference: 'ABCDEFGHJKMNPQRSTUVWXYZ23',
      eventId: '1',
      eventTitle: 'Kammerkonzert',
      venueName: 'Stadthalle Nordpark',
      startsAt: '2026-09-05T19:30:00',
      status: 'Active',
      seats: [{ seatId: 's-a1', row: 'A', column: 1, priceCategoryId: 'cat-a', priceCategoryName: 'Kategorie A', price: 32 }],
      totalPrice: 32
    };
    httpMock.expectOne('/api/bookings').flush(bookingResponse, { status: 201, statusText: 'Created' });

    expect(navigateSpy).toHaveBeenCalledWith(['/buchungen', bookingResponse.reference], {
      state: { booking: bookingResponse, justBooked: true }
    });
  });

  it('sets fieldErrors with "name" and sends no HTTP request when the name is empty on submit', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    selectFirstSeatWithCategory(compiled, fixture);
    setInputValue(compiled.querySelector('#summary-panel-email') as HTMLInputElement, 'max@example.com');
    fixture.detectChanges();

    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectNone('/api/bookings');
    const nameField = compiled.querySelector('.summary-panel__field') as HTMLElement;
    expect(nameField.classList.contains('summary-panel__field--invalid')).toBe(true);
  });

  it('shows an error banner naming the conflicting seat, reloads the seat map and keeps the remaining selection and form values on a 409 response', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    const seatEls = compiled.querySelectorAll('app-seat');

    (seatEls[0].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    (seatEls[1].querySelector('.seat') as HTMLElement).click();
    fixture.detectChanges();
    (compiled.querySelectorAll('.price-category-panel__pill')[0] as HTMLElement).click();
    fixture.detectChanges();

    fillNameAndEmail(compiled, fixture);
    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectOne('/api/bookings').flush(
      { conflictingSeats: ['A3'] },
      { status: 409, statusText: 'Conflict' }
    );
    fixture.detectChanges();

    const banner = compiled.querySelector('app-error-banner .error-banner');
    expect(banner?.textContent?.trim()).toBe('Platz A3 ist inzwischen vergeben.');

    const reloadReq = httpMock.expectOne('/api/events/1/sitzplan');
    reloadReq.flush({
      ...mockSeatMap,
      seats: [
        { seatId: 's-a1', row: 'A', column: 1, status: 'Free' },
        { seatId: 's-a3', row: 'A', column: 3, status: 'Occupied' },
        { seatId: 's-b1', row: 'B', column: 1, status: 'Free' },
        { seatId: 's-b3', row: 'B', column: 3, status: 'Free' }
      ]
    });
    fixture.detectChanges();

    expect(compiled.querySelectorAll('.seat--selected').length).toBe(1);
    expect((compiled.querySelector('#summary-panel-name') as HTMLInputElement).value).toBe('Max Mustermann');
    expect((compiled.querySelector('#summary-panel-email') as HTMLInputElement).value).toBe('max@example.com');
  });

  it('sets fieldErrors from a 400 response without discarding the current selection', () => {
    const fixture = TestBed.createComponent(SitzplanPage);
    fixture.componentRef.setInput('id', '1');
    fixture.detectChanges();

    const req = httpMock.expectOne('/api/events/1/sitzplan');
    req.flush(mockSeatMap);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    selectFirstSeatWithCategory(compiled, fixture);
    fillNameAndEmail(compiled, fixture);
    (compiled.querySelector('.summary-panel__submit') as HTMLButtonElement).click();
    fixture.detectChanges();

    httpMock.expectOne('/api/bookings').flush(
      { errors: { name: ['Pflichtangaben fehlen.'] } },
      { status: 400, statusText: 'Bad Request' }
    );
    fixture.detectChanges();

    const nameField = compiled.querySelector('.summary-panel__field') as HTMLElement;
    expect(nameField.classList.contains('summary-panel__field--invalid')).toBe(true);
    expect(compiled.querySelectorAll('.seat--selected').length).toBe(1);
  });
});
