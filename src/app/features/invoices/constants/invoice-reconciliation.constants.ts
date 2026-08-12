import { DiscrepancyType, InvoiceReconciliation } from '../models/invoice.model';

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
