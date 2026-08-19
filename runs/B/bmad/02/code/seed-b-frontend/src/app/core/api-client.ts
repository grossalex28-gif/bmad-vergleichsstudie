import { HttpClient } from '@angular/common/http';
import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';

@Injectable({ providedIn: 'root' })
export class ApiClient {
  private http = inject(HttpClient);

  get<T>(path: string, params?: Record<string, string | number>, headers?: Record<string, string>): Observable<T> {
    return this.http.get<T>('/api' + path, { params, headers });
  }

  post<T>(path: string, body: unknown, options?: { headers?: Record<string, string> }): Observable<T> {
    return this.http.post<T>('/api' + path, body, { headers: options?.headers });
  }

  patch<T>(path: string, body: unknown, options?: { headers?: Record<string, string> }): Observable<T> {
    return this.http.patch<T>('/api' + path, body, { headers: options?.headers });
  }

  delete<T>(path: string, options?: { headers?: Record<string, string> }): Observable<T> {
    return this.http.delete<T>('/api' + path, { headers: options?.headers });
  }
}
