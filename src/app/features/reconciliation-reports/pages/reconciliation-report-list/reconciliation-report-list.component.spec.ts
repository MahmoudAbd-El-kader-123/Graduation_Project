import { ComponentFixture, TestBed } from '@angular/core/testing';
import { provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReconciliationReportService } from '../../services/reconciliation-report.service';
import { ReconciliationReportListComponent } from './reconciliation-report-list.component';

const reportPage = {
  success: true,
  message: null,
  errors: null,
  data: {
    items: [{
      invoiceId: 42,
      invoiceNumber: '<img src=x onerror=alert(1)>',
      purchaseOrderId: 7,
      purchaseOrderNumber: 'PO-7',
      vendorName: 'Vendor A',
      uploadedByUserEmail: 'manager@example.com',
      status: 'NeedsReview',
      hasDiscrepancies: true,
      discrepancyCount: 3,
      differentItemCount: 1,
      missingFromInvoiceCount: 1,
      missingFromPurchaseOrderCount: 1,
      uploadedAt: '2026-08-13T10:00:00Z',
      statusMessage: 'Manual review is required.'
    }],
    pageNumber: 1,
    pageSize: 12,
    totalCount: 1,
    totalPages: 1,
    hasPreviousPage: false,
    hasNextPage: false
  }
};

describe('ReconciliationReportListComponent', () => {
  let fixture: ComponentFixture<ReconciliationReportListComponent>;
  let reportService: { getReports: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = { getReports: vi.fn().mockReturnValue(of(reportPage)) };
    await TestBed.configureTestingModule({
      imports: [ReconciliationReportListComponent],
      providers: [
        provideRouter([]),
        { provide: ReconciliationReportService, useValue: reportService }
      ]
    }).compileComponents();
  });

  function createComponent(): void {
    fixture = TestBed.createComponent(ReconciliationReportListComponent);
    fixture.detectChanges();
  }

  it('renders actionable report cards and backend status messages as text', () => {
    createComponent();

    const element = fixture.nativeElement as HTMLElement;
    expect(element.textContent).toContain('<img src=x onerror=alert(1)>');
    expect(element.querySelector('img')).toBeNull();
    expect(element.textContent).toContain('1 different');
    expect(element.textContent).toContain('Manual review is required.');
  });

  it('resets server pagination when a filter changes', () => {
    createComponent();
    fixture.componentInstance.pageNumber.set(3);

    fixture.componentInstance.status.set('Failed');
    fixture.componentInstance.onFilterChange();

    expect(fixture.componentInstance.pageNumber()).toBe(1);
    expect(reportService.getReports).toHaveBeenLastCalledWith(
      expect.objectContaining({ pageNumber: 1, status: 'Failed' })
    );
  });

  it('shows an empty state when no reports match', () => {
    reportService.getReports.mockReturnValueOnce(of({
      ...reportPage,
      data: { ...reportPage.data, items: [], totalCount: 0 }
    }));
    createComponent();

    expect(fixture.nativeElement.textContent).toContain('No reconciliation reports found.');
  });

  it.each([
    [400, 'The report filters are invalid'],
    [403, 'You do not have permission']
  ])('shows a safe page error for HTTP %s', (status, expectedMessage) => {
    reportService.getReports.mockReturnValueOnce(throwError(() => ({ status })));
    createComponent();

    expect(fixture.nativeElement.textContent).toContain(expectedMessage);
  });
});
