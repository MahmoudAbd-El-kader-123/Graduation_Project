import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiService } from '../../../shared/api/services/api.service';
import { ReconciliationReportQuery } from '../models/reconciliation-report.model';
import { ReconciliationReportService } from './reconciliation-report.service';

describe('ReconciliationReportService', () => {
  let service: ReconciliationReportService;
  let httpController: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        ApiService,
        ReconciliationReportService
      ]
    });
    service = TestBed.inject(ReconciliationReportService);
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => httpController.verify());

  it('maps supported list filters, sorting, and pagination to the report endpoint', () => {
    const query: ReconciliationReportQuery = {
      pageNumber: 2,
      pageSize: 24,
      searchTerm: 'INV-100',
      status: 'NeedsReview',
      hasDiscrepancies: false,
      fromDate: '2026-08-01',
      toDate: '2026-08-13',
      sortBy: 'discrepancyCount',
      sortDirection: 'asc'
    };

    service.getReports(query).subscribe();

    const request = httpController.expectOne(httpRequest =>
      httpRequest.url.endsWith('/reconciliation-reports'));
    expect(request.request.params.get('pageNumber')).toBe('2');
    expect(request.request.params.get('pageSize')).toBe('24');
    expect(request.request.params.get('searchTerm')).toBe('INV-100');
    expect(request.request.params.get('status')).toBe('NeedsReview');
    expect(request.request.params.get('hasDiscrepancies')).toBe('false');
    expect(request.request.params.get('fromDate')).toBe('2026-08-01');
    expect(request.request.params.get('toDate')).toBe('2026-08-13');
    expect(request.request.params.get('sortBy')).toBe('discrepancyCount');
    expect(request.request.params.get('sortDirection')).toBe('asc');
    expect(request.request.params.has('tenantId')).toBe(false);
    request.flush({ success: true, message: null, errors: null, data: { items: [] } });
  });

  it('uses only the secured reporting endpoint for report detail', () => {
    service.getReport(42).subscribe();

    const request = httpController.expectOne(httpRequest =>
      httpRequest.url.endsWith('/reconciliation-reports/42'));
    expect(request.request.url).not.toContain('/invoices/');
    request.flush({ success: true, message: null, errors: null, data: {} });
  });
});
