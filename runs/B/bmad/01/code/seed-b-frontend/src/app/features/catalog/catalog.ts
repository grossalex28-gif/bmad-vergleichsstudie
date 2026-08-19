import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { PagedResult, ProductListItem, ProductsApi } from '../../core/api/products.api';
import { CategoryNav, CategorySelection } from './category-nav/category-nav';
import { PropertyFilter, PropertyFilterSelection } from './property-filter/property-filter';

@Component({
  selector: 'app-catalog',
  imports: [DecimalPipe, RouterLink, CategoryNav, PropertyFilter],
  templateUrl: './catalog.html',
  styleUrl: './catalog.scss',
})
export class Catalog implements OnInit {
  private readonly productsApi = inject(ProductsApi);

  protected readonly page = signal(1);
  protected readonly result = signal<PagedResult<ProductListItem> | null>(null);
  protected readonly loading = signal(false);
  protected readonly error = signal(false);
  protected readonly selectedCategoryId = signal<string | undefined>(undefined);
  protected readonly selectedSubcategoryId = signal<string | undefined>(undefined);
  protected readonly sortBy = signal<'price' | 'name' | 'viewCount' | undefined>(undefined);
  protected readonly sortDir = signal<'asc' | 'desc' | undefined>(undefined);
  protected readonly q = signal<string | undefined>(undefined);
  protected readonly selectedProperties = signal<PropertyFilterSelection>({});
  protected readonly sortSelectValue = computed(() => {
    const sortBy = this.sortBy();
    const sortDir = this.sortDir();
    return sortBy && sortDir ? `${sortBy}-${sortDir}` : '';
  });
  protected readonly propertyQueryParams = computed(() => {
    const selection = this.selectedProperties();
    const entries = Object.entries(selection).flatMap(([name, values]) => values.map((value) => `${name}:${value}`));
    return entries.length > 0 ? entries : undefined;
  });

  ngOnInit(): void {
    this.loadPage(1);
  }

  protected goToPage(page: number): void {
    if (this.loading() || page < 1 || (this.result() && page > this.result()!.pageCount)) {
      return;
    }
    this.loadPage(page);
  }

  protected onCategorySelectionChange(selection: CategorySelection): void {
    if (this.loading()) {
      return;
    }
    this.selectedCategoryId.set(selection.categoryId ?? undefined);
    this.selectedSubcategoryId.set(selection.subcategoryId ?? undefined);
    this.selectedProperties.set({});
    this.loadPage(1);
  }

  protected onPropertyFilterChange(selection: PropertyFilterSelection): void {
    if (this.loading()) {
      return;
    }
    this.selectedProperties.set(selection);
    this.loadPage(1);
  }

  protected onSortSelectionChange(value: string): void {
    if (this.loading()) {
      return;
    }
    switch (value) {
      case 'price-asc':
        this.sortBy.set('price');
        this.sortDir.set('asc');
        break;
      case 'price-desc':
        this.sortBy.set('price');
        this.sortDir.set('desc');
        break;
      case 'name-asc':
        this.sortBy.set('name');
        this.sortDir.set('asc');
        break;
      case 'name-desc':
        this.sortBy.set('name');
        this.sortDir.set('desc');
        break;
      case 'viewCount-desc':
        this.sortBy.set('viewCount');
        this.sortDir.set('desc');
        break;
      default:
        this.sortBy.set(undefined);
        this.sortDir.set(undefined);
    }
    this.loadPage(1);
  }

  protected onSearchSubmit(term: string): void {
    if (this.loading()) {
      return;
    }
    const trimmed = term.trim();
    this.q.set(trimmed || undefined);
    this.loadPage(1);
  }

  private loadPage(page: number): void {
    this.loading.set(true);
    this.error.set(false);
    this.productsApi
      .getProducts(
        page,
        this.selectedCategoryId(),
        this.selectedSubcategoryId(),
        this.sortBy(),
        this.sortDir(),
        this.q(),
        this.propertyQueryParams(),
      )
      .subscribe({
        next: (result) => {
          this.page.set(result.page);
          this.result.set(result);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
          this.error.set(true);
        },
      });
  }
}
