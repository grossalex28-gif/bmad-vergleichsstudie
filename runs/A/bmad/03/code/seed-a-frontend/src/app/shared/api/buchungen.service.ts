import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Buchung, BuchungAnlegenRequest } from '../models/buchung';
import { BuchungDetail } from '../models/buchung-detail';

// [ASSUMPTION: gleiche Backend-Basis-URL wie veranstaltungen.service.ts — kein eigenes
// Umgebungskonfigurationssystem im Projektgerüst vorhanden]
const API_BASE_URL = 'http://localhost:5244';

@Injectable({ providedIn: 'root' })
export class BuchungenService {
  private readonly http = inject(HttpClient);

  buchungAnlegen(veranstaltungId: string, request: BuchungAnlegenRequest): Observable<Buchung> {
    return this.http.post<Buchung>(
      `${API_BASE_URL}/veranstaltungen/${veranstaltungId}/buchungen`,
      request,
    );
  }

  buchungAbrufen(referenz: string): Observable<BuchungDetail> {
    return this.http.get<BuchungDetail>(
      `${API_BASE_URL}/buchungen/${encodeURIComponent(referenz)}`,
    );
  }

  buchungStornieren(referenz: string): Observable<BuchungDetail> {
    return this.http.post<BuchungDetail>(
      `${API_BASE_URL}/buchungen/${encodeURIComponent(referenz)}/stornierung`,
      {},
    );
  }
}
