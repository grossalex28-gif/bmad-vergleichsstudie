import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Buchung, CreateBuchungRequest } from '../models/api.models';

@Injectable({ providedIn: 'root' })
export class BuchungenService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api/buchungen';

  create(request: CreateBuchungRequest): Observable<Buchung> {
    return this.http.post<Buchung>(this.baseUrl, request);
  }

  getByReferenz(referenz: string): Observable<Buchung> {
    return this.http.get<Buchung>(`${this.baseUrl}/${referenz}`);
  }

  stornieren(referenz: string): Observable<Buchung> {
    return this.http.post<Buchung>(`${this.baseUrl}/${referenz}/stornieren`, {});
  }
}
