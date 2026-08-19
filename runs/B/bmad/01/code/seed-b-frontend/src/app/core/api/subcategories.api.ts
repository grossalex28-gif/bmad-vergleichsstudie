import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

export interface PropertyFilterOption {
  name: string;
  values: string[];
}

@Injectable({ providedIn: 'root' })
export class SubcategoriesApi {
  private readonly http = inject(HttpClient);

  getProperties(subcategoryId: string): Observable<PropertyFilterOption[]> {
    return this.http.get<PropertyFilterOption[]>(`/api/subcategories/${subcategoryId}/properties`);
  }
}
