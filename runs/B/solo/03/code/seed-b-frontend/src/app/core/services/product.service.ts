import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { CreateRatingRequest, ProductDetail, ProductListResponse, ProductQuery, Rating } from '../models/product.model';

@Injectable({ providedIn: 'root' })
export class ProductService {
  private readonly http = inject(HttpClient);

  getList(query: ProductQuery): Observable<ProductListResponse> {
    let params = new HttpParams();

    if (query.categoryId) {
      params = params.set('categoryId', query.categoryId);
    }
    if (query.subCategoryId) {
      params = params.set('subCategoryId', query.subCategoryId);
    }
    if (query.search) {
      params = params.set('search', query.search);
    }
    if (query.sort) {
      params = params.set('sort', query.sort);
    }
    if (query.page) {
      params = params.set('page', query.page);
    }
    for (const [name, value] of Object.entries(query.properties ?? {})) {
      params = params.set(`properties[${name}]`, value);
    }

    return this.http.get<ProductListResponse>('/api/products', { params });
  }

  getDetail(id: string): Observable<ProductDetail> {
    return this.http.get<ProductDetail>(`/api/products/${id}`);
  }

  rate(id: string, request: CreateRatingRequest): Observable<Rating> {
    return this.http.post<Rating>(`/api/products/${id}/ratings`, request);
  }
}
