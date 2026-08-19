import { Component, OnDestroy, OnInit, computed, inject, signal } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { DecimalPipe } from '@angular/common';
import { CategoryService } from '../../core/services/category.service';
import { ProductService } from '../../core/services/product.service';
import { Category, Subcategory } from '../../core/models/category.model';
import { PagedResult, ProductListItem, SortBy, SortDir } from '../../core/models/product.model';

@Component({
  selector: 'app-product-list',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-list.component.html',
  styleUrl: './product-list.component.scss',
})
export class ProductListComponent implements OnInit, OnDestroy {
  private readonly categoryService = inject(CategoryService);
  private readonly productService = inject(ProductService);
  private searchDebounce: ReturnType<typeof setTimeout> | undefined;

  readonly categories = signal<Category[]>([]);
  readonly selectedCategoryId = signal<number | null>(null);
  readonly selectedSubcategoryId = signal<number | null>(null);
  readonly searchTerm = signal('');
  readonly sortBy = signal<SortBy>('name');
  readonly sortDir = signal<SortDir>('asc');
  readonly page = signal(1);
  readonly propertyFilters = signal<Record<string, string>>({});

  readonly result = signal<PagedResult<ProductListItem> | null>(null);
  readonly loading = signal(false);
  readonly error = signal<string | null>(null);

  readonly selectedSubcategory = computed<Subcategory | null>(() => {
    const catId = this.selectedCategoryId();
    const subId = this.selectedSubcategoryId();
    if (subId == null) return null;
    const cat = this.categories().find((c) => c.id === catId);
    return cat?.subcategories.find((s) => s.id === subId) ?? null;
  });

  readonly totalPages = computed(() => {
    const r = this.result();
    if (!r) return 1;
    return Math.max(1, Math.ceil(r.totalCount / r.pageSize));
  });

  ngOnInit(): void {
    this.categoryService.getCategories().subscribe((cats) => this.categories.set(cats));
    this.loadProducts();
  }

  ngOnDestroy(): void {
    clearTimeout(this.searchDebounce);
  }

  loadProducts(): void {
    this.loading.set(true);
    this.error.set(null);
    this.productService
      .getProducts({
        categoryId: this.selectedCategoryId() ?? undefined,
        subcategoryId: this.selectedSubcategoryId() ?? undefined,
        search: this.searchTerm() || undefined,
        sortBy: this.sortBy(),
        sortDir: this.sortDir(),
        page: this.page(),
        properties: this.propertyFilters(),
      })
      .subscribe({
        next: (res) => {
          this.result.set(res);
          this.loading.set(false);
        },
        error: () => {
          this.error.set('Produkte konnten nicht geladen werden.');
          this.loading.set(false);
        },
      });
  }

  selectCategory(categoryId: number | null): void {
    this.selectedCategoryId.set(categoryId);
    this.selectedSubcategoryId.set(null);
    this.propertyFilters.set({});
    this.page.set(1);
    this.loadProducts();
  }

  selectSubcategory(categoryId: number, subcategoryId: number): void {
    this.selectedCategoryId.set(categoryId);
    this.selectedSubcategoryId.set(subcategoryId);
    this.propertyFilters.set({});
    this.page.set(1);
    this.loadProducts();
  }

  onSearchChange(value: string): void {
    this.searchTerm.set(value);
    clearTimeout(this.searchDebounce);
    this.searchDebounce = setTimeout(() => {
      this.page.set(1);
      this.loadProducts();
    }, 300);
  }

  onSortChange(value: string): void {
    const [by, dir] = value.split(':') as [SortBy, SortDir];
    this.sortBy.set(by);
    this.sortDir.set(dir);
    this.page.set(1);
    this.loadProducts();
  }

  onPropertyFilterChange(name: string, value: string): void {
    this.propertyFilters.update((current) => ({ ...current, [name]: value }));
    this.page.set(1);
    this.loadProducts();
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.page.set(page);
    this.loadProducts();
  }
}
