import { Component, signal, inject } from '@angular/core';
import { RouterOutlet, Router, Event } from '@angular/router';
import { NgxSonnerToaster } from 'ngx-sonner';
import { ToastModule } from 'primeng/toast';
import { LanguageService } from './core/i18n/language.service';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, NgxSonnerToaster, ToastModule],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {
  protected readonly title = signal('SPIP.Frontend');
  private readonly router = inject(Router);
  // Constructed at the application root so localization also covers auth and error pages.
  private readonly languageService = inject(LanguageService);

}
