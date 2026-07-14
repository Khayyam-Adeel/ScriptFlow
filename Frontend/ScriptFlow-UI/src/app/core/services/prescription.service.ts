import { HttpClient, HttpParams } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PrescriptionStatus } from '../../shared/models/prescription-status';
import {
  CreatePrescriptionRequest,
  LocationVolume,
  Prescription,
  PrescriptionDailyVolume,
  PrescriptionStatusCount,
  RejectionRate,
  UpdatePrescriptionRequest,
} from '../models/prescription.model';

export interface PrescriptionListFilters {
  patientId?: string;
  providerId?: string;
  status?: PrescriptionStatus;
  /** Matched as a prefix (Scid LIKE scid + '%') by usp_Prescription_List - see that proc for why. */
  scid?: string;
  /** Both inclusive calendar days, formatted yyyy-MM-dd. */
  createdFrom?: string;
  createdTo?: string;
}

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

  list(filters: PrescriptionListFilters = {}): Observable<Prescription[]> {
    let params = new HttpParams();
    for (const [key, value] of Object.entries(filters)) {
      if (value) {
        params = params.set(key, value);
      }
    }

    return this.http.get<Prescription[]>(this.baseUrl, { params });
  }

  /** Counts across every prescription, not just a page of them - what the dashboard's status
   * tiles need. `list()` is capped to the 200 most recent matches, so it would undercount. */
  getStatusCounts(): Observable<PrescriptionStatusCount[]> {
    return this.http.get<PrescriptionStatusCount[]>(`${this.baseUrl}/status-counts`);
  }

  /** 14-day prescription volume trend for the dashboard's chart - see
   * GetPrescriptionDailyVolumeQueryHandler for the fixed window. */
  getDailyVolume(): Observable<PrescriptionDailyVolume[]> {
    return this.http.get<PrescriptionDailyVolume[]>(`${this.baseUrl}/daily-volume`);
  }

  /** Admin overview chart: total prescription volume per practice location. */
  getVolumeByLocation(): Observable<LocationVolume[]> {
    return this.http.get<LocationVolume[]>(`${this.baseUrl}/reporting/volume-by-location`);
  }

  /** Admin overview chart: rejection rate of finalized prescriptions per practice location. */
  getRejectionRateByLocation(): Observable<RejectionRate[]> {
    return this.http.get<RejectionRate[]>(`${this.baseUrl}/reporting/rejection-rate-by-location`);
  }

  /** Admin overview chart: rejection rate of finalized prescriptions per provider. */
  getRejectionRateByProvider(): Observable<RejectionRate[]> {
    return this.http.get<RejectionRate[]>(`${this.baseUrl}/reporting/rejection-rate-by-provider`);
  }
}
