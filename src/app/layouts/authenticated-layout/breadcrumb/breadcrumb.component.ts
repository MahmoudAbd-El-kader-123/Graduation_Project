import { Component, inject, computed } from '@angular/core';
import { Router, NavigationEnd, RouterLink } from '@angular/router';
import { toSignal } from '@angular/core/rxjs-interop';
import { filter, map } from 'rxjs';
import { LucideDynamicIcon, LucideChevronRight as ChevronRight, LucideHome as Home } from '@lucide/angular';

interface BreadcrumbItem {
  label: string;
  url?: string;
}

@Component({
  selector: 'app-breadcrumb',
  standalone: true,
  imports: [RouterLink, LucideDynamicIcon],
  template: `
    <nav class="flex items-center space-x-1 text-sm font-medium text-surface-500">
      <a routerLink="/dashboard" class="flex items-center hover:text-primary transition-colors">
        <svg lucideIcon="home" [style.width.px]="16" [style.height.px]="16"></svg>
      </a>
      
      @for (item of breadcrumbs(); track item.label; let last = $last) {
        <svg lucideIcon="chevron-right" [style.width.px]="16" [style.height.px]="16" class="text-surface-400"></svg>
        
        @if (!last && item.url) {
          <a [routerLink]="item.url" class="hover:text-primary transition-colors">
            {{ item.label }}
          </a>
        } @else {
          <span class="text-surface-900 dark:text-surface-0 font-semibold">{{ item.label }}</span>
        }
      }
    </nav>
  `
})
export class BreadcrumbComponent {
  private router = inject(Router);

  // For now, we will simply derive breadcrumb from current url segments.
  // In a real app we might use route data.
  readonly breadcrumbs = toSignal(
    this.router.events.pipe(
      filter((event): event is NavigationEnd => event instanceof NavigationEnd),
      map(event => this.createBreadcrumbs(event.urlAfterRedirects))
    ),
    { initialValue: this.createBreadcrumbs(this.router.url) }
  );

  private createBreadcrumbs(url: string): BreadcrumbItem[] {
    const segments = url.split('/').filter(s => s);
    const items: BreadcrumbItem[] = [];
    let currentUrl = '';

    for (const segment of segments) {
      currentUrl += `/${segment}`;
      // Capitalize first letter and format
      const label = segment.charAt(0).toUpperCase() + segment.slice(1).replace(/-/g, ' ');
      
      // We skip 'dashboard' since it's our root home icon
      if (segment.toLowerCase() !== 'dashboard') {
        items.push({ label, url: currentUrl });
      }
    }
    
    return items;
  }
}
