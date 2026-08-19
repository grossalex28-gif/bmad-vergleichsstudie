import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { PagedResult, ProductDetail, ProductListItem, RatingCreate, RatingResponse, SortOption } from '../models/product.model';

export interface ProductQueryParams {
  search?: string;
  categoryId?: string;
  subcategoryId?: string;
  sort?: SortOption;
  page?: number;
  eigenschaften?: Record<string, string>;
}

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getProducts(query: ProductQueryParams): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams();
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.subcategoryId) {
      params = params.set('subcategoryId', query.subcategoryId);
    }
    if (query.sort) {
      params = params.set('sort', query.sort);
    }
    if (query.page) {
      params = params.set('page', query.page);
    }
    for (const [key, value] of Object.entries(query.eigenschaften ?? {})) {
      if (value !== '' && value !== undefined && value !== null) {
        params = params.set(`eig.${key}`, value);
      }
    }

    return this.http.get<PagedResult<ProductListItem>>('/api/products', { params });
  }

  getProduct(id: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`/api/products/${id}`);
  }

  addRating(productId: string, rating: RatingCreate): Observable<RatingResponse> {
    return this.http.post<RatingResponse>(`/api/products/${productId}/ratings`, rating);
  }
}
