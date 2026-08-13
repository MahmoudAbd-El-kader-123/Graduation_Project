import { Component, computed, input } from '@angular/core';
import { ProgressSpinnerModule } from 'primeng/progressspinner';
import { TableModule } from 'primeng/table';
import { TagModule } from 'primeng/tag';
import {
  DISCREPANCY_LABELS,
  getReconciliationViewState,
  RECONCILIATION_ITEM_STATUS_LABELS
} from '../../constants/invoice-reconciliation.constants';
import {
  DiscrepancyType,
  InvoiceReconciliation,
  ReconciliationItem,
  ReconciliationItemStatus
} from '../../models/invoice.model';

type ReconciliationTagSeverity = 'success' | 'warn';

interface ReconciliationItemView {
  reconciliationItem: ReconciliationItem;
  statusLabel: string;
  statusSeverity: ReconciliationTagSeverity;
  statusDescription: string;
  quantityDifferent: boolean;
  unitPriceDifferent: boolean;
  amountDifferent: boolean;
}

@Component({
  selector: 'app-invoice-reconciliation',
  imports: [ProgressSpinnerModule, TableModule, TagModule],
  templateUrl: './invoice-reconciliation.component.html'
})
export class InvoiceReconciliationComponent {
  readonly reconciliation = input<InvoiceReconciliation | null>(null);
  readonly loading = input(true);
  readonly error = input<string | null>(null);
  readonly polling = input(true);

  readonly viewState = computed(() => getReconciliationViewState(this.reconciliation()));
  readonly discrepantItemComparisons = computed<ReconciliationItemView[]>(() =>
    (this.reconciliation()?.items ?? [])
      .filter(reconciliationItem => reconciliationItem.status !== 'Matched')
      .map(reconciliationItem => ({
        reconciliationItem,
        statusLabel: RECONCILIATION_ITEM_STATUS_LABELS[reconciliationItem.status],
        statusSeverity: 'warn',
        statusDescription: this.getItemStatusDescription(reconciliationItem.status),
        quantityDifferent: reconciliationItem.discrepancies.some(
          discrepancy => discrepancy.discrepancyType === 'QuantityMismatch'
        ),
        unitPriceDifferent: reconciliationItem.discrepancies.some(
          discrepancy => discrepancy.discrepancyType === 'UnitPriceMismatch'
        ),
        amountDifferent: reconciliationItem.discrepancies.some(
          discrepancy => discrepancy.discrepancyType === 'AmountMismatch'
        )
      }))
  );
  readonly invoiceLevelDiscrepancies = computed(() =>
    (this.reconciliation()?.discrepancies ?? []).filter(
      discrepancy => discrepancy.reconciliationItemId === null
    )
  );
  readonly itemCounts = computed(() => {
    const counts: Record<ReconciliationItemStatus, number> = {
      Matched: 0,
      Different: 0,
      MissingFromInvoice: 0,
      MissingFromPurchaseOrder: 0
    };

    for (const reconciliationItem of this.reconciliation()?.items ?? []) {
      counts[reconciliationItem.status]++;
    }

    return counts;
  });

  getDiscrepancyLabel(discrepancyType: DiscrepancyType): string {
    return DISCREPANCY_LABELS[discrepancyType];
  }

  getFieldLabel(fieldName: string): string {
    switch (fieldName) {
      case 'TotalAmount': return 'Invoice total';
      case 'UnitPrice': return 'Unit price';
      case 'Amount': return 'Line amount';
      default: return fieldName;
    }
  }

  displayValue(value: string | null): string {
    return value?.trim() || 'N/A';
  }

  private getItemStatusDescription(status: ReconciliationItemStatus): string {
    switch (status) {
      case 'Matched': return 'Purchase order and invoice values match.';
      case 'Different': return 'One or more item values are different.';
      case 'MissingFromInvoice': return 'Expected purchase-order item was not found in the invoice.';
      case 'MissingFromPurchaseOrder': return 'Invoice item was not found in the purchase order.';
    }
  }
}
