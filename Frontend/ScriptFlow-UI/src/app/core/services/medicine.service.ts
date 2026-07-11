import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { Medicine } from '../models/medicine.model';

@Injectable({ providedIn: 'root' })
export class MedicineService {
  private readonly baseUrl = `${environment.apiBaseUrl}/medicines`;

  constructor(private readonly http: HttpClient) {}

  list(search?: string): Observable<Medicine[]> {
    const params: Record<string, string> = {};
    if (search) {
      params['search'] = search;
    }
    return this.http.get<Medicine[]>(this.baseUrl, { params });
  }
}
