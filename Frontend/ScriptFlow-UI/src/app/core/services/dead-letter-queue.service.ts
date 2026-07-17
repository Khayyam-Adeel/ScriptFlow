import { HttpClient } from '@angular/common/http';
import { Injectable } from '@angular/core';
import { Observable } from 'rxjs';
import { environment } from '../../../environments/environment';
import {
  DeadLetterMessage,
  DeadLetterQueueSummary,
  RedriveDeadLetterQueueResult,
} from '../models/dead-letter-queue.model';

@Injectable({ providedIn: 'root' })
export class DeadLetterQueueService {
  private readonly baseUrl = `${environment.apiBaseUrl}/admin/dlq`;

  constructor(private readonly http: HttpClient) {}

  getSummary(): Observable<DeadLetterQueueSummary[]> {
    return this.http.get<DeadLetterQueueSummary[]>(this.baseUrl);
  }

  peekMessages(queueName: string, count = 50): Observable<DeadLetterMessage[]> {
    return this.http.get<DeadLetterMessage[]>(`${this.baseUrl}/${queueName}/messages`, {
      params: { count },
    });
  }

  redrive(queueName: string): Observable<RedriveDeadLetterQueueResult> {
    return this.http.post<RedriveDeadLetterQueueResult>(`${this.baseUrl}/${queueName}/redrive`, {});
  }
}
