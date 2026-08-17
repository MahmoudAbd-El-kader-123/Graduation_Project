import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { toast } from 'ngx-sonner';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { AuthStorageService } from '../../../../core/auth/services/auth-storage.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { LucideDynamicIcon, LucideMoon as Moon, LucideSun as Sun, LucideMonitor as Monitor } from '@lucide/angular';

@Component({
  selector: 'app-login',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, InputTextModule, PasswordModule, ButtonModule, LucideDynamicIcon],
  templateUrl: './login.html',
  styleUrl: './login.css'
})
export class Login {
  private readonly fb = inject(FormBuilder);
  private readonly authService = inject(AuthService);
  private readonly authStorage = inject(AuthStorageService);
  private readonly router = inject(Router);
  public readonly themeService = inject(ThemeService);

  readonly isLoading = signal(false);
  readonly icons = { Moon, Sun, Monitor };

  constructor() {
    if (this.authStorage.isAuthenticated()) {
      this.router.navigate(['/dashboard']);
    }
  }

  readonly loginForm = this.fb.group({
    email: ['', [Validators.required, Validators.email]],
    password: ['', [Validators.required]]
  });

  onSubmit(): void {
    if (this.loginForm.invalid) {
      this.loginForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const { email, password } = this.loginForm.value;

    this.authService.login({ email: email!, password: password! }).subscribe({
      next: () => {
        toast.success('Successfully logged in');
        this.router.navigate(['/dashboard']);
        this.isLoading.set(false);
      },
      error: (err) => {
        this.isLoading.set(false);
        const interceptorHandled = err.error && (
          (err.error.errors && Array.isArray(err.error.errors)) || 
          typeof err.error.message === 'string'
        );
        
        if (!interceptorHandled) {
          const errorMessage = err.error?.title || err.error?.detail || (typeof err.error === 'string' ? err.error : 'Login failed. Please check your credentials.');
          toast.error(errorMessage);
        }
      }
    });
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
