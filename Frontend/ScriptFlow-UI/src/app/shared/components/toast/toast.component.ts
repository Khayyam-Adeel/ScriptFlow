import { Component, inject } from '@angular/core';
import { NotificationService } from '../../../core/services/notification.service';

/** Fixed-position stack of dismissible toasts, driven entirely by NotificationService's signal. */
@Component({
  selector: 'app-toast',
  standalone: true,
  templateUrl: './toast.component.html',
  styleUrl: './toast.component.css',
})
export class ToastComponent {
  private readonly notificationService = inject(NotificationService);
  readonly notifications = this.notificationService.notifications;

  dismiss(id: number): void {
    this.notificationService.dismiss(id);
  }
}
