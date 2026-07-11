import { Injectable } from '@angular/core';
import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { Subject } from 'rxjs';
import { environment } from '../../../environments/environment';
import { PrescriptionStatus } from '../../shared/models/prescription-status';

export interface PrescriptionStatusChangedMessage {
  prescriptionId: string;
  status: PrescriptionStatus;
}

/**
 * Thin wrapper around a SignalR HubConnection to Notification.Service. Started on login
 * (and on app bootstrap when a session already exists), stopped on logout, so only an
 * authenticated session holds the socket open.
 */
@Injectable({ providedIn: 'root' })
export class PrescriptionHubService {
  private connection: HubConnection | null = null;
  private readonly statusChangedSubject = new Subject<PrescriptionStatusChangedMessage>();

  readonly statusChanged$ = this.statusChangedSubject.asObservable();

  start(getToken: () => string | null): void {
    if (this.connection) {
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl(environment.notificationHubUrl, { accessTokenFactory: () => getToken() ?? '' })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Warning)
      .build();

    this.connection.on('prescriptionStatusChanged', (message: PrescriptionStatusChangedMessage) => {
      this.statusChangedSubject.next(message);
    });

    // Best-effort: a down Notification.Service shouldn't block the rest of the app, and the
    // 5s prescription-list poll keeps status eventually correct while disconnected.
    this.connection.start().catch(() => undefined);
  }

  stop(): void {
    this.connection?.stop().catch(() => undefined);
    this.connection = null;
  }
}
