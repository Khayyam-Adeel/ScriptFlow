import { inject } from '@angular/core';
import { CanActivateFn, Router } from '@angular/router';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';

/** Blocks Admin-only routes (e.g. provider creation) for a signed-in Prescriber who
 * navigates there directly - the API enforces this regardless, this just avoids
 * showing the form only to have the submit fail with a 403. */
export const adminGuard: CanActivateFn = () => {
  const authService = inject(AuthService);
  const notifications = inject(NotificationService);
  const router = inject(Router);

  if (authService.isAdmin()) {
    return true;
  }

  notifications.error("You don't have permission to do that.");
  return router.createUrlTree(['/dashboard']);
};
