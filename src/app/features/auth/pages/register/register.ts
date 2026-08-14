import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router, RouterLink } from '@angular/router';
import { FormBuilder, ReactiveFormsModule, Validators, AbstractControl, ValidationErrors } from '@angular/forms';
import { InputTextModule } from 'primeng/inputtext';
import { PasswordModule } from 'primeng/password';
import { ButtonModule } from 'primeng/button';
import { toast } from 'ngx-sonner';
import { AuthService } from '../../../../core/auth/services/auth.service';
import { AuthStorageService } from '../../../../core/auth/services/auth-storage.service';
import { ThemeService } from '../../../../core/services/theme.service';
import { LucideDynamicIcon, LucideMoon as Moon, LucideSun as Sun, LucideMonitor as Monitor } from '@lucide/angular';
import { AUTH_ROUTES } from '../../../../core/auth/constants/auth.constants';

@Component({
  selector: 'app-register',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, RouterLink, InputTextModule, PasswordModule, ButtonModule, LucideDynamicIcon],
  templateUrl: './register.html'
})
export class Register {
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

  readonly registerForm = this.fb.group({
    fullName: ['', [Validators.required]],
    userName: ['', [Validators.required]],
    email: ['', [Validators.required, Validators.email]],
    phoneNumber: ['', [Validators.required]],
    password: ['', [Validators.required, Validators.minLength(6)]],
    confirmPassword: ['', [Validators.required]]
  }, { validators: this.passwordMatchValidator });

  passwordMatchValidator(control: AbstractControl): ValidationErrors | null {
    const password = control.get('password')?.value;
    const confirmPassword = control.get('confirmPassword')?.value;
    return password === confirmPassword ? null : { passwordMismatch: true };
  }

  onSubmit(): void {
    if (this.registerForm.invalid) {
      this.registerForm.markAllAsTouched();
      return;
    }

    this.isLoading.set(true);
    const formValue = this.registerForm.value;

    this.authService.register({
      fullName: formValue.fullName!,
      userName: formValue.userName!,
      email: formValue.email!,
      phoneNumber: formValue.phoneNumber!,
      password: formValue.password!,
      confirmPassword: formValue.confirmPassword!
    }).subscribe({
      next: () => {
        toast.success('Registration successful! Please log in.');
        this.router.navigate([AUTH_ROUTES.login]);
        this.isLoading.set(false);
      },
      error: () => {
        this.isLoading.set(false);
      }
    });
  }

  toggleTheme(): void {
    this.themeService.toggleTheme();
  }
}
