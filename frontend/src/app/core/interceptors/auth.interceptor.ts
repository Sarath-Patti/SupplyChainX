import { HttpInterceptorFn, HttpErrorResponse } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { environment } from '../../../environments/environment';

const TOKEN_KEY = 'supplychainx_token';
const USER_KEY = 'supplychainx_user';

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const token = localStorage.getItem(TOKEN_KEY);

  let authReq = req;
  const urlLower = req.url.toLowerCase();

  // Public endpoints that should not carry Bearer token (prevents CORS preflight overhead/issues)
  const isPublicEndpoint = urlLower.includes('/api/v1/auth/login') ||
                           urlLower.includes('/api/v1/auth/register') ||
                           urlLower.includes('/health');

  const isTargetApi = req.url.startsWith(environment.apiBaseUrl) || req.url.startsWith('/');

  if (token && isTargetApi && !isPublicEndpoint) {
    authReq = req.clone({
      setHeaders: {
        Authorization: `Bearer ${token}`
      }
    });
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401) {
        localStorage.removeItem(TOKEN_KEY);
        localStorage.removeItem(USER_KEY);
        if (!router.url.includes('/login')) {
          router.navigate(['/login'], { queryParams: { returnUrl: router.url } });
        }
      } else if (error.status === 403) {
        if (!router.url.includes('/unauthorized')) {
          router.navigate(['/unauthorized']);
        }
      }
      return throwError(() => error);
    })
  );
};
