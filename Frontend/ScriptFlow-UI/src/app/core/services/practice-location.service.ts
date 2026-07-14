import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, shareReplay, tap } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePracticeLocationRequest, PracticeLocation } from '../models/practice-location.model';

/** Practice locations are near-static reference data, so the unfiltered list is cached. */
@Injectable({ providedIn: 'root' })
export class PracticeLocationService {
  private readonly baseUrl = `${environment.apiBaseUrl}/practice-locations`;
  private cachedList$: Observable<PracticeLocation[]> | null = null;

  constructor(private readonly http: HttpClient) {}

  list(practiceId?: string): Observable<PracticeLocation[]> {
    if (!practiceId) {
      if (!this.cachedList$) {
        this.cachedList$ = this.http.get<PracticeLocation[]>(this.baseUrl).pipe(shareReplay(1));
      }

      return this.cachedList$;
    }

    return this.http.get<PracticeLocation[]>(this.baseUrl, { params: { practiceId } });
  }

  getById(id: string): Observable<PracticeLocation> {
    return this.http.get<PracticeLocation>(`${this.baseUrl}/${id}`);
  }

  create(request: CreatePracticeLocationRequest): Observable<PracticeLocation> {
    // Invalidate the cached unfiltered list so a freshly created location shows up
    // immediately in every picker that uses list() (provider form, prescription form, etc.).
    return this.http.post<PracticeLocation>(this.baseUrl, request).pipe(tap(() => (this.cachedList$ = null)));
  }
}
