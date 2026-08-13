// Matches backend InvoiceListItemDto exactly
export interface InvoiceListItemDto {
  id: number;
  invoiceNumber: string | null;
  purchaseOrderId: number;
  purchaseOrderNumber: string | null;
  vendorName: string | null;
  status: string | null;
  totalAmount: number;
  discrepancyCount: number;
  hasDiscrepancies: boolean;
  invoiceDate: string;
  uploadedAt: string;
  uploadedByUserEmail: string | null;
  lastError: string | null;
}

// Matches backend InvoiceItemDto
export interface InvoiceItemDto {
  id: number;
  supplierSku: string | null;
  description: string | null;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

// Matches backend DiscrepancyDto
export interface DiscrepancyDto {
  id: number;
  discrepancyType: string | null;
  fieldName: string | null;
  expectedValue: string | null;
  actualValue: string | null;
  isResolved: boolean;
}

// Matches backend InvoiceProcessingLogDto
export interface InvoiceProcessingLogDto {
  id: number;
  fromStatus: string | null;
  toStatus: string | null;
  eventType: string | null;
  message: string | null;
  timestamp: string;
}

// Matches backend InvoiceDetailDto
export interface InvoiceDetailDto {
  id: number;
  invoiceNumber: string | null;
  vendorName: string | null;
  vendorId: number;
  purchaseOrderId: number | null;
  status: string | null;
  invoiceDate: string;
  currency: string | null;
  subtotal: number;
  vat: number;
  totalAmount: number;
  uploadedAt: string;
  items: InvoiceItemDto[] | null;
  discrepancies: DiscrepancyDto[] | null;
  processingLogs: InvoiceProcessingLogDto[] | null;
}

// Matches backend InvoiceUploadResultDto
export interface InvoiceUploadResultDto {
  invoiceId: number;
  status: string | null;
  fileName: string | null;
}

export type DiscrepancyType =
  | 'MissingSku'
  | 'MissingFromInvoice'
  | 'QuantityMismatch'
  | 'UnitPriceMismatch'
  | 'AmountMismatch';

export interface InvoiceDiscrepancy {
  id: number;
  discrepancyType: DiscrepancyType;
  fieldName: string;
  reconciliationItemId: number | null;
  purchaseOrderItemId: number | null;
  invoiceItemId: number | null;
  purchaseOrderSku: string | null;
  invoiceSku: string | null;
  productName: string | null;
  expectedValue: string;
  actualValue: string;
  isResolved: boolean;
}

export type ReconciliationItemStatus =
  | 'Matched'
  | 'Different'
  | 'MissingFromInvoice'
  | 'MissingFromPurchaseOrder';

export interface ReconciliationItem {
  id: number;
  purchaseOrderItemId: number | null;
  invoiceItemId: number | null;
  purchaseOrderSku: string | null;
  invoiceSku: string | null;
  productName: string | null;
  expectedQuantity: string;
  actualQuantity: string;
  expectedUnitPrice: string;
  actualUnitPrice: string;
  expectedAmount: string;
  actualAmount: string;
  status: ReconciliationItemStatus;
  discrepancies: InvoiceDiscrepancy[];
}

export interface InvoiceReconciliation {
  invoiceId: number;
  purchaseOrderId: number | null;
  invoiceNumber: string;
  status: string;
  isReconciled: boolean;
  hasDiscrepancies: boolean;
  discrepancyCount: number;
  items: ReconciliationItem[];
  discrepancies: InvoiceDiscrepancy[];
}
