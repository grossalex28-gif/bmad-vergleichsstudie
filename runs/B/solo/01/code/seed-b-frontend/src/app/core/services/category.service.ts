import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';

import { Category } from '../models/category.model';

@Injectable({ providedIn: 'root' })
export class CategoryService {
  private readonly http = inject(HttpClient);
  private categories$?: Observable<Category[]>;

  getCategories(): Observable<Category[]> {
    this.categories$ ??= this.http.get<Category[]>('/api/categories').pipe(shareReplay(1));
    return this.categories$;
  }
}
