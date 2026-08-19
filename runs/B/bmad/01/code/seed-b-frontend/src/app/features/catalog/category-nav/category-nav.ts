import { Component, OnInit, inject, input, output, signal } from '@angular/core';

import { Category, CategoriesApi } from '../../../core/api/categories.api';

export interface CategorySelection {
  categoryId: string | null;
  subcategoryId: string | null;
}

@Component({
  selector: 'app-category-nav',
  imports: [],
  templateUrl: './category-nav.html',
  styleUrl: './category-nav.scss',
})
export class CategoryNav implements OnInit {
  private readonly categoriesApi = inject(CategoriesApi);

  readonly selection = input<CategorySelection>({ categoryId: null, subcategoryId: null });
  readonly disabled = input(false);
  readonly selectionChange = output<CategorySelection>();

  protected readonly categories = signal<Category[]>([]);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);

  ngOnInit(): void {
    this.loading.set(true);
    this.error.set(false);
    this.categoriesApi.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set(true);
      },
    });
  }

  protected selectAll(): void {
    this.selectionChange.emit({ categoryId: null, subcategoryId: null });
  }

  protected selectCategory(categoryId: string): void {
    this.selectionChange.emit({ categoryId, subcategoryId: null });
  }

  protected selectSubcategory(categoryId: string, subcategoryId: string): void {
    this.selectionChange.emit({ categoryId, subcategoryId });
  }
}
