import { DecimalPipe } from '@angular/common';
import { Component, computed, inject } from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { switchMap } from 'rxjs';

import { Category } from '../../core/models/category.model';
import { SortOption } from '../../core/models/product.model';
import { CategoryService } from '../../core/services/category.service';
import { ProductService } from '../../core/services/product.service';

@Component({
  selector: 'app-product-list',
  imports: [RouterLink, DecimalPipe],
  templateUrl: './product-list.html',
  styleUrl: './product-list.scss'
})
export class ProductList {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly productService = inject(ProductService);
  private readonly categoryService = inject(CategoryService);

  protected readonly categories = toSignal(this.categoryService.getCategories(), {
    initialValue: [] as Category[]
  });

  private readonly queryParamMap = toSignal(this.route.queryParamMap, { requireSync: true });

  protected readonly search = computed(() => this.queryParamMap().get('search') ?? '');
  protected readonly categoryId = computed(() => this.queryParamMap().get('categoryId') ?? '');
  protected readonly subcategoryId = computed(() => this.queryParamMap().get('subcategoryId') ?? '');
  protected readonly sort = computed<SortOption>(() => (this.queryParamMap().get('sort') as SortOption) ?? 'name_asc');
  protected readonly page = computed(() => Number(this.queryParamMap().get('page') ?? '1'));

  protected readonly eigenschaften = computed(() => {
    const result: Record<string, string> = {};
    for (const key of this.queryParamMap().keys) {
      if (key.startsWith('eig.')) {
        result[key.slice(4)] = this.queryParamMap().get(key) ?? '';
      }
    }
    return result;
  });

  protected readonly selectedSubcategoryProps = computed(() => {
    const subId = this.subcategoryId();
    if (!subId) {
      return [];
    }
    for (const category of this.categories()) {
      const subcategory = category.unterkategorien.find((s) => s.id === subId);
      if (subcategory) {
        return subcategory.eigenschaften;
      }
    }
    return [];
  });

  private readonly queryKey = computed(() => ({
    search: this.search(),
    categoryId: this.categoryId(),
    subcategoryId: this.subcategoryId(),
    sort: this.sort(),
    page: this.page(),
    eigenschaften: this.eigenschaften()
  }));

  protected readonly result = toSignal(
    toObservable(this.queryKey).pipe(switchMap((query) => this.productService.getProducts(query))),
    { initialValue: null }
  );

  protected clearCategory(): void {
    this.updateParams({ categoryId: null, subcategoryId: null, page: null }, true);
  }

  protected selectCategory(categoryId: string): void {
    this.updateParams({ categoryId, subcategoryId: null, page: null }, true);
  }

  protected selectSubcategory(categoryId: string, subcategoryId: string): void {
    this.updateParams({ categoryId, subcategoryId, page: null }, true);
  }

  protected onSearchSubmit(term: string): void {
    this.updateParams({ search: term || null, page: null });
  }

  protected onSortChange(sort: string): void {
    this.updateParams({ sort, page: null });
  }

  protected onEigenschaftChange(name: string, value: string): void {
    this.updateParams({ [`eig.${name}`]: value || null, page: null });
  }

  protected goToPage(page: number): void {
    this.updateParams({ page: page <= 1 ? null : String(page) });
  }

  private updateParams(patch: Record<string, string | null>, resetEigenschaften = false): void {
    const current = { ...this.route.snapshot.queryParams };
    if (resetEigenschaften) {
      for (const key of Object.keys(current)) {
        if (key.startsWith('eig.')) {
          delete current[key];
        }
      }
    }

    const next: Record<string, string | null> = { ...current, ...patch };
    for (const key of Object.keys(next)) {
      if (next[key] === null || next[key] === '') {
        delete next[key];
      }
    }

    this.router.navigate([], { relativeTo: this.route, queryParams: next });
  }
}
