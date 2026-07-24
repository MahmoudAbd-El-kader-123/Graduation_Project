import { Injectable, inject } from '@angular/core';
import { Router } from '@angular/router';
import { Observable, tap, catchError, of, firstValueFrom } from 'rxjs';
import { ApiService } from '../../services/api.service';
import { AuthStorageService } from './auth-storage.service';
import { AUTH_ENDPOINTS, AUTH_ROUTES } from '../constants/auth.constants';
import { AuthResponse } from '../models/auth-response.model';
import { ApiResponse } from '../../models/api-response.model';
import { isTokenExpired } from '../utils/jwt.util';
import { hasPermission, hasAnyPermission, hasAllPermissions } from '../utils/permission.utils';
import { Role, Permission } from '../models/auth.types';

@Injectable({
  providedIn: 'root'
})
export class AuthService {
  private readonly apiService = inject(ApiService);
  private readonly authStorage = inject(AuthStorageService);
  private readonly router = inject(Router);

  async initializeAuth(): Promise<void> {
    try {
      const activeToken = this.authStorage.getAccessToken();
      const token = activeToken ?? this.authStorage.getLastAccessToken();
      const refreshTokenValue = this.authStorage.getRefreshToken();

      if (activeToken && !isTokenExpired(activeToken)) {
        this.authStorage.rebuildMinimalSessionFromToken();
      } else if (token && refreshTokenValue) {
        try {
          const response = await firstValueFrom(
            this.refreshToken(token, refreshTokenValue)
          );

          if (!response.success || !response.data) {
            this.logout();
          }
        } catch {
          this.logout();
        }
      } else {
        this.authStorage.clearSession();
      }
    } catch (e) {
      console.error('Failed to initialize auth', e);
      this.logout();
    } finally {
      this.authStorage.isInitialized.set(true);
    }
  }

  login(payload: any): Observable<ApiResponse<AuthResponse>> {
    return this.apiService.post<ApiResponse<AuthResponse>>(AUTH_ENDPOINTS.login || '/auth/login', payload).pipe(
      tap(response => {
        if (response.success && response.data) {
          this.handleAuthSuccess(response.data);
        }
      })
    );
  }

  register(payload: any): Observable<ApiResponse<AuthResponse>> {
    return this.apiService.post<ApiResponse<AuthResponse>>(AUTH_ENDPOINTS.register || '/auth/register', payload).pipe(
      tap(response => {
        if (response.success && response.data && response.data.token) {
          this.handleAuthSuccess(response.data);
        }
      })
    );
  }

  refreshToken(token: string, refreshToken: string): Observable<ApiResponse<AuthResponse>> {
    return this.apiService.post<ApiResponse<AuthResponse>>(AUTH_ENDPOINTS.refreshToken || '/auth/refresh-token', {
      token,
      refreshToken
    }).pipe(
      tap(response => {
        if (response.success && response.data) {
          const authData = response.data;
          this.authStorage.saveTokens(authData.token, authData.refreshToken, authData.expiresAt);
          // If we had a full session, we might want to keep fullName/userName/phoneNumber, 
          // but for safety, rebuilding from token gives us the minimal required state.
          // Or we can just update tokens and not destroy existing runtime state if we don't have to.
          // Since refresh response might contain roles/permissions, let's update them:
          this.handleAuthSuccess(authData);
        }
      })
    );
  }

  logout(): void {
    this.authStorage.clearSession();
    this.router.navigate([AUTH_ROUTES.login]);
  }

  private handleAuthSuccess(authData: AuthResponse): void {
    this.authStorage.saveTokens(authData.token, authData.refreshToken, authData.expiresAt);
    
    // Save profile data into separate cookie
    this.authStorage.saveProfile({
      fullName: authData.fullName || null,
      userName: authData.userName || null,
      phoneNumber: authData.phoneNumber || null
    });

    // Convert role from array if necessary, normalize to Role type
    const rawRole = (authData.roles && authData.roles.length > 0) ? authData.roles[0].toLowerCase() : 'admin';
    const normalizedRole = rawRole.replace('role_', '') as Role;

    this.authStorage.session.set({
      userId: authData.userId,
      email: authData.email,
      role: normalizedRole,
      permissions: (authData.permissions?.map(p => p.toLowerCase()) || []) as Permission[],
      fullName: authData.fullName || null,
      userName: authData.userName || null,
      phoneNumber: authData.phoneNumber || null
    });
  }

  // --- Authorization Helpers ---

  hasRole(role: Role): boolean {
    return this.authStorage.role() === role.toLowerCase();
  }

  hasPermission(permission: Permission): boolean {
    return hasPermission(this.authStorage.permissions() as string[], permission);
  }

  hasAnyPermission(permissions: Permission[]): boolean {
    return hasAnyPermission(this.authStorage.permissions() as string[], permissions);
  }

  hasAllPermissions(permissions: Permission[]): boolean {
    return hasAllPermissions(this.authStorage.permissions() as string[], permissions);
  }

  canAccess(options: { role?: Role; permissions?: Permission[]; requireAll?: boolean }): boolean {
    if (!this.authStorage.isAuthenticated()) {
      return false;
    }

    if (options.role && !this.hasRole(options.role)) {
      return false;
    }

    if (options.permissions && options.permissions.length > 0) {
      if (options.requireAll) {
        if (!this.hasAllPermissions(options.permissions)) {
          return false;
        }
      } else {
        if (!this.hasAnyPermission(options.permissions)) {
          return false;
        }
      }
    }

    return true;
  }
}
