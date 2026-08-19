import { TestBed } from '@angular/core/testing';
import { Sitzplan } from '../../core/models/veranstaltung.model';
import { SitzplanComponent } from './sitzplan';

function erstelleSitzplan(): Sitzplan {
  return {
    raumName: 'Testsaal',
    reihen: ['A', 'B'],
    spalten: 3,
    gangSpalten: [2],
    gangHinweis: 'Mittelgang',
    belegtePlaetze: [{ reihe: 'A', spalte: 1 }]
  };
}

describe('SitzplanComponent', () => {
  async function erstelleComponent() {
    await TestBed.configureTestingModule({ imports: [SitzplanComponent] }).compileComponents();
    const fixture = TestBed.createComponent(SitzplanComponent);
    fixture.componentRef.setInput('sitzplan', erstelleSitzplan());
    fixture.detectChanges();
    return fixture;
  }

  it('rendert für jede Reihe nur die Sitzplätze, nicht die Gangspalte', async () => {
    const fixture = await erstelleComponent();
    const buttons = fixture.nativeElement.querySelectorAll('button.sitz');
    // 2 Reihen x 2 Sitzplätze (Spalte 2 ist Gang) = 4 Buttons
    expect(buttons.length).toBe(4);
  });

  it('markiert einen belegten Sitzplatz als nicht klickbar', async () => {
    const fixture = await erstelleComponent();
    const belegterButton = Array.from<HTMLButtonElement>(
      fixture.nativeElement.querySelectorAll('button.sitz')
    ).find((b) => b.textContent?.trim() === '1' && b.classList.contains('sitz--belegt'));

    expect(belegterButton).toBeTruthy();
    expect(belegterButton!.disabled).toBe(true);
  });

  it('emittiert sitzplatzUmschalten bei Klick auf einen freien Sitzplatz', async () => {
    const fixture = await erstelleComponent();
    const emitted: unknown[] = [];
    fixture.componentInstance.sitzplatzUmschalten.subscribe((p) => emitted.push(p));

    const freierButton = Array.from<HTMLButtonElement>(
      fixture.nativeElement.querySelectorAll('button.sitz')
    ).find((b) => !b.disabled);
    freierButton!.click();

    // Reihenfolge im DOM: A1 (belegt), A3 (frei, erster klickbarer Button), B1, B3.
    expect(emitted).toEqual([{ reihe: 'A', spalte: 3 }]);
  });

  it('zeigt bereits ausgewählte Sitzplätze mit eigenem Status an', async () => {
    const fixture = await erstelleComponent();
    fixture.componentRef.setInput('ausgewaehlt', [{ reihe: 'A', spalte: 3 }]);
    fixture.detectChanges();

    const ausgewaehlterButton = Array.from<HTMLButtonElement>(
      fixture.nativeElement.querySelectorAll('button.sitz')
    ).find((b) => b.classList.contains('sitz--ausgewaehlt'));

    expect(ausgewaehlterButton?.textContent?.trim()).toBe('3');
  });
});
