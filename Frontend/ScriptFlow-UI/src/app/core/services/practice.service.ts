import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePracticeRequest, Practice } from '../models/practice.model';

/** Practices are near-static reference data, so the list is cached for the lifetime of the app. */
@Injectable({ providedIn: 'root' })
export class PracticeService {
  private readonly baseUrl = `${environment.apiBaseUrl}/practices`;
  private cachedList$: Observable<Practice[]> | null = null;

  constructor(private readonly http: HttpClient) {}

  list(): Observable<Practice[]> {
    if (!this.cachedList$) {
      this.cachedList$ = this.http.get<Practice[]>(this.baseUrl).pipe(shareReplay(1));
    }

    return this.cachedList$;
  }

  getById(id: string): Observable<Practice> {
    return this.http.get<Practice>(`${this.baseUrl}/${id}`);
  }

  create(request: CreatePracticeRequest): Observable<Practice> {
    // Invalidate the cached list so a freshly created practice shows up immediately
    // everywhere list() is used, instead of waiting for a full page reload.
    return this.http.post<Practice>(this.baseUrl, request).pipe(tap(() => (this.cachedList$ = null)));
  }
}
