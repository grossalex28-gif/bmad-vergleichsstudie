import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

import { ApiClient } from '../../core/api-client';
import { ProductDetail, RatingSubmission, RatingSubmissionResult } from './product.model';

@Injectable({ providedIn: 'root' })
export class ProductApiService {
  private apiClient = inject(ApiClient);

  getProduct(id: string): Observable<ProductDetail> {
    return this.apiClient.get<ProductDetail>(`/products/${id}`);
  }

  submitRating(productId: string, submission: RatingSubmission): Observable<RatingSubmissionResult> {
    return this.apiClient.post<RatingSubmissionResult>(`/products/${productId}/ratings`, submission);
  }
}
