import { TestBed } from '@angular/core/testing';

import { Seat } from './seat';

describe('Seat', () => {
  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [Seat]
    }).compileComponents();
  });

  it('renders the seat symbol and not the occupied stripe pattern when free', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat--free')).toBeTruthy();
    expect(compiled.querySelector('.seat--occupied')).toBeFalsy();
    expect(compiled.querySelector('.seat__symbol')).toBeTruthy();
    expect(compiled.querySelector('.seat__cross')).toBeFalsy();
  });

  it('renders the occupied stripe pattern and cross icon and not the seat symbol when occupied', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Occupied');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat--occupied')).toBeTruthy();
    expect(compiled.querySelector('.seat--free')).toBeFalsy();
    expect(compiled.querySelector('.seat__cross')).toBeTruthy();
    expect(compiled.querySelector('.seat__symbol')).toBeFalsy();
  });

  it('sets an aria-label with row, column and the German free-state text', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat')?.getAttribute('aria-label')).toBe('Reihe B, Platz 3, frei');
  });

  it('sets an aria-label with row, column and the German occupied-state text', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Occupied');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat')?.getAttribute('aria-label')).toBe('Reihe B, Platz 3, belegt');
  });

  it('emits toggled exactly once when a free seat is clicked', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.detectChanges();

    const onToggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(onToggled);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.seat') as HTMLElement).click();

    expect(onToggled).toHaveBeenCalledTimes(1);
  });

  it('does not emit toggled when an occupied seat is clicked', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Occupied');
    fixture.detectChanges();

    const onToggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(onToggled);

    const compiled = fixture.nativeElement as HTMLElement;
    (compiled.querySelector('.seat') as HTMLElement).click();

    expect(onToggled).not.toHaveBeenCalled();
  });

  it('renders the selected state with the check icon and an aria-label ending on "ausgewählt"', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.componentRef.setInput('selected', true);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat--selected')).toBeTruthy();
    expect(compiled.querySelector('.seat--free')).toBeFalsy();
    expect(compiled.querySelector('.seat__check')).toBeTruthy();
    expect(compiled.querySelector('.seat__symbol')).toBeFalsy();
    expect(compiled.querySelector('.seat')?.getAttribute('aria-label')).toBe('Reihe B, Platz 3, ausgewählt');
  });

  it('has no tabindex attribute when occupied, tabindex 0 when focused and free, tabindex -1 when unfocused and free', () => {
    const occupied = TestBed.createComponent(Seat);
    occupied.componentRef.setInput('row', 'B');
    occupied.componentRef.setInput('column', 3);
    occupied.componentRef.setInput('status', 'Occupied');
    occupied.detectChanges();
    expect((occupied.nativeElement as HTMLElement).querySelector('.seat')?.hasAttribute('tabindex')).toBe(false);

    const focused = TestBed.createComponent(Seat);
    focused.componentRef.setInput('row', 'B');
    focused.componentRef.setInput('column', 3);
    focused.componentRef.setInput('status', 'Free');
    focused.componentRef.setInput('focused', true);
    focused.detectChanges();
    expect((focused.nativeElement as HTMLElement).querySelector('.seat')?.getAttribute('tabindex')).toBe('0');

    const unfocused = TestBed.createComponent(Seat);
    unfocused.componentRef.setInput('row', 'B');
    unfocused.componentRef.setInput('column', 3);
    unfocused.componentRef.setInput('status', 'Free');
    unfocused.componentRef.setInput('focused', false);
    unfocused.detectChanges();
    expect((unfocused.nativeElement as HTMLElement).querySelector('.seat')?.getAttribute('tabindex')).toBe('-1');
  });

  it('emits toggled when Enter is pressed on a focused free seat', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.componentRef.setInput('focused', true);
    fixture.detectChanges();

    const onToggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(onToggled);

    const compiled = fixture.nativeElement as HTMLElement;
    compiled.querySelector('.seat')?.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter', bubbles: true }));

    expect(onToggled).toHaveBeenCalledTimes(1);
  });

  it('emits toggled and prevents default scrolling when Space is pressed on a focused free seat', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.componentRef.setInput('focused', true);
    fixture.detectChanges();

    const onToggled = vi.fn();
    fixture.componentInstance.toggled.subscribe(onToggled);

    const compiled = fixture.nativeElement as HTMLElement;
    const spaceEvent = new KeyboardEvent('keydown', { key: ' ', bubbles: true, cancelable: true });
    compiled.querySelector('.seat')?.dispatchEvent(spaceEvent);

    expect(onToggled).toHaveBeenCalledTimes(1);
    expect(spaceEvent.defaultPrevented).toBe(true);
  });

  it('sets role="button" and aria-pressed on free/selected seats, and neither on occupied seats', () => {
    const free = TestBed.createComponent(Seat);
    free.componentRef.setInput('row', 'B');
    free.componentRef.setInput('column', 3);
    free.componentRef.setInput('status', 'Free');
    free.detectChanges();
    const freeSeat = (free.nativeElement as HTMLElement).querySelector('.seat');
    expect(freeSeat?.getAttribute('role')).toBe('button');
    expect(freeSeat?.getAttribute('aria-pressed')).toBe('false');

    const selected = TestBed.createComponent(Seat);
    selected.componentRef.setInput('row', 'B');
    selected.componentRef.setInput('column', 3);
    selected.componentRef.setInput('status', 'Free');
    selected.componentRef.setInput('selected', true);
    selected.detectChanges();
    expect((selected.nativeElement as HTMLElement).querySelector('.seat')?.getAttribute('aria-pressed')).toBe('true');

    const occupied = TestBed.createComponent(Seat);
    occupied.componentRef.setInput('row', 'B');
    occupied.componentRef.setInput('column', 3);
    occupied.componentRef.setInput('status', 'Occupied');
    occupied.detectChanges();
    const occupiedSeat = (occupied.nativeElement as HTMLElement).querySelector('.seat');
    expect(occupiedSeat?.hasAttribute('role')).toBe(false);
    expect(occupiedSeat?.hasAttribute('aria-pressed')).toBe(false);
  });

  it('moves DOM focus to the seat element when clicked, so keyboard navigation can continue from it', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    document.body.appendChild(compiled);
    try {
      const seatEl = compiled.querySelector('.seat') as HTMLElement;
      seatEl.click();
      expect(document.activeElement).toBe(seatEl);
    } finally {
      compiled.remove();
    }
  });

  it('appends the category name and formatted price to the aria-label when selected with a category assigned', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.componentRef.setInput('selected', true);
    fixture.componentRef.setInput('categoryName', 'Kategorie A');
    fixture.componentRef.setInput('categoryPrice', 32);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat')?.getAttribute('aria-label')).toBe('Reihe B, Platz 3, ausgewählt, Kategorie A, 32,00 €');
  });

  it('does not append a category suffix to the aria-label when the seat is not selected, even if category inputs are set', () => {
    const fixture = TestBed.createComponent(Seat);
    fixture.componentRef.setInput('row', 'B');
    fixture.componentRef.setInput('column', 3);
    fixture.componentRef.setInput('status', 'Free');
    fixture.componentRef.setInput('selected', false);
    fixture.componentRef.setInput('categoryName', 'Kategorie A');
    fixture.componentRef.setInput('categoryPrice', 32);
    fixture.detectChanges();

    const compiled = fixture.nativeElement as HTMLElement;
    expect(compiled.querySelector('.seat')?.getAttribute('aria-label')).toBe('Reihe B, Platz 3, frei');
  });
});
