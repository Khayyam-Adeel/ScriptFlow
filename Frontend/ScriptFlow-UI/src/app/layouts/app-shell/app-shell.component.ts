import { Component, DestroyRef, inject } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
import { PrescriptionHubService } from '../../core/services/prescription-hub.service';
import { NotificationService } from '../../core/services/notification.service';
import { ToastComponent } from '../../shared/components/toast/toast.component';

/** Top nav + side nav shell wrapped around every authenticated route. */
@Component({
  selector: 'app-shell',
  standalone: true,
  imports: [RouterOutlet, RouterLink, RouterLinkActive, ToastComponent],
  templateUrl: './app-shell.component.html',
  styleUrl: './app-shell.component.css',
})
export class AppShellComponent {
  private readonly authService = inject(AuthService);
  private readonly router = inject(Router);
  private readonly prescriptionHub = inject(PrescriptionHubService);
  private readonly notifications = inject(NotificationService);
  private readonly destroyRef = inject(DestroyRef);

  readonly user = this.authService.user;
  readonly isAdmin = this.authService.isAdmin;

  constructor() {
    // System-wide, not tied to any one page: a message that exhausted its own retries and got
    // dead-lettered (see RabbitMqEventConsumer<TEvent>) used to only ever show up as a Serilog
    // error line - nobody using the app would ever know. Wired at the shell level (not a
    // specific prescription page) since the failure isn't necessarily about whatever the user
    // is currently looking at.
    this.prescriptionHub.messageDeadLettered$
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(({ eventType }) => {
        this.notifications.error(`System alert: ${eventType} failed permanently and was moved to the dead-letter queue.`);
      });
  }

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
