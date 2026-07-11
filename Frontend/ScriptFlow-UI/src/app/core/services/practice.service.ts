import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable, shareReplay } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Practice } from '../models/practice.model';

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
}
