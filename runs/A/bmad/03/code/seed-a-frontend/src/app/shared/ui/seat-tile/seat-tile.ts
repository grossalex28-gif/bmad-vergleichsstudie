import { Component, computed, input, output } from '@angular/core';

@Component({
  selector: 'app-seat-tile',
  imports: [],
  templateUrl: './seat-tile.html',
  styleUrl: './seat-tile.scss',
})
export class SeatTile {
  readonly reihe = input.required<string>();
  readonly spalte = input.required<number>();
  readonly status = input.required<'Frei' | 'Belegt'>();
  readonly ausgewaehlt = input<boolean>(false);
  readonly fokussierbar = input<boolean>(false);

  readonly auswahlUmschalten = output<void>();

  protected readonly code = computed(() => `${this.reihe()}${this.spalte()}`);
  protected readonly ariaLabel = computed(() => {
    if (this.status() === 'Belegt') {
      return `Reihe ${this.reihe()}, Platz ${this.spalte()}, belegt`;
    }
    return `Reihe ${this.reihe()}, Platz ${this.spalte()}, ${this.ausgewaehlt() ? 'ausgewählt' : 'frei'}`;
  });

  protected onKlick(): void {
    if (this.status() === 'Belegt') return;
    this.auswahlUmschalten.emit();
  }
}
