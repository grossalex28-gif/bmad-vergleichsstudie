import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Sitzplan } from '../models/sitzplan';
import { Veranstaltung } from '../models/veranstaltung';
import { VeranstaltungDetail } from '../models/veranstaltung-detail';

// [ASSUMPTION: keine Umgebungskonfiguration im Projektgerüst vorhanden — Backend-Basis-URL
// entspricht dem ASP.NET-Core-Dev-Profil "http" aus Properties/launchSettings.json]
const API_BASE_URL = 'http://localhost:5244';

@Injectable({ providedIn: 'root' })
export class VeranstaltungenService {
  private readonly http = inject(HttpClient);

  getVeranstaltungen(
    von?: string | null,
    bis?: string | null,
    spielstaetteId?: string | null
  ): Observable<Veranstaltung[]> {
    let params = new HttpParams();
    if (von) {
      params = params.set('von', von);
    }
    if (bis) {
      params = params.set('bis', bis);
    }
    if (spielstaetteId) {
      params = params.set('spielstaetteId', spielstaetteId);
    }

    return this.http.get<Veranstaltung[]>(`${API_BASE_URL}/veranstaltungen`, { params });
  }

  getVeranstaltung(id: string): Observable<VeranstaltungDetail> {
    return this.http.get<VeranstaltungDetail>(`${API_BASE_URL}/veranstaltungen/${id}`);
  }

  getSitzplan(veranstaltungId: string): Observable<Sitzplan> {
    return this.http.get<Sitzplan>(`${API_BASE_URL}/veranstaltungen/${veranstaltungId}/sitzplan`);
  }
}
