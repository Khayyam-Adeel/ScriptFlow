import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PrescriptionStatus } from '../../shared/models/prescription-status';
import {
  CreatePrescriptionRequest,
  Prescription,
  PrescriptionStatusCount,
  UpdatePrescriptionRequest,
} from '../models/prescription.model';

@Injectable({ providedIn: 'root' })
export class PrescriptionService {
  private readonly baseUrl = `${environment.apiBaseUrl}/prescriptions`;

  constructor(private readonly http: HttpClient) {}

  create(request: CreatePrescriptionRequest): Observable<Prescription> {
    return this.http.post<Prescription>(this.baseUrl, request);
  }

  update(id: string, request: UpdatePrescriptionRequest): Observable<Prescription> {
    return this.http.put<Prescription>(`${this.baseUrl}/${id}`, request);
  }

  sign(id: string): Observable<Prescription> {
    return this.http.post<Prescription>(`${this.baseUrl}/${id}/sign`, {});
  }

  repeat(id: string): Observable<Prescription> {
    return this.http.post<Prescription>(`${this.baseUrl}/${id}/repeat`, {});
  }

  getById(id: string): Observable<Prescription> {
    return this.http.get<Prescription>(`${this.baseUrl}/${id}`);
  }

  list(patientId?: string, status?: PrescriptionStatus): Observable<Prescription[]> {
    const params: Record<string, string> = {};
    if (patientId) {
      params['patientId'] = patientId;
    }
    if (status) {
      params['status'] = status;
    }

    return this.http.get<Prescription[]>(this.baseUrl, { params });
  }

  /** Counts across every prescription, not just a page of them - what the dashboard's status
   * tiles need. `list()` is capped to the 200 most recent matches, so it would undercount. */
  getStatusCounts(): Observable<PrescriptionStatusCount[]> {
    return this.http.get<PrescriptionStatusCount[]>(`${this.baseUrl}/status-counts`);
  }
}
