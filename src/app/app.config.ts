import { ApplicationConfig, provideBrowserGlobalErrorListeners, provideZonelessChangeDetection, provideAppInitializer, inject } from '@angular/core';
import { provideRouter } from '@angular/router';
import { provideAnimations } from '@angular/platform-browser/animations';
import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { providePrimeNG } from 'primeng/config';
import { MessageService, ConfirmationService } from 'primeng/api';
import { definePreset } from '@primeuix/themes';
import Aura from '@primeuix/themes/aura';
import { provideLucideIcons } from '@lucide/angular';
import { APP_LUCIDE_ICONS_ARRAY } from './core/icons/lucide-icons';
import { importProvidersFrom } from '@angular/core';

import { routes } from './app.routes';
import { authInterceptor } from './core/auth/interceptors/auth.interceptor';
import { errorInterceptor } from './core/interceptors/error.interceptor';
import { AuthService } from './core/auth/services/auth.service';
import { environment } from '../environments/environment';

const SkyBluePreset = definePreset(Aura, {
    semantic: {
        primary: {
            50: '#f0f9ff',
            100: '#e0f2fe',
            200: '#bae6fd',
            300: '#7dd3fc',
            400: '#38bdf8',
            500: '#0ea5e9',
            600: '#0284c7',
            700: '#0369a1',
            800: '#075985',
            900: '#0c4a6e',
            950: '#082f49'
        }
    }
});

export const appConfig: ApplicationConfig = {
  providers: [
    provideAppInitializer(() => {
        return inject(AuthService).initializeAuth();
    }),
    provideZonelessChangeDetection(),
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideAnimations(),
    MessageService,
    ConfirmationService,
    provideLucideIcons(...APP_LUCIDE_ICONS_ARRAY),
    provideHttpClient(
      withInterceptors([errorInterceptor, authInterceptor])
    ),
    providePrimeNG({
        theme: {
            preset: SkyBluePreset,
            options: {
                darkModeSelector: '.my-app-dark'
            }
        },
        license: environment.primeUiLicense
    })
  ]
};
