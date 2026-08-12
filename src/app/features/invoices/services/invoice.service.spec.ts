import { provideHttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { ApiResponse } from '../../../shared/api/models/api-response.model';
import { ApiService } from '../../../shared/api/services/api.service';
import { InvoiceReconciliation } from '../models/invoice.model';
import { InvoiceService } from './invoice.service';

function response(status: string): ApiResponse<InvoiceReconciliation> {
  return {
    success: true,
    message: null,
    errors: null,
    data: {
      invoiceId: 42,
      purchaseOrderId: 7,
      invoiceNumber: 'INV-42',
      status,
      isReconciled: status === 'Completed',
      hasDiscrepancies: false,
      discrepancyCount: 0,
      discrepancies: []
    }
  };
}

describe('InvoiceService reconciliation polling', () => {
  let httpController: HttpTestingController;

  beforeEach(() => {
    vi.useFakeTimers();
    TestBed.configureTestingModule({
      providers: [
        provideHttpClient(),
        provideHttpClientTesting(),
        InvoiceService,
        ApiService
      ]
    });
    httpController = TestBed.inject(HttpTestingController);
  });

  afterEach(() => {
    httpController.verify();
    vi.useRealTimers();
  });

  it('stops polling after emitting the first terminal reconciliation state', async () => {
    const statuses: string[] = [];
    let completed = false;
    TestBed.inject(InvoiceService).pollReconciliation(42).subscribe({
      next: result => statuses.push(result.data.status),
      complete: () => completed = true
    });

    await vi.advanceTimersByTimeAsync(0);
    httpController.expectOne(request => request.url.endsWith('/invoices/42/reconciliation'))
      .flush(response('Processing'));

    await vi.advanceTimersByTimeAsync(2500);
    httpController.expectOne(request => request.url.endsWith('/invoices/42/reconciliation'))
      .flush(response('Completed'));

    await vi.advanceTimersByTimeAsync(5000);

    expect(statuses).toEqual(['Processing', 'Completed']);
    expect(completed).toBe(true);
  });
});
