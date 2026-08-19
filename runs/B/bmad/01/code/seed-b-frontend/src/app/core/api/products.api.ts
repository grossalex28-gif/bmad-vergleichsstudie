import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface ProductListItem {
  id: string;
  name: string;
  lowestPrice: number | null;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  pageCount: number;
  page: number;
  pageSize: number;
}

export interface ProductProperty {
  name: string;
  value: string;
}

export interface ProductOffer {
  supplierId: string;
  supplierName: string;
  price: number;
}

export interface ProductDetail {
  id: string;
  name: string;
  description: string;
  categoryId: string;
  categoryName: string;
  subcategoryId: string;
  subcategoryName: string;
  properties: ProductProperty[];
  offers: ProductOffer[];
  averageRating: number | null;
  ratingCount: number;
}

@Injectable({ providedIn: 'root' })
export class ProductsApi {
  private readonly http = inject(HttpClient);

  getProducts(
    page = 1,
    categoryId?: string,
    subcategoryId?: string,
    sortBy?: 'price' | 'name' | 'viewCount',
    sortDir?: 'asc' | 'desc',
    q?: string,
    properties?: string[],
  ): Observable<PagedResult<ProductListItem>> {
    const params: Record<string, string | number | string[]> = { page };
    if (categoryId !== undefined) {
      params['categoryId'] = categoryId;
    }
    if (subcategoryId !== undefined) {
      params['subcategoryId'] = subcategoryId;
    }
    if (sortBy !== undefined) {
      params['sortBy'] = sortBy;
    }
    if (sortDir !== undefined) {
      params['sortDir'] = sortDir;
    }
    if (q !== undefined) {
      params['q'] = q;
    }
    if (properties !== undefined && properties.length > 0) {
      params['property'] = properties;
    }

    return this.http.get<PagedResult<ProductListItem>>('/api/products', { params });
  }

  getProduct(id: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`/api/products/${encodeURIComponent(id)}`);
  }

  submitRating(productId: string, authorName: string, value: number): Observable<void> {
    return this.http.post<void>(`/api/products/${encodeURIComponent(productId)}/ratings`, { authorName, value });
  }
}
