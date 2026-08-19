import { Component, inject, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

import { PriceCategory } from '../../../core/api/event-detail';
import { BookingSelectionService } from '../booking-selection.service';

@Component({
  selector: 'app-price-category-list',
  imports: [CurrencyPipe],
  templateUrl: './price-category-list.html',
  styleUrl: './price-category-list.scss'
})
export class PriceCategoryList {
  readonly priceCategories = input.required<PriceCategory[]>();

  protected readonly selection = inject(BookingSelectionService);

  onCategoryChange(rowLabel: string, columnNumber: number, value: string): void {
    if (value === '') {
      return;
    }
    this.selection.assignCategory(rowLabel, columnNumber, Number(value));
  }
}
