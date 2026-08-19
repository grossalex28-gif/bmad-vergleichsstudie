import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { PagedResult, ProductDetail, ProductListItem, ProductQuery, ReviewCreate, ReviewResult } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/products';

  getProducts(query: ProductQuery): Observable<PagedResult<ProductListItem>> {
    let params = new HttpParams();
    if (query.categoryId != null) params = params.set('categoryId', query.categoryId);
    if (query.subcategoryId != null) params = params.set('subcategoryId', query.subcategoryId);
    if (query.search) params = params.set('search', query.search);
    if (query.sortBy) params = params.set('sortBy', query.sortBy);
    if (query.sortDir) params = params.set('sortDir', query.sortDir);
    if (query.page) params = params.set('page', query.page);
    if (query.properties) {
      for (const [key, value] of Object.entries(query.properties)) {
        if (value) params = params.set(key, value);
      }
    }
    return this.http.get<PagedResult<ProductListItem>>(this.baseUrl, { params });
  }

  getProduct(id: number): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`${this.baseUrl}/${id}`);
  }

  addReview(id: number, review: ReviewCreate): Observable<ReviewResult> {
    return this.http.post<ReviewResult>(`${this.baseUrl}/${id}/reviews`, review);
  }
}
