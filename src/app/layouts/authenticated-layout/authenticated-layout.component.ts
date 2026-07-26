import { Component, viewChild } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { SidebarComponent } from './sidebar/sidebar.component';
import { TopbarComponent } from './topbar/topbar.component';
import { ConfirmDialogModule } from 'primeng/confirmdialog';

@Component({
  selector: 'app-authenticated-layout',
  standalone: true,
  imports: [RouterOutlet, SidebarComponent, TopbarComponent, ConfirmDialogModule],
  template: `
    <div class="flex h-screen w-full bg-surface-50 dark:bg-surface-950 overflow-hidden">
      <!-- Sidebar -->
      <app-sidebar #sidebar />

      <!-- Main Content -->
      <div class="flex flex-col flex-1 min-w-0">
        <!-- Topbar -->
        <app-topbar (menuToggle)="sidebar.toggleMobile()" />

        <!-- Main Scrollable Area -->
        <main class="flex-1 overflow-y-auto p-4 lg:p-8">
          <div class="mx-auto max-w-7xl">
            <router-outlet />
          </div>
        </main>
      </div>
    </div>
    <p-confirmdialog />
  `
})
export class AuthenticatedLayoutComponent {
}
