import { HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { AuthService } from '../services/auth.service';

const PUBLIC_PATHS = ['/auth/login', '/auth/register'];

/** Attaches the bearer token to every API call except login/register, which have no token yet.
 * Matched by exact path end, not substring - `/auth/register-admin` (Admin-only, needs the
 * token attached) also contains `/auth/register` as a substring, so `.includes()` here would
 * wrongly treat it as public too and strip its token, turning every call into an anonymous
 * request the API then 401s. */
export const authInterceptor: HttpInterceptorFn = (request, next) => {
  const authService = inject(AuthService);
  const isPublicAuthCall = PUBLIC_PATHS.some((path) => request.url.endsWith(path));

  if (isPublicAuthCall || !authService.token) {
    return next(request);
  }

  return next(
    request.clone({
      setHeaders: { Authorization: `Bearer ${authService.token}` },
    }),
  );
};
