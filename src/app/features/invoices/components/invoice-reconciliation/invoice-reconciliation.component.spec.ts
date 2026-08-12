import { ComponentFixture, TestBed } from '@angular/core/testing';
import { InvoiceReconciliation } from '../../models/invoice.model';
import { InvoiceReconciliationComponent } from './invoice-reconciliation.component';

function reconciliation(
  status: string,
  hasDiscrepancies = false
): InvoiceReconciliation {
  return {
    invoiceId: 42,
    purchaseOrderId: 7,
    invoiceNumber: 'INV-42',
    status,
    isReconciled: status === 'Completed',
    hasDiscrepancies,
    discrepancyCount: hasDiscrepancies ? 1 : 0,
    discrepancies: hasDiscrepancies
      ? [{
          id: 1,
          discrepancyType: 'MissingSku',
          fieldName: 'SupplierSku',
          expectedValue: 'N/A',
          actualValue: 'SKU-404',
          isResolved: false
        }]
      : []
  };
}

describe('InvoiceReconciliationComponent', () => {
  let fixture: ComponentFixture<InvoiceReconciliationComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      imports: [InvoiceReconciliationComponent]
    }).compileComponents();

    fixture = TestBed.createComponent(InvoiceReconciliationComponent);
  });

  it('shows processing feedback before a terminal result arrives', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Comparing invoice with purchase order');
  });

  it.each([
    ['Completed', false, 'Invoice matches the purchase order'],
    ['NeedsReview', false, 'Manual review required'],
    ['Failed', false, 'Invoice comparison failed']
  ])('shows the %s terminal state', (status, hasDiscrepancies, expectedText) => {
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('reconciliation', reconciliation(status, hasDiscrepancies));
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain(expectedText);
  });

  it('shows completed differences with contract labels and preserves N/A text', () => {
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('reconciliation', reconciliation('Completed', true));
    fixture.detectChanges();

    const text = fixture.nativeElement.textContent as string;
    expect(text).toContain('1 difference');
    expect(text).toContain('Invoice SKU not found in the purchase order');
    expect(text).toContain('Expected (PO)');
    expect(text).toContain('Actual (Invoice)');
    expect(text).toContain('N/A');
    expect(text).toContain('SKU-404');
    expect(text).toContain('Open');
  });
});
