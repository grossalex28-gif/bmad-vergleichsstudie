import { CurrencyPipe } from '@angular/common';
import { Component, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CategorySelection } from '../../../core/models/category.model';
import {
  PagedResult,
  ProductListItem,
  ProductSortBy,
  ProductSortDirection
} from '../../../core/models/product.model';
import { ProductService } from '../../../core/services/product.service';
import { CategoryNav } from '../category-nav/category-nav';

@Component({
  selector: 'app-product-list',
  imports: [CurrencyPipe, CategoryNav, RouterLink],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss'
})
export class ProductList implements OnInit {
  private readonly productService = inject(ProductService);

  readonly page = signal(1);
  readonly result = signal<PagedResult<ProductListItem> | null>(null);
  readonly error = signal(false);
  readonly categoryId = signal<string | null>(null);
  readonly subcategoryId = signal<string | null>(null);
  readonly sortBy = signal<ProductSortBy>('name');
  readonly sortDirection = signal<ProductSortDirection>('asc');
  readonly search = signal('');
  private readonly submittedSearch = signal('');
  readonly attributeFilterSelections = signal<Record<string, string>>({});

  private latestRequestId = 0;

  ngOnInit(): void {
    this.loadPage(1);
  }

  onCategorySelectionChange(selection: CategorySelection): void {
    this.attributeFilterSelections.set({});
    this.categoryId.set(selection.categoryId);
    this.subcategoryId.set(selection.subcategoryId);
    this.loadPage(1);
  }

  onAttributeFilterChange(name: string, event: Event): void {
    const value = (event.target as HTMLSelectElement).value;
    const current = { ...this.attributeFilterSelections() };
    if (value) {
      current[name] = value;
    } else {
      delete current[name];
    }
    this.attributeFilterSelections.set(current);
    this.loadPage(1);
  }

  onSortByChange(event: Event): void {
    this.sortBy.set((event.target as HTMLSelectElement).value as ProductSortBy);
    this.loadPage(1);
  }

  onSortDirectionChange(event: Event): void {
    this.sortDirection.set((event.target as HTMLSelectElement).value as ProductSortDirection);
    this.loadPage(1);
  }

  onSearchSubmit(): void {
    this.submittedSearch.set(this.search());
    this.loadPage(1);
  }

  onSearchInputChange(event: Event): void {
    this.search.set((event.target as HTMLInputElement).value);
  }

  loadPage(page: number): void {
    this.page.set(page);
    this.error.set(false);
    const requestId = ++this.latestRequestId;
    const attributeFilters = Object.entries(this.attributeFilterSelections()).map(([name, value]) => ({ name, value }));

    this.productService
      .getProducts(
        page,
        this.categoryId(),
        this.subcategoryId(),
        this.sortBy(),
        this.sortDirection(),
        this.submittedSearch(),
        attributeFilters
      )
      .subscribe({
        next: (result) => {
          if (this.latestRequestId === requestId) {
            this.result.set(result);
          }
        },
        error: () => {
          if (this.latestRequestId === requestId) {
            this.error.set(true);
          }
        }
      });
  }
}
