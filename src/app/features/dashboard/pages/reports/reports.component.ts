import { Component } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TagModule } from 'primeng/tag';

@Component({
  selector: 'app-reports',
  imports: [RouterLink, TagModule],
  template: `
    <div class="p-6">
      <div class="mb-6">
        <h1 class="text-2xl font-semibold text-surface-900 dark:text-surface-0">Reports</h1>
        <p class="mt-1 text-surface-600 dark:text-surface-300">Review operational exceptions and reconciliation results.</p>
      </div>

      <a
        routerLink="/dashboard/reports/reconciliation"
        class="block max-w-xl rounded-xl border border-surface-200 bg-surface-0 p-5 shadow-sm transition hover:border-primary focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-primary dark:border-surface-700 dark:bg-surface-900">
        <div class="flex items-start justify-between gap-4">
          <div>
            <h2 class="text-lg font-semibold text-surface-900 dark:text-surface-0">Invoice Reconciliation</h2>
            <p class="mt-2 text-sm text-surface-600 dark:text-surface-300">
              Review invoice-to-purchase-order differences across the system.
            </p>
          </div>
          <p-tag value="View reports" severity="info" [rounded]="true"></p-tag>
        </div>
      </a>
    </div>
  `
})
export class ReportsComponent {}
