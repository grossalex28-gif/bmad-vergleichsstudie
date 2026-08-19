import { Component, inject, OnInit, output, signal } from '@angular/core';

import { CategoryNavItem, CategorySelection } from '../../../core/models/category.model';
import { CategoryService } from '../../../core/services/category.service';

@Component({
  selector: 'app-category-nav',
  imports: [],
  templateUrl: './category-nav.html',
  styleUrl: './category-nav.scss'
})
export class CategoryNav implements OnInit {
  private readonly categoryService = inject(CategoryService);

  readonly categories = signal<CategoryNavItem[]>([]);
  readonly selection = signal<CategorySelection>({ categoryId: null, subcategoryId: null });
  readonly error = signal(false);

  readonly selectionChange = output<CategorySelection>();

  ngOnInit(): void {
    this.categoryService.getCategories().subscribe({
      next: (categories) => this.categories.set(categories),
      error: () => this.error.set(true)
    });
  }

  selectAll(): void {
    const selection: CategorySelection = { categoryId: null, subcategoryId: null };
    this.selection.set(selection);
    this.selectionChange.emit(selection);
  }

  selectCategory(categoryId: string): void {
    const selection: CategorySelection = { categoryId, subcategoryId: null };
    this.selection.set(selection);
    this.selectionChange.emit(selection);
  }

  selectSubcategory(categoryId: string, subcategoryId: string): void {
    const selection: CategorySelection = { categoryId, subcategoryId };
    this.selection.set(selection);
    this.selectionChange.emit(selection);
  }
}
