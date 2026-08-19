import { Component, computed, inject, signal } from '@angular/core';
import { RouterLink } from '@angular/router';

import { CatalogApiService } from '../catalog-api.service';
import { CategoryNav } from '../category-nav/category-nav';
import { CategorySummary, ProductSummary } from '../product.model';
import { PropertyFilters } from '../property-filters/property-filters';
import { SearchBox } from '../search-box/search-box';
import { SortSelect, SortSelection } from '../sort-select/sort-select';

@Component({
  selector: 'app-product-list',
  imports: [CategoryNav, SortSelect, SearchBox, PropertyFilters, RouterLink],
  templateUrl: './product-list.html'
})
export class ProductList {
  private catalogApi = inject(CatalogApiService);

  protected readonly page = signal(1);
  protected readonly items = signal<ProductSummary[]>([]);
  protected readonly totalCount = signal(0);
  protected readonly loading = signal(false);
  protected readonly pageSize = signal(0);
  protected readonly categories = signal<CategorySummary[]>([]);
  protected readonly selectedCategoryId = signal<string | null>(null);
  protected readonly selectedSubcategoryId = signal<string | null>(null);
  protected readonly selectedSortBy = signal<string | null>(null);
  protected readonly selectedSortDirection = signal<string | null>(null);
  protected readonly selectedSearch = signal<string | null>(null);
  protected readonly selectedPropertyFilters = signal<Record<string, string> | null>(null);

  protected readonly selectedSubcategoryProperties = computed<string[]>(() => {
    const subcategoryId = this.selectedSubcategoryId();
    if (!subcategoryId) {
      return [];
    }
    for (const category of this.categories()) {
      const match = category.subcategories.find((s) => s.id === subcategoryId);
      if (match) {
        return match.properties;
      }
    }
    return [];
  });

  protected readonly totalPages = computed(() => Math.max(1, Math.ceil(this.totalCount() / (this.pageSize() || 1))));
  protected readonly hasPreviousPage = computed(() => this.page() > 1);
  protected readonly hasNextPage = computed(() => this.page() * this.pageSize() < this.totalCount());

  constructor() {
    this.loadPage(this.page());
    this.loadCategories();
  }

  protected goToPreviousPage(): void {
    if (this.hasPreviousPage() && !this.loading()) {
      this.loadPage(this.page() - 1);
    }
  }

  protected goToNextPage(): void {
    if (this.hasNextPage() && !this.loading()) {
      this.loadPage(this.page() + 1);
    }
  }

  protected selectCategory(categoryId: string): void {
    if (this.loading()) {
      return;
    }
    this.selectedCategoryId.set(categoryId);
    this.selectedSubcategoryId.set(null);
    this.selectedPropertyFilters.set(null);
    this.loadPage(1);
  }

  protected selectSubcategory(subcategoryId: string): void {
    if (this.loading()) {
      return;
    }
    this.selectedCategoryId.set(null);
    this.selectedSubcategoryId.set(subcategoryId);
    this.selectedPropertyFilters.set(null);
    this.loadPage(1);
  }

  protected clearSelection(): void {
    if (this.loading()) {
      return;
    }
    this.selectedCategoryId.set(null);
    this.selectedSubcategoryId.set(null);
    this.selectedPropertyFilters.set(null);
    this.loadPage(1);
  }

  protected onSortChanged(sort: SortSelection | null): void {
    if (this.loading()) {
      return;
    }
    this.selectedSortBy.set(sort?.sortBy ?? null);
    this.selectedSortDirection.set(sort?.sortDirection ?? null);
    this.loadPage(1);
  }

  protected onSearchChanged(search: string | null): void {
    if (this.loading()) {
      return;
    }
    this.selectedSearch.set(search);
    this.loadPage(1);
  }

  protected onPropertyFiltersChanged(filters: Record<string, string> | null): void {
    if (this.loading()) {
      return;
    }
    this.selectedPropertyFilters.set(filters);
    this.loadPage(1);
  }

  private loadCategories(): void {
    this.catalogApi.getCategories().subscribe({
      next: (categories) => {
        this.categories.set(categories);
      },
      error: () => {
        this.categories.set([]);
      }
    });
  }

  private loadPage(page: number): void {
    this.loading.set(true);
    this.catalogApi
      .getProducts(
        page,
        this.selectedCategoryId(),
        this.selectedSubcategoryId(),
        this.selectedSortBy(),
        this.selectedSortDirection(),
        this.selectedSearch(),
        this.selectedPropertyFilters()
      )
      .subscribe({
        next: (response) => {
          this.page.set(response.page);
          this.items.set(response.items);
          this.totalCount.set(response.totalCount);
          this.pageSize.set(response.pageSize);
          this.loading.set(false);
        },
        error: () => {
          this.loading.set(false);
        }
      });
  }
}
