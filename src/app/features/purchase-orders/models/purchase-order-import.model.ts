export interface PurchaseOrderImport {
  vendorId: string;
  file: File;
  hasMixedVatRates: boolean;
  headerRowNumber?: number; // Backend doesn't support yet, but frontend keeps it for future
}
