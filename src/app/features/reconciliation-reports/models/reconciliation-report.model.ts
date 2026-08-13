import { InvoiceReconciliation } from '../../invoices/models/invoice.model';

export interface ReconciliationReportSummary {
  invoiceId: number;
  invoiceNumber: string | null;
  purchaseOrderId: number | null;
  purchaseOrderNumber: string | null;
  vendorName: string | null;
  uploadedByUserEmail: string | null;
  status: string;
  hasDiscrepancies: boolean;
  discrepancyCount: number;
  differentItemCount: number;
  missingFromInvoiceCount: number;
  missingFromPurchaseOrderCount: number;
  uploadedAt: string;
  statusMessage: string | null;
}

export interface ReconciliationReportInvoice {
  invoiceId: number;
  invoiceNumber: string | null;
  purchaseOrderId: number | null;
  purchaseOrderNumber: string | null;
  vendorName: string | null;
  uploadedByUserEmail: string | null;
  uploadedAt: string;
  invoiceDate: string | null;
}

export interface ManagerReconciliationReport {
  invoice: ReconciliationReportInvoice;
  reconciliation: InvoiceReconciliation;
  statusMessage: string | null;
}

export type ReconciliationReportSortField = 'uploadedAt' | 'discrepancyCount';
export type ReconciliationReportSortDirection = 'asc' | 'desc';

export interface ReconciliationReportQuery {
  pageNumber: number;
  pageSize: number;
  searchTerm?: string;
  status?: string;
  hasDiscrepancies?: boolean;
  fromDate?: string;
  toDate?: string;
  sortBy: ReconciliationReportSortField;
  sortDirection: ReconciliationReportSortDirection;
}
