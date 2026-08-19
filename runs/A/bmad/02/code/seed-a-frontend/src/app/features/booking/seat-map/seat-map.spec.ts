import { TestBed } from '@angular/core/testing';

import { SeatMapGrid } from './seat-map';
import { SeatMap, SeatStatus } from '../../../core/api/seat-map';
import { BookingSelectionService } from '../booking-selection.service';

describe('SeatMapGrid', () => {
  const seatMap: SeatMap = {
    rowLabels: ['A', 'B'],
    columnCount: 4,
    aisleColumns: [3],
    rows: [
      {
        rowLabel: 'A',
        cells: [
          { columnNumber: 1, status: 'free' },
          { columnNumber: 2, status: 'occupied' },
          { columnNumber: 3, status: 'aisle' },
          { columnNumber: 4, status: 'free' }
        ]
      },
      {
        rowLabel: 'B',
        cells: [
          { columnNumber: 1, status: 'free' },
          { columnNumber: 2, status: 'free' },
          { columnNumber: 3, status: 'aisle' },
          { columnNumber: 4, status: 'occupied' }
        ]
      }
    ]
  };

  function createFixture() {
    TestBed.configureTestingModule({ imports: [SeatMapGrid], providers: [BookingSelectionService] });
    const fixture = TestBed.createComponent(SeatMapGrid);
    fixture.componentRef.setInput('seatMap', seatMap);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
    return fixture;
  }

  afterEach(() => {
    document.body.innerHTML = '';
  });

  it('rendert role="grid" und je Reihe ein role="row"', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[role="grid"]')).not.toBeNull();
    expect(root.querySelectorAll('[role="row"]').length).toBe(2);
  });

  it('setzt aria-label je Zelle nach dem Muster "Reihe X, Platz Y, frei/belegt"', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const freieZelle = root.querySelector('[aria-label="Reihe A, Platz 1, frei"]');
    const belegteZelle = root.querySelector('[aria-label="Reihe A, Platz 2, belegt"]');

    expect(freieZelle).not.toBeNull();
    expect(belegteZelle).not.toBeNull();
  });

  it('rendert Gang-Positionen ohne role="gridcell" und als .seat-cell--aisle ohne Button', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const aisleCells = root.querySelectorAll('.seat-cell--aisle');
    expect(aisleCells.length).toBe(2);
    aisleCells.forEach(cell => {
      expect(cell.getAttribute('role')).not.toBe('gridcell');
      expect(cell.tagName).not.toBe('BUTTON');
    });
  });

  it('rendert belegte Zellen als native disabled-Buttons', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const occupiedCells = root.querySelectorAll<HTMLButtonElement>('.seat-cell--occupied');
    expect(occupiedCells.length).toBe(2);
    occupiedCells.forEach(cell => {
      expect(cell.tagName).toBe('BUTTON');
      expect(cell.disabled).toBe(true);
    });
  });

  it('setzt initial genau eine freie Zelle auf tabIndex 0, alle anderen freien Zellen auf -1', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const freeCells = Array.from(root.querySelectorAll<HTMLButtonElement>('.seat-cell--free'));
    const zeroTabIndex = freeCells.filter(cell => cell.tabIndex === 0);

    expect(zeroTabIndex.length).toBe(1);
    expect(zeroTabIndex[0].getAttribute('aria-label')).toBe('Reihe A, Platz 1, frei');
    freeCells
      .filter(cell => cell !== zeroTabIndex[0])
      .forEach(cell => expect(cell.tabIndex).toBe(-1));
  });

  it('verschiebt den Fokus bei ArrowRight von Reihe A Platz 1 zu Reihe A Platz 4 (überspringt belegt und Gang)', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const start = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    start.focus();
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowRight', bubbles: true }));
    fixture.detectChanges();

    expect(document.activeElement?.getAttribute('aria-label')).toBe('Reihe A, Platz 4, frei');
  });

  it('verschiebt den Fokus bei ArrowDown von Reihe A Platz 1 zu Reihe B Platz 1', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const start = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    start.focus();
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowDown', bubbles: true }));
    fixture.detectChanges();

    expect(document.activeElement?.getAttribute('aria-label')).toBe('Reihe B, Platz 1, frei');
  });

  it('verschiebt den Fokus bei ArrowLeft von Reihe A Platz 4 zu Reihe A Platz 1 (überspringt Gang und belegt)', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const start = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 4, frei"]')!;
    start.focus();
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    fixture.detectChanges();

    expect(document.activeElement?.getAttribute('aria-label')).toBe('Reihe A, Platz 1, frei');
  });

  it('verschiebt den Fokus bei ArrowUp von Reihe B Platz 1 zu Reihe A Platz 1', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const start = root.querySelector<HTMLButtonElement>('[aria-label="Reihe B, Platz 1, frei"]')!;
    start.focus();
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    fixture.detectChanges();

    expect(document.activeElement?.getAttribute('aria-label')).toBe('Reihe A, Platz 1, frei');
  });

  it('belässt den Fokus unverändert, wenn ArrowUp/ArrowLeft am Rand keine weitere freie Zelle findet', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const start = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    start.focus();
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowUp', bubbles: true }));
    start.dispatchEvent(new KeyboardEvent('keydown', { key: 'ArrowLeft', bubbles: true }));
    fixture.detectChanges();

    expect(document.activeElement?.getAttribute('aria-label')).toBe('Reihe A, Platz 1, frei');
  });

  it('markiert eine freie Zelle nach Klick als ausgewählt', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const cell = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    cell.click();
    fixture.detectChanges();

    expect(root.querySelector('[aria-label="Reihe A, Platz 1, ausgewählt"]')).not.toBeNull();
    expect(cell.classList.contains('seat-cell--selected')).toBe(true);
    expect(cell.classList.contains('seat-cell--free')).toBe(false);
  });

  it('hebt die Auswahl bei erneutem Klick auf dieselbe Zelle wieder auf', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const cell = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    cell.click();
    fixture.detectChanges();
    cell.click();
    fixture.detectChanges();

    expect(root.querySelector('[aria-label="Reihe A, Platz 1, frei"]')).not.toBeNull();
    expect(cell.classList.contains('seat-cell--free')).toBe(true);
    expect(cell.classList.contains('seat-cell--selected')).toBe(false);
  });

  it('hält zwei nicht benachbarte freie Zellen gleichzeitig ausgewählt', () => {
    const fixture = createFixture();
    const root = fixture.nativeElement as HTMLElement;

    const first = root.querySelector<HTMLButtonElement>('[aria-label="Reihe A, Platz 1, frei"]')!;
    const second = root.querySelector<HTMLButtonElement>('[aria-label="Reihe B, Platz 2, frei"]')!;
    first.click();
    second.click();
    fixture.detectChanges();

    expect(root.querySelector('[aria-label="Reihe A, Platz 1, ausgewählt"]')).not.toBeNull();
    expect(root.querySelector('[aria-label="Reihe B, Platz 2, ausgewählt"]')).not.toBeNull();
  });

  it('zeigt "Alle Plätze sind belegt." statt eines Grids, wenn keine Zelle frei ist', () => {
    TestBed.configureTestingModule({ imports: [SeatMapGrid], providers: [BookingSelectionService] });
    const fixture = TestBed.createComponent(SeatMapGrid);
    fixture.componentRef.setInput('seatMap', {
      rowLabels: ['A'],
      columnCount: 2,
      aisleColumns: [],
      rows: [
        {
          rowLabel: 'A',
          cells: [
            { columnNumber: 1, status: 'occupied' },
            { columnNumber: 2, status: 'occupied' }
          ]
        }
      ]
    } satisfies SeatMap);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    expect(root.querySelector('[role="grid"]')).toBeNull();
    expect(root.querySelector('.seat-map__empty')?.textContent).toContain('Alle Plätze sind belegt.');
  });

  it('rendert einen unbekannten Zellstatus inert statt als klickbaren freien Platz', () => {
    TestBed.configureTestingModule({ imports: [SeatMapGrid], providers: [BookingSelectionService] });
    const fixture = TestBed.createComponent(SeatMapGrid);
    fixture.componentRef.setInput('seatMap', {
      rowLabels: ['A'],
      columnCount: 2,
      aisleColumns: [],
      rows: [
        {
          rowLabel: 'A',
          cells: [
            { columnNumber: 1, status: 'reserved' as unknown as SeatStatus },
            { columnNumber: 2, status: 'free' }
          ]
        }
      ]
    } satisfies SeatMap);
    document.body.appendChild(fixture.nativeElement);
    fixture.detectChanges();
    const root = fixture.nativeElement as HTMLElement;

    const unknownCell = root.querySelector('.seat-cell--unknown');
    expect(unknownCell).not.toBeNull();
    expect(unknownCell?.tagName).not.toBe('BUTTON');
  });
});
