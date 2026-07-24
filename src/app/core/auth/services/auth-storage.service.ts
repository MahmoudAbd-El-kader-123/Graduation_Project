import { Injectable, computed, signal } from '@angular/core';
import { AUTH_COOKIE_KEYS } from '../constants/auth.constants';
import { UserSession } from '../models/user-session.model';
import { Role, Permission, StoredProfile } from '../models/auth.types';
import { decodeJwt, extractPermissionsFromPayload, extractRoleFromPayload } from '../utils/jwt.util';

@Injectable({
  providedIn: 'root'
})
export class AuthStorageService {
  readonly session = signal<UserSession | null>(null);
  readonly isInitialized = signal<boolean>(false);

  readonly role = computed(() => this.session()?.role ?? null);
  readonly permissions = computed(() => this.session()?.permissions ?? []);
  readonly isAuthenticated = computed(() => {
    const hasAccessToken = !!this.getAccessToken();
    const hasRefreshCredentials = !!this.getLastAccessToken() && !!this.getRefreshToken();

    return !!this.session() && (hasAccessToken || hasRefreshCredentials);
  });

  getAccessToken(): string | null {
    return this.getCookie(AUTH_COOKIE_KEYS.accessToken);
  }

  getLastAccessToken(): string | null {
    return this.getCookie(AUTH_COOKIE_KEYS.lastAccessToken);
  }

  getRefreshToken(): string | null {
    return this.getCookie(AUTH_COOKIE_KEYS.refreshToken);
  }

  saveTokens(accessToken: string, refreshToken: string, expiresAt: string): void {
    this.setCookie(AUTH_COOKIE_KEYS.accessToken, accessToken, expiresAt);
    
    // TEMPORARY WORKAROUND:
    // Backend currently does not expose refreshTokenExpiresAt.
    // We extend the refresh token cookie to 7 days to ensure it outlives
    // the short-lived access token, allowing the interceptor to read it.
    const refreshExpiry = new Date();
    refreshExpiry.setDate(refreshExpiry.getDate() + 7);
    this.setCookie(AUTH_COOKIE_KEYS.lastAccessToken, accessToken, refreshExpiry.toUTCString());
    this.setCookie(AUTH_COOKIE_KEYS.refreshToken, refreshToken, refreshExpiry.toUTCString());
    
    this.setCookie(AUTH_COOKIE_KEYS.expiresAt, expiresAt, expiresAt);
  }

  clearTokens(): void {
    this.removeCookie(AUTH_COOKIE_KEYS.accessToken);
    this.removeCookie(AUTH_COOKIE_KEYS.lastAccessToken);
    this.removeCookie(AUTH_COOKIE_KEYS.refreshToken);
    this.removeCookie(AUTH_COOKIE_KEYS.expiresAt);
  }

  saveProfile(profile: StoredProfile): void {
    // Save as JSON string
    this.setCookie(AUTH_COOKIE_KEYS.profile, JSON.stringify(profile));
  }

  restoreProfile(): StoredProfile | null {
    const profileJson = this.getCookie(AUTH_COOKIE_KEYS.profile);
    if (!profileJson) return null;
    try {
      return JSON.parse(profileJson) as StoredProfile;
    } catch {
      return null;
    }
  }

  clearProfile(): void {
    this.removeCookie(AUTH_COOKIE_KEYS.profile);
  }

  clearSession(): void {
    this.clearTokens();
    this.clearProfile();
    this.session.set(null);
  }

  rebuildMinimalSessionFromToken(): void {
    const token = this.getAccessToken();
    if (!token) {
      this.session.set(null);
      return;
    }

    const payload = decodeJwt(token);
    if (!payload) {
      this.session.set(null);
      return;
    }

    const roleStr = extractRoleFromPayload(payload);
    const role: Role = (roleStr as Role) || 'admin';

    const profile = this.restoreProfile();

    const session: UserSession = {
      userId: payload.sub || '',
      email: payload.email || '',
      role: role,
      permissions: extractPermissionsFromPayload(payload) as Permission[],
      fullName: profile?.fullName ?? null,
      userName: profile?.userName ?? null,
      phoneNumber: profile?.phoneNumber ?? null
    };

    this.session.set(session);
  }

  // --- Private Cookie Management to enforce Single Source of Truth ---

  private setCookie(name: string, value: string, expiresAt?: string): void {
    let expires = '';
    if (expiresAt) {
      const date = new Date(expiresAt);
      expires = '; expires=' + date.toUTCString();
    }
    // Set to root path so all routes can access it
    document.cookie = `${name}=${encodeURIComponent(value || '')}${expires}; path=/; SameSite=Lax`;
  }

  private getCookie(name: string): string | null {
    const nameEQ = name + '=';
    const ca = document.cookie.split(';');
    for (let i = 0; i < ca.length; i++) {
      let c = ca[i];
      while (c.charAt(0) === ' ') c = c.substring(1, c.length);
      if (c.indexOf(nameEQ) === 0) return decodeURIComponent(c.substring(nameEQ.length, c.length));
    }
    return null;
  }

  private removeCookie(name: string): void {
    document.cookie = name + '=; Path=/; Expires=Thu, 01 Jan 1970 00:00:01 GMT;';
  }
}
