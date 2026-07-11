import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreateProviderRequest, Provider } from '../models/provider.model';

@Injectable({ providedIn: 'root' })
export class ProviderService {
  private readonly baseUrl = `${environment.apiBaseUrl}/providers`;

  constructor(private readonly http: HttpClient) {}

  create(request: CreateProviderRequest): Observable<Provider> {
    return this.http.post<Provider>(this.baseUrl, request);
  }

  getById(id: string): Observable<Provider> {
    return this.http.get<Provider>(`${this.baseUrl}/${id}`);
  }

  list(practiceLocationId?: string): Observable<Provider[]> {
    const params: Record<string, string> = {};
    if (practiceLocationId) {
      params['practiceLocationId'] = practiceLocationId;
    }
    return this.http.get<Provider[]>(this.baseUrl, { params });
  }
}
