import { Component, input, output } from '@angular/core';

export interface SortSelection {
  sortBy: 'price' | 'name' | 'viewCount';
  sortDirection: 'asc' | 'desc';
}

@Component({
  selector: 'app-sort-select',
  templateUrl: './sort-select.html'
})
export class SortSelect {
  disabled = input(false);
  sortChanged = output<SortSelection | null>();

  protected onChange(value: string): void {
    if (!value) {
      this.sortChanged.emit(null);
      return;
    }
    const [sortBy, sortDirection] = value.split('-') as [SortSelection['sortBy'], SortSelection['sortDirection']];
    this.sortChanged.emit({ sortBy, sortDirection });
  }
}
