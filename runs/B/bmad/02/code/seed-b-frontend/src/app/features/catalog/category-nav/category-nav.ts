import { Component, input, output } from '@angular/core';

import { CategorySummary } from '../product.model';

@Component({
  selector: 'app-category-nav',
  templateUrl: './category-nav.html'
})
export class CategoryNav {
  categories = input.required<CategorySummary[]>();

  categorySelected = output<string>();
  subcategorySelected = output<string>();
  selectionCleared = output<void>();
}
