import { Component, computed, input, model } from '@angular/core';

@Component({
  selector: 'app-filter-bar',
  imports: [],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.scss'
})
export class FilterBar {
  readonly von = model<string | null>(null);
  readonly bis = model<string | null>(null);
  readonly spielstaetteId = model<string | null>(null);

  readonly spielstaetten = input<{ id: string; name: string }[]>([]);

  protected readonly filterAktiv = computed(
    () => this.von() !== null || this.bis() !== null || this.spielstaetteId() !== null
  );

  protected zuruecksetzen(): void {
    this.von.set(null);
    this.bis.set(null);
    this.spielstaetteId.set(null);
  }
}
