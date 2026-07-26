export interface PurchaseOrderImportResult {
  purchaseOrderId: string;
  itemsImported: number;
  productsCreated: number;
  productsMatched: number;
  rowsSkipped: number;
}
