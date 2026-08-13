import { Injectable, inject } from '@angular/core';
import { Observable } from 'rxjs';
import { ApiResponse } from '../../../shared/api/models/api-response.model';
import { PagedResult } from '../../../shared/api/models/paged-result.model';
import { ApiService } from '../../../shared/api/services/api.service';
import {
  ManagerReconciliationReport,
  ReconciliationReportQuery,
  ReconciliationReportSummary
} from '../models/reconciliation-report.model';

const RECONCILIATION_REPORT_ENDPOINT = '/reconciliation-reports';

@Injectable()
export class ReconciliationReportService {
  private readonly api = inject(ApiService);

  getReports(query: ReconciliationReportQuery): Observable<ApiResponse<PagedResult<ReconciliationReportSummary>>> {
    return this.api.get<ApiResponse<PagedResult<ReconciliationReportSummary>>>(
      RECONCILIATION_REPORT_ENDPOINT,
      this.toQueryParams(query)
    );
  }

  getReport(invoiceId: number): Observable<ApiResponse<ManagerReconciliationReport>> {
    return this.api.get<ApiResponse<ManagerReconciliationReport>>(
      `${RECONCILIATION_REPORT_ENDPOINT}/${invoiceId}`
    );
  }

  private toQueryParams(query: ReconciliationReportQuery): Record<string, string | number | boolean> {
    const parameters: Record<string, string | number | boolean> = {
      pageNumber: query.pageNumber,
      pageSize: query.pageSize,
      sortBy: query.sortBy,
      sortDirection: query.sortDirection
    };

    if (query.searchTerm) parameters['searchTerm'] = query.searchTerm;
    if (query.status) parameters['status'] = query.status;
    if (query.hasDiscrepancies !== undefined) parameters['hasDiscrepancies'] = query.hasDiscrepancies;
    if (query.fromDate) parameters['fromDate'] = query.fromDate;
    if (query.toDate) parameters['toDate'] = query.toDate;

    return parameters;
  }
}
