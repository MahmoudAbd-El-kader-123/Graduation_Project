import { HttpInterceptorFn, HttpErrorResponse, HttpRequest, HttpHandlerFn, HttpEvent, HttpContextToken } from '@angular/common/http';
import { inject, signal } from '@angular/core';
import { catchError, switchMap, throwError, from, Observable } from 'rxjs';
import { AuthStorageService } from '../services/auth-storage.service';
import { AuthService } from '../services/auth.service';
import { AUTH_ENDPOINTS } from '../constants/auth.constants';

export const IS_RETRY = new HttpContextToken<boolean>(() => false);

const isRefreshing = signal(false);
let refreshPromise: Promise<void> | null = null;

export const authInterceptor: HttpInterceptorFn = (req, next) => {
  const authStorage = inject(AuthStorageService);
  const authService = inject(AuthService);

  const isAuthEndpoint = req.url.includes(AUTH_ENDPOINTS.login) || 
                         req.url.includes(AUTH_ENDPOINTS.register) ||
                         req.url.includes(AUTH_ENDPOINTS.refreshToken);

  let authReq = req;

  if (!isAuthEndpoint) {
    const token = authStorage.getAccessToken();
    if (token) {
      authReq = req.clone({
        headers: req.headers.set('Authorization', `Bearer ${token}`)
      });
    }
  }

  return next(authReq).pipe(
    catchError((error: HttpErrorResponse) => {
      if (error.status === 401 && !isAuthEndpoint && !req.context.get(IS_RETRY)) {
        return handle401Error(authReq, next, authStorage, authService);
      }
      return throwError(() => error);
    })
  );
};

function handle401Error(req: HttpRequest<any>, next: HttpHandlerFn, authStorage: AuthStorageService, authService: AuthService): Observable<HttpEvent<any>> {
  if (!isRefreshing()) {
    isRefreshing.set(true);
    const refreshToken = authStorage.getRefreshToken();
    const token = authStorage.getAccessToken() ?? authStorage.getLastAccessToken();

    if (token && refreshToken) {
      refreshPromise = new Promise<void>((resolve, reject) => {
        authService.refreshToken(token, refreshToken).subscribe({
          next: (response) => {
            if (response.success && response.data) {
              resolve();
            } else {
              authService.logout();
              reject(new Error('Refresh failed'));
            }
          },
          error: (err) => {
            authService.logout();
            reject(err);
          }
        });
      }).finally(() => {
        isRefreshing.set(false);
        refreshPromise = null;
      });
    } else {
      isRefreshing.set(false);
      refreshPromise = null;
      authService.logout();
      return throwError(() => new Error('No refresh token available'));
    }
  }

  if (refreshPromise) {
    return from(refreshPromise).pipe(
      switchMap(() => {
        const newToken = authStorage.getAccessToken();
        if (newToken) {
          const authReq = req.clone({
            headers: req.headers.set('Authorization', `Bearer ${newToken}`),
            context: req.context.set(IS_RETRY, true)
          });
          return next(authReq);
        }
        authService.logout();
        return throwError(() => new Error('Token refresh failed to provide a new access token'));
      })
    );
  }

  return throwError(() => new Error('Refresh flow encountered an unknown state'));
}
