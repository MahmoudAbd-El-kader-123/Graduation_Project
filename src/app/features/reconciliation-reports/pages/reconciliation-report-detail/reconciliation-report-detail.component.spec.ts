import { ComponentFixture, TestBed } from '@angular/core/testing';
import { ActivatedRoute, Router, convertToParamMap, provideRouter } from '@angular/router';
import { of, throwError } from 'rxjs';
import { ReconciliationReportService } from '../../services/reconciliation-report.service';
import { ReconciliationReportDetailComponent } from './reconciliation-report-detail.component';

const reportResponse = {
  success: true,
  message: null,
  errors: null,
  data: {
    invoice: {
      invoiceId: 42,
      invoiceNumber: 'INV-42',
      purchaseOrderId: 7,
      purchaseOrderNumber: 'PO-7',
      vendorName: 'Vendor A',
      uploadedByUserEmail: 'uploader@example.com',
      uploadedAt: '2026-08-13T10:00:00Z',
      invoiceDate: '2026-08-12T00:00:00Z'
    },
    reconciliation: {
      invoiceId: 42,
      purchaseOrderId: 7,
      invoiceNumber: 'INV-42',
      status: 'NeedsReview',
      isReconciled: false,
      hasDiscrepancies: false,
      discrepancyCount: 0,
      items: [],
      discrepancies: []
    },
    statusMessage: 'Purchase order relationship needs review.'
  }
};

describe('ReconciliationReportDetailComponent', () => {
  let fixture: ComponentFixture<ReconciliationReportDetailComponent>;
  let reportService: { getReport: ReturnType<typeof vi.fn> };

  beforeEach(async () => {
    reportService = { getReport: vi.fn().mockReturnValue(of(reportResponse)) };
    await TestBed.configureTestingModule({
      imports: [ReconciliationReportDetailComponent],
      providers: [
        provideRouter([]),
        { provide: ActivatedRoute, useValue: { snapshot: { paramMap: convertToParamMap({ invoiceId: '42' }) } } },
        { provide: ReconciliationReportService, useValue: reportService }
      ]
    }).compileComponents();
  });

  it('loads the secured report snapshot and displays manual-review context', () => {
    fixture = TestBed.createComponent(ReconciliationReportDetailComponent);
    fixture.detectChanges();

    expect(reportService.getReport).toHaveBeenCalledWith(42);
    expect(fixture.nativeElement.textContent).toContain('Manual review required');
    expect(fixture.nativeElement.textContent).toContain('Purchase order relationship needs review.');
    expect(fixture.nativeElement.textContent).not.toContain('Resolve');
    expect(fixture.nativeElement.textContent).not.toContain('Rematch');
  });

  it('navigates to unauthorized when the secured endpoint returns 403', () => {
    reportService.getReport.mockReturnValue(throwError(() => ({ status: 403 })));
    const router = TestBed.inject(Router);
    const navigate = vi.spyOn(router, 'navigate').mockResolvedValue(true);

    fixture = TestBed.createComponent(ReconciliationReportDetailComponent);
    fixture.detectChanges();

    expect(navigate).toHaveBeenCalledWith(['/unauthorized']);
  });
});
