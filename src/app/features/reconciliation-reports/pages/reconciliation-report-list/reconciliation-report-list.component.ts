import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, computed, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormsModule } from '@angular/forms';
import { RouterLink } from '@angular/router';
import { ButtonModule } from 'primeng/button';
import { DatePickerModule } from 'primeng/datepicker';
import { InputTextModule } from 'primeng/inputtext';
import { SelectModule } from 'primeng/select';
import { TagModule } from 'primeng/tag';
import { Subject, debounceTime, distinctUntilChanged } from 'rxjs';
import { EmptyStateComponent } from '../../../../shared/table/components/empty-state/empty-state';
import { ErrorStateComponent } from '../../../../shared/table/components/error-state/error-state';
import { PaginationComponent } from '../../../../shared/table/components/pagination/pagination';
import {
  ReconciliationReportQuery,
  ReconciliationReportSortDirection,
  ReconciliationReportSortField,
  ReconciliationReportSummary
} from '../../models/reconciliation-report.model';
import { ReconciliationReportService } from '../../services/reconciliation-report.service';

interface SelectOption<T> {
  label: string;
  value: T;
}

@Component({
  selector: 'app-reconciliation-report-list',
  imports: [
    FormsModule,
    RouterLink,
    ButtonModule,
    DatePickerModule,
    InputTextModule,
    SelectModule,
    TagModule,
    EmptyStateComponent,
    ErrorStateComponent,
    PaginationComponent
  ],
  templateUrl: './reconciliation-report-list.component.html'
})
export class ReconciliationReportListComponent {
  private readonly reportService = inject(ReconciliationReportService);
  private readonly destroyRef = inject(DestroyRef);
  private readonly searchChanges = new Subject<string>();

  readonly reports = signal<ReconciliationReportSummary[]>([]);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);
  readonly totalCount = signal(0);
  readonly pageNumber = signal(1);
  readonly pageSize = signal(12);

  readonly searchTerm = signal('');
  readonly status = signal<string | undefined>(undefined);
  readonly hasDiscrepancies = signal<boolean | undefined>(undefined);
  readonly fromDate = signal<Date | null>(null);
  readonly toDate = signal<Date | null>(null);
  readonly sortBy = signal<ReconciliationReportSortField>('uploadedAt');
  readonly sortDirection = signal<ReconciliationReportSortDirection>('desc');

  readonly filtersActive = computed(() =>
    this.searchTerm().trim().length > 0 ||
    this.status() !== undefined ||
    this.hasDiscrepancies() !== undefined ||
    this.fromDate() !== null ||
    this.toDate() !== null
  );

  readonly statusOptions: SelectOption<string | undefined>[] = [
    { label: 'All statuses', value: undefined },
    { label: 'Processing', value: 'Processing' },
    { label: 'Completed', value: 'Completed' },
    { label: 'Needs review', value: 'NeedsReview' },
    { label: 'Failed', value: 'Failed' }
  ];
  readonly discrepancyOptions: SelectOption<boolean | undefined>[] = [
    { label: 'All reports', value: undefined },
    { label: 'With discrepancies', value: true },
    { label: 'Without discrepancies', value: false }
  ];
  readonly sortOptions: SelectOption<ReconciliationReportSortField>[] = [
    { label: 'Uploaded date', value: 'uploadedAt' },
    { label: 'Discrepancy count', value: 'discrepancyCount' }
  ];
  readonly directionOptions: SelectOption<ReconciliationReportSortDirection>[] = [
    { label: 'Descending', value: 'desc' },
    { label: 'Ascending', value: 'asc' }
  ];

  constructor() {
    this.searchChanges.pipe(
      debounceTime(300),
      distinctUntilChanged(),
      takeUntilDestroyed(this.destroyRef)
    ).subscribe(() => this.resetPageAndLoad());

    this.loadReports();
  }

  onSearchChange(searchTerm: string): void {
    const limitedSearchTerm = searchTerm.slice(0, 100);
    this.searchTerm.set(limitedSearchTerm);
    this.searchChanges.next(limitedSearchTerm.trim());
  }

  onFilterChange(): void {
    this.resetPageAndLoad();
  }

  onPageChange(event: { pageNumber: number; pageSize: number }): void {
    this.pageNumber.set(event.pageNumber);
    this.pageSize.set(event.pageSize);
    this.loadReports();
  }

  clearFilters(): void {
    this.searchTerm.set('');
    this.status.set(undefined);
    this.hasDiscrepancies.set(undefined);
    this.fromDate.set(null);
    this.toDate.set(null);
    this.sortBy.set('uploadedAt');
    this.sortDirection.set('desc');
    this.resetPageAndLoad();
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

  formatDate(date: string): string {
    return new Date(date).toLocaleDateString('en-US', {
      year: 'numeric', month: 'short', day: '2-digit'
    });
  }

  private resetPageAndLoad(): void {
    this.pageNumber.set(1);
    this.loadReports();
  }

  private loadReports(): void {
    this.loading.set(true);
    this.error.set(null);

    this.reportService.getReports(this.buildQuery()).pipe(
      takeUntilDestroyed(this.destroyRef)
    ).subscribe({
      next: response => {
        this.reports.set(response.data.items);
        this.totalCount.set(response.data.totalCount);
        this.loading.set(false);
      },
      error: (error: HttpErrorResponse) => {
        this.error.set(this.errorMessage(error.status));
        this.loading.set(false);
      }
    });
  }

  private buildQuery(): ReconciliationReportQuery {
    const searchTerm = this.searchTerm().trim();
    return {
      pageNumber: this.pageNumber(),
      pageSize: this.pageSize(),
      searchTerm: searchTerm || undefined,
      status: this.status(),
      hasDiscrepancies: this.hasDiscrepancies(),
      fromDate: this.toDateOnly(this.fromDate()),
      toDate: this.toDateOnly(this.toDate()),
      sortBy: this.sortBy(),
      sortDirection: this.sortDirection()
    };
  }

  private toDateOnly(date: Date | null): string | undefined {
    if (!date) return undefined;
    const year = date.getFullYear();
    const month = String(date.getMonth() + 1).padStart(2, '0');
    const day = String(date.getDate()).padStart(2, '0');
    return `${year}-${month}-${day}`;
  }

  private errorMessage(status: number): string {
    switch (status) {
      case 400: return 'The report filters are invalid. Review the selected values and try again.';
      case 401: return 'Your session has expired. Sign in again to view reconciliation reports.';
      case 403: return 'You do not have permission to view reconciliation reports.';
      default: return 'Reconciliation reports could not be loaded. Please try again.';
    }
  }
}
