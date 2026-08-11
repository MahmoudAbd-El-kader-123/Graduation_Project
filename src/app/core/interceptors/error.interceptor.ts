import { HttpInterceptorFn, HttpErrorResponse, HttpContextToken } from '@angular/common/http';
import { inject } from '@angular/core';
import { Router } from '@angular/router';
import { catchError, throwError } from 'rxjs';
import { toast } from 'ngx-sonner';
import { AuthService } from '../auth/services/auth.service';
import { AUTH_ROUTES, AUTH_ENDPOINTS } from '../auth/constants/auth.constants';
import { ApiResponse } from '../models/api-response.model';

export const SKIP_ERROR_TOAST = new HttpContextToken<boolean>(() => false);

export const errorInterceptor: HttpInterceptorFn = (req, next) => {
  const router = inject(Router);
  const authService = inject(AuthService);

  return next(req).pipe(
    catchError((error: HttpErrorResponse) => {
      if (req.context.get(SKIP_ERROR_TOAST)) {
        return throwError(() => error);
      }

      if (error.error && error.error.errors && Array.isArray(error.error.errors)) {
        const apiResponse = error.error as ApiResponse<any>;
        apiResponse.errors.forEach(err => toast.error(err));
      } else if (error.error && typeof error.error.message === 'string') {
        toast.error(error.error.message);
      } else {
        const isAuthEndpoint = req.url.includes(AUTH_ENDPOINTS.login) || 
                               req.url.includes(AUTH_ENDPOINTS.register);

        if (!isAuthEndpoint) {
          switch (error.status) {
            case 400:
              toast.error('Bad Request. Please check your input.');
              break;
            case 401:
              toast.error('Your session has expired. Please log in again.');
              break;
            case 403:
              toast.error('Forbidden. You do not have permission to access this resource.');
              break;
            case 404:
              toast.error('Resource not found.');
              break;
            case 500:
              toast.error('Internal Server Error. Please try again later.');
              break;
            default:
              toast.error('An unexpected error occurred.');
              break;
          }
        }
      }
      return throwError(() => error);
    })
  );
};
