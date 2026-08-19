import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../core/api-client';
import { CategorySummary, ProductListResponse } from './product.model';

@Injectable({ providedIn: 'root' })
export class CatalogApiService {
  private apiClient = inject(ApiClient);

  getProducts(
    page: number,
    categoryId?: string | null,
    subcategoryId?: string | null,
    sortBy?: string | null,
    sortDirection?: string | null,
    search?: string | null,
    propertyFilters?: Record<string, string> | null
  ): Observable<ProductListResponse> {
    const params: Record<string, string | number> = { page };
    if (subcategoryId) {
      params['subcategoryId'] = subcategoryId;
    } else if (categoryId) {
      params['categoryId'] = categoryId;
    }
    if (sortBy) {
      params['sortBy'] = sortBy;
    }
    if (sortDirection) {
      params['sortDirection'] = sortDirection;
    }
    if (search) {
      params['search'] = search;
    }
    if (propertyFilters) {
      for (const [name, value] of Object.entries(propertyFilters)) {
        params[`properties[${name}]`] = value;
      }
    }
    return this.apiClient.get<ProductListResponse>('/products', params);
  }

  getCategories(): Observable<CategorySummary[]> {
    return this.apiClient.get<CategorySummary[]>('/categories');
  }
}
