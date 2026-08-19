import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Buchung, BuchungCreateRequest } from '../models/buchung.model';

@Injectable({ providedIn: 'root' })
export class BuchungenService {
  private readonly http = inject(HttpClient);

  erstellen(anfrage: BuchungCreateRequest): Observable<Buchung> {
    return this.http.post<Buchung>('/api/buchungen', anfrage);
  }

  holeMitReferenz(referenz: string): Observable<Buchung> {
    return this.http.get<Buchung>(`/api/buchungen/${referenz}`);
  }

  stornieren(referenz: string): Observable<Buchung> {
    return this.http.post<Buchung>(`/api/buchungen/${referenz}/stornieren`, null);
  }
}
