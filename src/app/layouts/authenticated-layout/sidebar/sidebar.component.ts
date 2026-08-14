import { LucideDynamicIcon } from '@lucide/angular';
import { Component, inject } from '@angular/core';
import { RouterLink, RouterLinkActive } from '@angular/router';
import { Drawer } from 'primeng/drawer';
import { DashboardNavigationService } from '../../../features/dashboard/services/dashboard-navigation.service';
import { LayoutService } from '../services/layout.service';

@Component({
  selector: 'app-sidebar',
  standalone: true,
  imports: [RouterLink, RouterLinkActive, Drawer, LucideDynamicIcon],
  template: `
    <!-- Desktop Sidebar -->
    <aside class="hidden lg:flex flex-col w-64 h-screen bg-surface-0 dark:bg-surface-900 border-r border-surface-200 dark:border-surface-700 sticky top-0 left-0 z-20">
      <div class="p-6 border-b border-surface-200 dark:border-surface-700 flex items-center gap-2">
        <div class="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-primary-contrast font-bold text-xl">S</div>
        <span class="text-xl font-bold text-surface-900 dark:text-surface-0">SPIP</span>
      </div>
      <nav class="flex-1 overflow-y-auto py-4 px-3 flex flex-col gap-4">
        @for (group of navService.sidebar(); track group.label) {
          <div class="flex flex-col gap-1">
            <div class="px-3 py-2 text-xs font-semibold text-surface-500 uppercase tracking-wider">
              {{ group.label }}
            </div>
            @for (item of group.items; track item.route) {
              <a [routerLink]="item.route" 
                 routerLinkActive="bg-primary/10 text-primary font-medium" 
                 [routerLinkActiveOptions]="{exact: false}"
                 class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-surface-600 dark:text-surface-300 hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors">
                <svg [lucideIcon]="item.icon" [style.width.px]="20" [style.height.px]="20"></svg>
                <span>{{ item.label }}</span>
              </a>
            }
          </div>
        }
      </nav>
    </aside>

    <!-- Mobile Drawer -->
    <p-drawer [visible]="layoutService.mobileMenuOpen()" (visibleChange)="layoutService.mobileMenuOpen.set($event)" [modal]="true" position="left" styleClass="w-64 !p-0">
      <ng-template #header>
        <div class="flex items-center gap-2 w-full">
          <div class="w-8 h-8 bg-primary rounded-lg flex items-center justify-center text-primary-contrast font-bold text-xl">S</div>
          <span class="text-xl font-bold text-surface-900 dark:text-surface-0">SPIP</span>
        </div>
      </ng-template>
      <nav class="flex-1 py-4 px-3 flex flex-col gap-4">
        @for (group of navService.sidebar(); track group.label) {
          <div class="flex flex-col gap-1">
            <div class="px-3 py-2 text-xs font-semibold text-surface-500 uppercase tracking-wider">
              {{ group.label }}
            </div>
            @for (item of group.items; track item.route) {
              <a [routerLink]="item.route" 
                 routerLinkActive="bg-primary/10 text-primary font-medium" 
                 [routerLinkActiveOptions]="{exact: false}"
                 (click)="layoutService.mobileMenuOpen.set(false)"
                 class="flex items-center gap-3 px-3 py-2.5 rounded-lg text-surface-600 dark:text-surface-300 hover:bg-surface-100 dark:hover:bg-surface-800 transition-colors">
                <svg [lucideIcon]="item.icon" [style.width.px]="20" [style.height.px]="20"></svg>
                <span>{{ item.label }}</span>
              </a>
            }
          </div>
        }
      </nav>
    </p-drawer>
  `
})
export class SidebarComponent {
  readonly navService = inject(DashboardNavigationService);
  readonly layoutService = inject(LayoutService);

  toggleMobile(): void {
    this.layoutService.mobileMenuOpen.update(v => !v);
  }
}
