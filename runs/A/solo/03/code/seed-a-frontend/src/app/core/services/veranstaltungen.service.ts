import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Sitzplan, Spielstaette, VeranstaltungDetail, VeranstaltungListItem } from '../models/api.models';

export interface VeranstaltungenFilter {
  von?: string;
  bis?: string;
  spielstaetteId?: string;
}

@Injectable({ providedIn: 'root' })
export class VeranstaltungenService {
  private readonly http = inject(HttpClient);
  private readonly baseUrl = '/api';

  getSpielstaetten(): Observable<Spielstaette[]> {
    return this.http.get<Spielstaette[]>(`${this.baseUrl}/spielstaetten`);
  }

  getVeranstaltungen(filter: VeranstaltungenFilter): Observable<VeranstaltungListItem[]> {
    let params = new HttpParams();
    if (filter.von) {
      params = params.set('von', filter.von);
    }
    if (filter.bis) {
      params = params.set('bis', filter.bis);
    }
    if (filter.spielstaetteId) {
      params = params.set('spielstaetteId', filter.spielstaetteId);
    }
    return this.http.get<VeranstaltungListItem[]>(`${this.baseUrl}/veranstaltungen`, { params });
  }

  getDetail(id: string): Observable<VeranstaltungDetail> {
    return this.http.get<VeranstaltungDetail>(`${this.baseUrl}/veranstaltungen/${id}`);
  }

  getSitzplan(id: string): Observable<Sitzplan> {
    return this.http.get<Sitzplan>(`${this.baseUrl}/veranstaltungen/${id}/sitzplan`);
  }
}
