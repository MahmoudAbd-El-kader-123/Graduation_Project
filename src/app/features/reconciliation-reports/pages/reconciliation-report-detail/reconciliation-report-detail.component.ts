import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { ActivatedRoute, Router, RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { TagModule } from 'primeng/tag';
import { InvoiceReconciliationComponent } from '../../../invoices/components/invoice-reconciliation/invoice-reconciliation.component';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { ManagerReconciliationReport } from '../../models/reconciliation-report.model';
import { ReconciliationReportService } from '../../services/reconciliation-report.service';

@Component({
  selector: 'app-reconciliation-report-detail',
  imports: [
    RouterLink,
    ButtonModule,
    TagModule,
    InvoiceReconciliationComponent,
    ErrorStateComponent
  ],
  templateUrl: './reconciliation-report-detail.component.html'
})
export class ReconciliationReportDetailComponent {
  private readonly route = inject(ActivatedRoute);
  private readonly router = inject(Router);
  private readonly reportService = inject(ReconciliationReportService);
  private readonly destroyRef = inject(DestroyRef);

  readonly report = signal<ManagerReconciliationReport | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  constructor() {
    const invoiceId = Number(this.route.snapshot.paramMap.get('invoiceId'));
    if (!Number.isInteger(invoiceId) || invoiceId <= 0) {
      this.error.set('The reconciliation report address is invalid.');
      this.loading.set(false);
      return;
    }

    this.loadReport(invoiceId);
  }

  statusSeverity(status: string): 'success' | 'info' | 'warn' | 'danger' | 'secondary' {
    switch (status.toLowerCase()) {
      case 'completed': return 'success';
      case 'processing': return 'info';
      case 'needsreview': return 'warn';
      case 'failed': return 'danger';
      default: return 'secondary';
    }
  }

  formatDate(date: string | null): string {
    if (!date) return 'N/A';
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: '2-digit'
    });
  }

  private loadReport(invoiceId: number): void {
    this.reportService.getReport(invoiceId).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: response => {
        this.report.set(response.data);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        if (error.status === 403) {
          this.router.navigate(['/unauthorized']);
          return;
        }
        this.error.set(error.status === 404
          ? 'The reconciliation report was not found.'
          : error.status === 401
            ? 'Your session has expired. Sign in again to view this report.'
            : 'The reconciliation report could not be loaded. Please try again.');
        this.loading.set(false);
      }
    });
  }
}
