import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Spielstaette } from '../models/spielstaette.model';

@Injectable({ providedIn: 'root' })
export class SpielstaettenService {
  private readonly http = inject(HttpClient);

  getAlle(): Observable<Spielstaette[]> {
    return this.http.get<Spielstaette[]>('/api/spielstaetten');
  }
}
