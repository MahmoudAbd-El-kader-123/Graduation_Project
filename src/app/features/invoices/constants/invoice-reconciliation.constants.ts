import {
  DiscrepancyType,
  InvoiceReconciliation,
  ReconciliationItemStatus
} from '../models/invoice.model';

export type ReconciliationViewState =
  | 'processing'
  | 'match'
  | 'differences'
  | 'needs-review'
  | 'failed';

export const DISCREPANCY_LABELS: Record<DiscrepancyType, string> = {
  MissingSku: 'Invoice SKU not found in the purchase order',
  MissingFromInvoice: 'Purchase-order SKU missing from the invoice',
  QuantityMismatch: 'Quantity mismatch',
  UnitPriceMismatch: 'Unit-price mismatch',
  AmountMismatch: 'Amount mismatch'
};

export const RECONCILIATION_ITEM_STATUS_LABELS: Record<ReconciliationItemStatus, string> = {
  Matched: 'Matched',
  Different: 'Different',
  MissingFromInvoice: 'Missing from invoice',
  MissingFromPurchaseOrder: 'Missing from purchase order'
};

export function getReconciliationViewState(
  reconciliation: InvoiceReconciliation | null
): ReconciliationViewState {
  if (!reconciliation) return 'processing';

  switch (reconciliation.status.toLowerCase()) {
    case 'completed':
      return reconciliation.hasDiscrepancies ? 'differences' : 'match';
    case 'needsreview':
      return 'needs-review';
    case 'failed':
      return 'failed';
    default:
      return 'processing';
  }
}
