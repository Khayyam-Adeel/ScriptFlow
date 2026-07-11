import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

const PUBLIC_PATHS = ['/auth/login', '/auth/register'];

/** Attaches the bearer token to every API call except login/register, which have no token yet. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const isPublicAuthCall = PUBLIC_PATHS.some((path) => request.url.includes(path));

  if (isPublicAuthCall || !authService.token) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { Authorization: `Bearer ${authService.token}` },
    }),
  );
};
