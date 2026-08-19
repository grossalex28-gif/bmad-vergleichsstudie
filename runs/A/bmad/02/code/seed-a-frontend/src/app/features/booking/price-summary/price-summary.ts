import { Component, inject, input } from '@angular/core';
import { CurrencyPipe } from '@angular/common';

import { PriceCategory } from '../../../core/api/event-detail';
import { BookingSelectionService } from '../booking-selection.service';
import { PriceCategoryList } from '../price-category-list/price-category-list';

@Component({
  selector: 'app-price-summary',
  imports: [PriceCategoryList, CurrencyPipe],
  templateUrl: './price-summary.html',
  styleUrl: './price-summary.scss'
})
export class PriceSummary {
  readonly priceCategories = input.required<PriceCategory[]>();

  protected readonly selection = inject(BookingSelectionService);
}
