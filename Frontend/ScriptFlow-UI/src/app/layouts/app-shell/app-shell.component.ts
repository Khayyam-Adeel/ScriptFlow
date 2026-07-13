import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive, RouterOutlet } from '@angular/router';
import { Router } from '@angular/router';
import { AuthService } from '../../core/services/auth.service';
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

  readonly user = this.authService.user;
  readonly isAdmin = this.authService.isAdmin;

  logout(): void {
    this.authService.logout();
    this.router.navigate(['/login']);
  }
}
