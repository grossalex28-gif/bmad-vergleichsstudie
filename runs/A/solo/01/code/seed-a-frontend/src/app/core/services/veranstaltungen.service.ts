import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { Sitzplan, VeranstaltungDetail, VeranstaltungListItem } from '../models/veranstaltung.model';

export interface VeranstaltungsFilter {
  von?: string;
  bis?: string;
  spielstaetteId?: string;
}

@Injectable({ providedIn: 'root' })
export class VeranstaltungenService {
  private readonly http = inject(HttpClient);

  getAlle(filter: VeranstaltungsFilter = {}): Observable<VeranstaltungListItem[]> {
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

    return this.http.get<VeranstaltungListItem[]>('/api/veranstaltungen', { params });
  }

  getEine(id: string): Observable<VeranstaltungDetail> {
    return this.http.get<VeranstaltungDetail>(`/api/veranstaltungen/${id}`);
  }

  getSitzplan(id: string): Observable<Sitzplan> {
    return this.http.get<Sitzplan>(`/api/veranstaltungen/${id}/sitzplan`);
  }
}
