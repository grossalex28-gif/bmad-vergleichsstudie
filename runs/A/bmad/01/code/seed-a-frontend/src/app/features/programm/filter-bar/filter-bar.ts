import { Component, input, model } from '@angular/core';

import { VenueListItem } from '../../../core/models/venue.model';

@Component({
  selector: 'app-filter-bar',
  imports: [],
  templateUrl: './filter-bar.html',
  styleUrl: './filter-bar.scss'
})
export class FilterBar {
  readonly von = model<string | null>(null);
  readonly bis = model<string | null>(null);
  readonly venueId = model<string | null>(null);
  readonly venues = input<VenueListItem[]>([]);

  onVonChange(value: string): void {
    this.von.set(value === '' ? null : value);
  }

  onBisChange(value: string): void {
    this.bis.set(value === '' ? null : value);
  }

  onVenueChange(value: string): void {
    this.venueId.set(value === '' ? null : value);
  }
}
