import { ComponentFixture, TestBed } from '@angular/core/testing';
import {
  DiscrepancyType,
  InvoiceDiscrepancy,
  InvoiceReconciliation,
  ReconciliationItem,
  ReconciliationItemStatus
} from '../../models/invoice.model';
import { InvoiceReconciliationComponent } from './invoice-reconciliation.component';

function discrepancy(
  id: number,
  discrepancyType: DiscrepancyType,
  reconciliationItemId: number | null,
  fieldName: string,
  expectedValue: string,
  actualValue: string
): InvoiceDiscrepancy {
  return {
    id,
    discrepancyType,
    fieldName,
    reconciliationItemId,
    purchaseOrderItemId: reconciliationItemId === null ? null : 100 + reconciliationItemId,
    invoiceItemId: reconciliationItemId === null ? null : 200 + reconciliationItemId,
    purchaseOrderSku: reconciliationItemId === null ? null : `PO-${reconciliationItemId}`,
    invoiceSku: reconciliationItemId === null ? null : `INV-${reconciliationItemId}`,
    productName: reconciliationItemId === null ? null : `Product ${reconciliationItemId}`,
    expectedValue,
    actualValue,
    isResolved: false
  };
}

function reconciliationItem(
  id: number,
  status: ReconciliationItemStatus,
  discrepancies: InvoiceDiscrepancy[] = []
): ReconciliationItem {
  const missingFromInvoice = status === 'MissingFromInvoice';
  const missingFromPurchaseOrder = status === 'MissingFromPurchaseOrder';

  return {
    id,
    purchaseOrderItemId: missingFromPurchaseOrder ? null : 100 + id,
    invoiceItemId: missingFromInvoice ? null : 200 + id,
    purchaseOrderSku: missingFromPurchaseOrder ? null : `PO-${id}`,
    invoiceSku: missingFromInvoice ? null : `INV-${id}`,
    productName: `Product ${id}`,
    expectedQuantity: missingFromPurchaseOrder ? 'N/A' : '5',
    actualQuantity: missingFromInvoice ? 'N/A' : status === 'Different' ? '6' : '5',
    expectedUnitPrice: missingFromPurchaseOrder ? 'N/A' : '11.00',
    actualUnitPrice: missingFromInvoice ? 'N/A' : '11.00',
    expectedAmount: missingFromPurchaseOrder ? 'N/A' : '55.00',
    actualAmount: missingFromInvoice ? 'N/A' : status === 'Different' ? '66.00' : '55.00',
    status,
    discrepancies
  };
}

function reconciliation(
  status: string,
  items: ReconciliationItem[] = [],
  discrepancies: InvoiceDiscrepancy[] = []
): InvoiceReconciliation {
  return {
    invoiceId: 42,
    purchaseOrderId: 7,
    invoiceNumber: 'INV-42',
    status,
    isReconciled: status === 'Completed',
    hasDiscrepancies: discrepancies.length > 0,
    discrepancyCount: discrepancies.length,
    items,
    discrepancies
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

  function render(result: InvoiceReconciliation): HTMLElement {
    fixture.componentRef.setInput('loading', false);
    fixture.componentRef.setInput('reconciliation', result);
    fixture.detectChanges();
    return fixture.nativeElement as HTMLElement;
  }

  it('shows processing feedback before a terminal result arrives', () => {
    fixture.detectChanges();

    expect(fixture.nativeElement.textContent).toContain('Comparing invoice with purchase order');
  });

  it.each([
    ['NeedsReview', 'Manual review required'],
    ['Failed', 'Invoice comparison failed']
  ])('shows the %s terminal state', (status, expectedText) => {
    const element = render(reconciliation(status));

    expect(element.textContent).toContain(expectedText);
  });

  it('hides matched rows after a completed reconciliation without differences', () => {
    const element = render(reconciliation('Completed', [reconciliationItem(1, 'Matched')]));
    const row = element.querySelector('[data-reconciliation-item-id="1"]');

    expect(element.textContent).toContain('Invoice matches the purchase order');
    expect(row).toBeNull();
    expect(element.textContent).not.toContain('Item comparisons');
  });

  it('renders backend-provided different and missing item statuses with safe N/A values', () => {
    const quantityDifference = discrepancy(11, 'QuantityMismatch', 2, 'Quantity', '5', '6');
    const items = [
      reconciliationItem(2, 'Different', [quantityDifference]),
      reconciliationItem(3, 'MissingFromInvoice'),
      reconciliationItem(4, 'MissingFromPurchaseOrder')
    ];
    const element = render(reconciliation('Completed', [reconciliationItem(1, 'Matched'), ...items], [quantityDifference]));

    expect(element.querySelector('[data-reconciliation-item-id="1"]')).toBeNull();
    expect(element.querySelector('[data-reconciliation-item-id="2"]')?.textContent)
      .toContain('Quantity mismatch: 5 expected, 6 actual');
    expect(element.querySelector('[data-reconciliation-item-id="3"]')?.textContent)
      .toContain('Expected purchase-order item was not found in the invoice.');
    expect(element.querySelector('[data-reconciliation-item-id="4"]')?.textContent)
      .toContain('Invoice item was not found in the purchase order.');
    expect(element.textContent).toContain('N/A');
    expect(element.textContent).toContain('Differences require attention');
  });

  it('keeps invoice-level differences separate from item discrepancies', () => {
    const itemDifference = discrepancy(21, 'AmountMismatch', 5, 'Amount', '55.00', '66.00');
    const invoiceDifference = discrepancy(22, 'AmountMismatch', null, 'TotalAmount', '500.00', '511.00');
    const element = render(
      reconciliation(
        'Completed',
        [reconciliationItem(5, 'Different', [itemDifference])],
        [itemDifference, invoiceDifference]
      )
    );
    const itemRow = element.querySelector('[data-reconciliation-item-id="5"]');
    const invoiceLevel = element.querySelector('[data-testid="invoice-level-differences"]');

    expect(itemRow?.textContent).toContain('Amount mismatch: 55.00 expected, 66.00 actual');
    expect(itemRow?.textContent).not.toContain('500.00');
    expect(invoiceLevel?.textContent).toContain('Invoice total');
    expect(invoiceLevel?.textContent).toContain('500.00');
    expect(invoiceLevel?.textContent).not.toContain('55.00');
  });

  it('does not expose resolution or frontend matching controls', () => {
    const element = render(reconciliation('Completed', [reconciliationItem(2, 'Different')]));

    expect(element.querySelector('button')).toBeNull();
    expect(element.textContent).not.toContain('Resolve');
    expect(element.textContent).not.toContain('Rematch');
  });
});
