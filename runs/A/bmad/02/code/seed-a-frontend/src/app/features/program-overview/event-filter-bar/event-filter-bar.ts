import { Component, input, model } from '@angular/core';
import { DatePipe } from '@angular/common';

import { Venue } from '../../../core/api/venue';

@Component({
  selector: 'app-event-filter-bar',
  imports: [DatePipe],
  templateUrl: './event-filter-bar.html',
  styleUrl: './event-filter-bar.scss'
})
export class EventFilterBar {
  readonly von = model<string | null>(null);
  readonly bis = model<string | null>(null);
  readonly venueId = model<number | null>(null);
  readonly venues = input<Venue[]>([]);

  onVonChange(value: string): void {
    this.von.set(value || null);
  }

  onBisChange(value: string): void {
    this.bis.set(value || null);
  }

  onVenueChange(value: string): void {
    this.venueId.set(value ? Number(value) : null);
  }

  selectedVenueName(): string | undefined {
    return this.venues().find(v => v.id === this.venueId())?.name;
  }

  filterZuruecksetzen(): void {
    this.von.set(null);
    this.bis.set(null);
    this.venueId.set(null);
  }
}
