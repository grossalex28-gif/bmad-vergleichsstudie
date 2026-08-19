import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PagedResult, Product, ProductListItem, ProductSortBy, ProductSortDirection, RatingSummary } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getProducts(
    page: number,
    categoryId?: string | null,
    subcategoryId?: string | null,
    sortBy?: ProductSortBy,
    sortDirection?: ProductSortDirection,
    search?: string | null,
    attributeFilters?: { name: string; value: string }[] | null
  ): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams().set('page', page);
    if (categoryId) {
      params = params.set('categoryId', categoryId);
    }
    if (subcategoryId) {
      params = params.set('subcategoryId', subcategoryId);
    }
    if (sortBy) {
      params = params.set('sortBy', sortBy);
    }
    if (sortDirection) {
      params = params.set('sortDirection', sortDirection);
    }
    const trimmedSearch = search?.trim();
    if (trimmedSearch) {
      params = params.set('search', trimmedSearch);
    }
    if (attributeFilters && attributeFilters.length > 0) {
      for (const filter of attributeFilters) {
        params = params.append('attr', `${filter.name}:${filter.value}`);
      }
    }
    return this.http.get<PagedResult<ProductListItem>>('/api/products', { params });
  }

  getProduct(id: string): Observable<Product> {
    return this.http.get<Product>(`/api/products/${encodeURIComponent(id)}`);
  }

  submitRating(productId: string, authorName: string, value: number): Observable<RatingSummary> {
    return this.http.post<RatingSummary>(`/api/products/${encodeURIComponent(productId)}/ratings`, { authorName, value });
  }
}
