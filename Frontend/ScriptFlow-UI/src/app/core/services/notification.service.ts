import { Injectable, signal } from '@angular/core';

export type NotificationKind = 'error' | 'success' | 'info';

export interface Notification {
  id: number;
  message: string;
  kind: NotificationKind;
}

const AUTO_DISMISS_MS = 5000;

/** Signal-based toast queue. ToastComponent (shared) renders whatever is in `notifications`. */
@Injectable({ providedIn: 'root' })
export class NotificationService {
  private nextId = 1;
  readonly notifications = signal<Notification[]>([]);

  show(message: string, kind: NotificationKind = 'info'): void {
    const notification: Notification = { id: this.nextId++, message, kind };
    this.notifications.update((current) => [...current, notification]);
    setTimeout(() => this.dismiss(notification.id), AUTO_DISMISS_MS);
  }

  error(message: string): void {
    this.show(message, 'error');
  }

  success(message: string): void {
    this.show(message, 'success');
  }

  dismiss(id: number): void {
    this.notifications.update((current) => current.filter((n) => n.id !== id));
  }
}
