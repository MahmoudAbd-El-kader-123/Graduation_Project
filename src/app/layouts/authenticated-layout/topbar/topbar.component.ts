import { Component, inject, output, signal, computed } from '@angular/core';
import { LucideAngularModule } from 'lucide-angular';
import { Button } from 'primeng/button';
import { ThemeService } from '../../../core/services/theme.service';
import { LanguageService } from '../../../core/i18n/language.service';
import { BreadcrumbComponent } from '../breadcrumb/breadcrumb.component';
import { UserMenuComponent } from '../user-menu/user-menu.component';

@Component({
  selector: 'app-topbar',
  standalone: true,
  imports: [LucideAngularModule, Button, BreadcrumbComponent, UserMenuComponent],
  template: `
    <header class="h-16 bg-surface-0 dark:bg-surface-900 border-b border-surface-200 dark:border-surface-700 flex items-center justify-between px-4 lg:px-6 sticky top-0 z-10 w-full">
      <div class="flex items-center gap-4">
        <!-- Mobile Menu Toggle -->
        <p-button variant="text" severity="secondary" rounded="true" styleClass="lg:!hidden !p-2" (onClick)="toggleSidebar()">
          <lucide-icon name="menu" [size]="20"></lucide-icon>
        </p-button>
        
        <!-- Breadcrumb -->
        <div class="hidden md:block">
          <app-breadcrumb />
        </div>
      </div>

      <div class="flex items-center gap-3">
        <!-- Language Switcher -->
        <div class="language-switcher" role="group" aria-label="Language">
          <button
            type="button"
            class="language-option"
            [class.language-option-active]="languageService.language() === 'en'"
            [attr.aria-pressed]="languageService.language() === 'en'"
            (click)="languageService.setLanguage('en')">
            English
          </button>
          <span class="language-divider" aria-hidden="true"></span>
          <button
            type="button"
            class="language-option"
            [class.language-option-active]="languageService.language() === 'ar'"
            [attr.aria-pressed]="languageService.language() === 'ar'"
            (click)="languageService.setLanguage('ar')">
            العربية
          </button>
        </div>

        <!-- Theme Toggle -->
        <p-button variant="text" severity="secondary" rounded="true" styleClass="!p-2" (onClick)="toggleTheme()">
          @if (themeService.currentTheme() === 'dark' || (themeService.currentTheme() === 'system' && isSystemDark())) {
            <lucide-icon name="sun" [size]="20"></lucide-icon>
          } @else {
            <lucide-icon name="moon" [size]="20"></lucide-icon>
          }
        </p-button>

        <!-- User Menu -->
        <app-user-menu />
      </div>
    </header>
  `,
  styles: [`
    .language-switcher {
      display: inline-flex;
      align-items: center;
      height: 2.25rem;
      padding: 0.2rem;
      border: 1px solid var(--p-surface-200);
      border-radius: 0.65rem;
      background: var(--p-surface-50);
    }

    :host-context(.my-app-dark) .language-switcher {
      border-color: var(--p-surface-700);
      background: var(--p-surface-800);
    }

    .language-option {
      height: 1.75rem;
      padding: 0 0.6rem;
      border: 0;
      border-radius: 0.45rem;
      background: transparent;
      color: var(--p-surface-600);
      font: inherit;
      font-size: 0.8rem;
      font-weight: 600;
      cursor: pointer;
      transition: color 150ms ease, background 150ms ease, box-shadow 150ms ease;
    }

    .language-option:hover {
      color: var(--p-primary-color);
    }

    .language-option-active {
      color: var(--p-primary-contrast-color);
      background: var(--p-primary-color);
      box-shadow: 0 1px 3px rgb(0 0 0 / 12%);
    }

    .language-divider {
      width: 1px;
      height: 1rem;
      background: var(--p-surface-300);
    }

    :host-context(.my-app-dark) .language-divider {
      background: var(--p-surface-600);
    }

    @media (max-width: 480px) {
      .language-option { padding-inline: 0.4rem; font-size: 0.75rem; }
    }
  `]
})
export class TopbarComponent {
  readonly themeService = inject(ThemeService);
  readonly languageService = inject(LanguageService);
  
  readonly menuToggle = output<void>();
  
  // Track system preference robustly
  readonly isSystemDark = signal<boolean>(window.matchMedia('(prefers-color-scheme: dark)').matches);

  constructor() {
    window.matchMedia('(prefers-color-scheme: dark)').addEventListener('change', (e) => {
      this.isSystemDark.set(e.matches);
    });
  }

  toggleSidebar(): void {
    this.menuToggle.emit();
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
