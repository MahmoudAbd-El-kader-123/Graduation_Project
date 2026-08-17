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

const CustomFatortyPreset = definePreset(Aura, {
    semantic: {
        primary: {
            50:  '#fffbeb',
            100: '#fef3c7',
            200: '#fde68a',
            300: '#fcd34d',
            400: '#fbbf24',
            500: '#f59e0b',
            600: '#e8940d',
            700: '#d97706',
            800: '#b45309',
            900: '#92400e',
            950: '#78350f'
        },
        colorScheme: {
            light: {
                primary: {
                    color:         '{primary.600}',
                    inverseColor:  '#ffffff',
                    hoverColor:    '{primary.700}',
                    activeColor:   '{primary.800}'
                },
                highlight: {
                    background:    'rgba(232, 148, 13, 0.12)',
                    focusBackground: 'rgba(232, 148, 13, 0.20)',
                    color:         '{primary.700}',
                    focusColor:    '{primary.800}'
                },
                surface: {
                    0:   '#ffffff',
                    50:  '#f8fafc',
                    100: '#f1f5f9',
                    200: '#e2e8f0',
                    300: '#cbd5e1',
                    400: '#94a3b8',
                    500: '#64748b',
                    600: '#475569',
                    700: '#334155',
                    800: '#1e293b',
                    900: '#0f172a',
                    950: '#020617'
                }
            },
            dark: {
                primary: {
                    color:         '{primary.400}',
                    inverseColor:  '{surface.900}',
                    hoverColor:    '{primary.300}',
                    activeColor:   '{primary.200}'
                },
                highlight: {
                    background:    'rgba(245, 166, 35, 0.16)',
                    focusBackground: 'rgba(245, 166, 35, 0.25)',
                    color:         '{primary.300}',
                    focusColor:    '{primary.200}'
                },
                surface: {
                    0:   '#ffffff',
                    50:  '#f8fafc',
                    100: '#f1f5f9',
                    200: '#e2e8f0',
                    300: '#cbd5e1',
                    400: '#94a3b8',
                    500: '#64748b',
                    600: '#475569',
                    700: '#334155',
                    800: '#1e293b',
                    900: '#0f172a',
                    950: '#020617'
                }
            }
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
            preset: CustomFatortyPreset,
            options: {
                darkModeSelector: '.my-app-dark'
            }
        },
        license: environment.primeUiLicense
    })
  ]
};
