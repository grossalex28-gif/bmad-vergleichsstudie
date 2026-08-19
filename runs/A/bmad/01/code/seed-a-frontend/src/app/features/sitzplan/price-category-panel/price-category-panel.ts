import { Component, input, output } from '@angular/core';

import { PriceCategory } from '../../../core/models/seat-map.model';
import { formatEuroAmount } from '../../../shared/format-currency';

@Component({
  selector: 'app-price-category-panel',
  imports: [],
  templateUrl: './price-category-panel.html',
  styleUrl: './price-category-panel.scss'
})
export class PriceCategoryPanel {
  readonly categories = input.required<PriceCategory[]>();
  readonly selectedCategoryId = input<string | null>(null);

  readonly categorySelected = output<string>();

  protected readonly formatEuroAmount = formatEuroAmount;
}
