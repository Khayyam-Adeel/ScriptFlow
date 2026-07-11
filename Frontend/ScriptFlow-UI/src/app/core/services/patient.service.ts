import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { CreatePatientRequest, Patient } from '../models/patient.model';

@Injectable({ providedIn: 'root' })
export class PatientService {
  private readonly baseUrl = `${environment.apiBaseUrl}/patients`;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePatientRequest): Observable<Patient> {
    return this.http.post<Patient>(this.baseUrl, request);
  }

  getById(id: string): Observable<Patient> {
    return this.http.get<Patient>(`${this.baseUrl}/${id}`);
  }

  /** Matches Profile.usp_Patient_Search: case-insensitive contains-match on first name, last name, or NHI. */
  search(query: string): Observable<Patient[]> {
    return this.http.get<Patient[]>(`${this.baseUrl}/search`, { params: { query } });
  }
}
