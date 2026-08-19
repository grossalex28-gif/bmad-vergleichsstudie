import { DecimalPipe } from '@angular/common';
import { Component, computed, inject, signal } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { debounceTime, distinctUntilChanged, switchMap } from 'rxjs';
import { CategoryService } from '../../core/services/category.service';
import { ProductService } from '../../core/services/product.service';
import { Category, SubCategory } from '../../core/models/category.model';
import { ProductQuery, ProductSort } from '../../core/models/product.model';

const EMPTY_LIST = { items: [], page: 1, pageSize: 12, totalCount: 0, totalPages: 0 };

@Component({
  selector: 'app-product-list',
  imports: [FormsModule, RouterLink, DecimalPipe],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss',
})
export class ProductList {
  private readonly categoryService = inject(CategoryService);
  private readonly productService = inject(ProductService);

  protected readonly categories = toSignal(this.categoryService.getAll(), { initialValue: [] as Category[] });

  protected readonly selectedCategoryId = signal<string | undefined>(undefined);
  protected readonly selectedSubCategoryId = signal<string | undefined>(undefined);
  protected readonly searchInput = signal('');
  protected readonly sort = signal<ProductSort>('NameAsc');
  protected readonly page = signal(1);
  protected readonly propertyInputs = signal<Record<string, string>>({});
  protected readonly appliedProperties = signal<Record<string, string>>({});

  protected readonly selectedSubCategory = computed<SubCategory | undefined>(() => {
    const subCategoryId = this.selectedSubCategoryId();
    if (!subCategoryId) {
      return undefined;
    }
    for (const category of this.categories()) {
      const match = category.subCategories.find((s) => s.id === subCategoryId);
      if (match) {
        return match;
      }
    }
    return undefined;
  });

  private readonly debouncedSearch = toSignal(
    toObservable(this.searchInput).pipe(debounceTime(300), distinctUntilChanged()),
    { initialValue: '' },
  );

  private readonly query = computed<ProductQuery>(() => ({
    categoryId: this.selectedCategoryId(),
    subCategoryId: this.selectedSubCategoryId(),
    search: this.debouncedSearch() || undefined,
    sort: this.sort(),
    page: this.page(),
    properties: this.appliedProperties(),
  }));

  protected readonly result = toSignal(
    toObservable(this.query).pipe(switchMap((query) => this.productService.getList(query))),
    { initialValue: EMPTY_LIST },
  );

  protected selectCategory(category: Category): void {
    this.selectedCategoryId.set(category.id);
    this.selectedSubCategoryId.set(undefined);
    this.resetFilters();
  }

  protected selectSubCategory(category: Category, subCategory: SubCategory): void {
    this.selectedCategoryId.set(category.id);
    this.selectedSubCategoryId.set(subCategory.id);
    this.resetFilters();
  }

  protected clearCategoryFilter(): void {
    this.selectedCategoryId.set(undefined);
    this.selectedSubCategoryId.set(undefined);
    this.resetFilters();
  }

  protected onSortChange(sort: ProductSort): void {
    this.sort.set(sort);
    this.page.set(1);
  }

  protected applyPropertyFilters(): void {
    const cleaned = Object.fromEntries(
      Object.entries(this.propertyInputs()).filter(([, value]) => value?.trim()),
    );
    this.appliedProperties.set(cleaned);
    this.page.set(1);
  }

  protected updatePropertyInput(name: string, value: string): void {
    this.propertyInputs.update((current) => ({ ...current, [name]: value }));
  }

  protected goToPage(page: number): void {
    if (page < 1 || page > (this.result().totalPages || 1)) {
      return;
    }
    this.page.set(page);
  }

  private resetFilters(): void {
    this.propertyInputs.set({});
    this.appliedProperties.set({});
    this.page.set(1);
  }
}
