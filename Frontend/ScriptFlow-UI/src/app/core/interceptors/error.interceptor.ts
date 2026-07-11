import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { AuthService } from '../services/auth.service';
import { NotificationService } from '../services/notification.service';
import { ProblemDetails } from '../models/problem-details.model';

/** Turns the API's problem+json error shape into a toast, and forces re-login on 401. */
export const errorInterceptor: HttpInterceptorFn = (request, next) => {
  const notifications = inject(NotificationService);
  const authService = inject(AuthService);
  const router = inject(Router);

  return next(request).pipe(
    catchError((error: unknown) => {
      if (error instanceof HttpErrorResponse) {
        if (error.status === 401) {
          authService.logout();
          router.navigate(['/login']);
        }

        const problem = error.error as Partial<ProblemDetails> | null;
        notifications.error('Something went wrong. Please try again.');
      }

      return throwError(() => error);
    }),
  );
};
