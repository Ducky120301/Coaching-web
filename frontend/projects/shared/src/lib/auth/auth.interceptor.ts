import { HttpErrorResponse, HttpInterceptorFn, HttpRequest } from '@angular/common/http';
import { inject } from '@angular/core';
import { catchError, from, switchMap, throwError } from 'rxjs';
import { APP_CONFIG } from '../config/app-config';
import { AuthService } from './auth.service';
import { SKIP_AUTH } from './skip-auth';

function withBearer(req: HttpRequest<unknown>, token: string | null): HttpRequest<unknown> {
  return token ? req.clone({ setHeaders: { Authorization: `Bearer ${token}` } }) : req;
}

// @BR-f00-codebase-010: 401 → gia hạn 1 lần → gửi lại request gốc
export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const auth = inject(AuthService);
  const { apiBaseUrl } = inject(APP_CONFIG);
  if (req.context.get(SKIP_AUTH) || !req.url.startsWith(`${apiBaseUrl}/api/`)) {
    return next(req);
  }
  return next(withBearer(req, auth.accessToken())).pipe(
    catchError((error: unknown) => {
      if (!(error instanceof HttpErrorResponse) || error.status !== 401) {
        return throwError(() => error);
      }
      return from(auth.refresh()).pipe(
        catchError((refreshError: unknown) => {
          auth.handleSessionExpired();
          return throwError(() => refreshError);
        }),
        switchMap((token) => next(withBearer(req, token))),
      );
    }),
  );
};
