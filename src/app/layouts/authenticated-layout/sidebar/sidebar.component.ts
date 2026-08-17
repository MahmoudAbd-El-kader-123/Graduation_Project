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
  styles: [`
    :host {
      display: contents;
    }

    /* ── Desktop sidebar shell ── */
    .fatorty-sidebar {
      background-color: var(--fatorty-navy-900);
      border-right: 1px solid var(--fatorty-navy-800);
      display: flex;
      flex-direction: column;
      width: 16rem;
      height: 100vh;
      position: sticky;
      top: 0;
      left: 0;
      z-index: 20;
      flex-shrink: 0;
    }

    /* ── Logo area ── */
    .sidebar-logo-area {
      padding: 1.5rem 1.25rem;
      border-bottom: 1px solid var(--fatorty-navy-800);
      display: flex;
      align-items: center;
      justify-content: center;
      background: linear-gradient(160deg, var(--fatorty-navy-900) 0%, var(--fatorty-navy-800) 100%);
      min-height: 6rem;
    }

    .sidebar-logo-area img {
      height: 4.5rem;
      width: 100%;
      object-fit: contain;
      filter: drop-shadow(0 2px 10px rgba(245,166,35,0.3));
      transition: filter 0.2s ease;
      padding: 0 0.25rem;
    }

    .sidebar-logo-area img:hover {
      filter: drop-shadow(0 4px 18px rgba(245,166,35,0.55));
    }

    /* ── Nav scroll container ── */
    .sidebar-nav {
      flex: 1;
      overflow-y: auto;
      padding: 1rem 0.625rem;
      display: flex;
      flex-direction: column;
      gap: 1.25rem;
      scrollbar-width: thin;
      scrollbar-color: var(--fatorty-navy-700) transparent;
    }

    /* ── Group ── */
    .nav-group {
      display: flex;
      flex-direction: column;
      gap: 0.125rem;
    }

    .nav-group-label {
      padding: 0 0.75rem 0.375rem;
      font-size: 0.62rem;
      font-weight: 700;
      letter-spacing: 0.1em;
      text-transform: uppercase;
      color: var(--fatorty-navy-400);
    }

    /* ── Nav link ── */
    .nav-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      border-radius: 8px;
      color: #94A3B8;
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      border-left: 2px solid transparent;
      transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
      margin: 0;
    }

    .nav-link:hover {
      background: rgba(255,255,255,0.055);
      color: #E2E8F0;
    }

    .nav-link:hover svg {
      color: var(--fatorty-orange) !important;
    }

    /* Active state applied by routerLinkActive directive */
    .nav-link-active {
      background: rgba(245, 166, 35, 0.12) !important;
      color: #FFFFFF !important;
      border-left-color: var(--fatorty-orange) !important;
      font-weight: 600;
    }

    .nav-link-active svg {
      color: var(--fatorty-orange) !important;
    }

    svg {
      flex-shrink: 0;
      transition: color 0.15s ease;
    }

    /* ── Footer strip ── */
    .sidebar-footer {
      padding: 0.875rem 1.25rem;
      border-top: 1px solid var(--fatorty-navy-800);
      display: flex;
      align-items: center;
      gap: 0.5rem;
    }

    .sidebar-footer-dot {
      width: 7px;
      height: 7px;
      border-radius: 50%;
      background: #22C55E;
      box-shadow: 0 0 6px rgba(34,197,94,0.6);
      flex-shrink: 0;
    }

    .sidebar-footer-text {
      font-size: 0.7rem;
      color: var(--fatorty-navy-400);
      font-weight: 500;
    }

    /* ── Desktop visibility ── */
    @media (max-width: 1023px) {
      .fatorty-sidebar {
        display: none;
      }
    }

    /* ── Mobile drawer content styles ── */
    .mobile-nav-link {
      display: flex;
      align-items: center;
      gap: 0.75rem;
      padding: 0.625rem 0.75rem;
      border-radius: 8px;
      color: #94A3B8;
      font-size: 0.875rem;
      font-weight: 500;
      text-decoration: none;
      border-left: 2px solid transparent;
      transition: background 0.15s ease, color 0.15s ease, border-color 0.15s ease;
    }

    .mobile-nav-link:hover {
      background: rgba(13,27,42,0.06);
      color: #1E293B;
    }

    .mobile-nav-link-active {
      background: rgba(245, 166, 35, 0.10) !important;
      color: #B45309 !important;
      border-left-color: var(--fatorty-orange) !important;
      font-weight: 600;
    }
  `],
  template: `
    <!-- ── Desktop Sidebar ── -->
    <aside class="fatorty-sidebar">
      <!-- Logo -->
      <div class="sidebar-logo-area">
        <img src="logo.png" alt="Fatorty — Keep costs under control" />
      </div>

      <!-- Navigation -->
      <nav class="sidebar-nav">
        @for (group of navService.sidebar(); track group.label) {
          <div class="nav-group">
            <div class="nav-group-label">{{ group.label }}</div>
            @for (item of group.items; track item.route) {
              <a
                [routerLink]="item.route"
                routerLinkActive="nav-link-active"
                [routerLinkActiveOptions]="{ exact: false }"
                class="nav-link"
              >
                <svg [lucideIcon]="item.icon" [style.width.px]="18" [style.height.px]="18"></svg>
                <span>{{ item.label }}</span>
              </a>
            }
          </div>
        }
      </nav>

      <!-- Footer status -->
      <div class="sidebar-footer">
        <div class="sidebar-footer-dot"></div>
        <span class="sidebar-footer-text">System Online</span>
      </div>
    </aside>

    <!-- ── Mobile Drawer ── -->
    <p-drawer
      [visible]="layoutService.mobileMenuOpen()"
      (visibleChange)="layoutService.mobileMenuOpen.set($event)"
      [modal]="true"
      position="left"
      styleClass="w-64 !p-0"
    >
      <ng-template #header>
        <div style="display:flex;align-items:center;justify-content:center;width:100%;padding:0.5rem 0;">
          <img src="logo.png" alt="Fatorty" style="height:2.25rem;object-fit:contain;" />
        </div>
      </ng-template>
      <nav style="padding:1rem 0.625rem;display:flex;flex-direction:column;gap:1rem;">
        @for (group of navService.sidebar(); track group.label) {
          <div style="display:flex;flex-direction:column;gap:0.125rem;">
            <div style="padding:0 0.75rem 0.375rem;font-size:0.62rem;font-weight:700;letter-spacing:0.1em;text-transform:uppercase;color:#94A3B8;">
              {{ group.label }}
            </div>
            @for (item of group.items; track item.route) {
              <a
                [routerLink]="item.route"
                routerLinkActive="mobile-nav-link-active"
                [routerLinkActiveOptions]="{ exact: false }"
                (click)="layoutService.mobileMenuOpen.set(false)"
                class="mobile-nav-link"
              >
                <svg [lucideIcon]="item.icon" [style.width.px]="18" [style.height.px]="18"></svg>
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
