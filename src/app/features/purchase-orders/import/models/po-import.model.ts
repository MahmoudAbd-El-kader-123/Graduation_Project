export interface ImportPreviewData {
  dataGrid: string[][];
  totalRowsFound: number;
}

export interface ImportPreviewResponse {
  success: boolean;
  message: string;
  data: ImportPreviewData;
  errors: string[] | null;
}

export interface VendorMapping {
  vendorId: string;
  mappings: ColumnMapping[];
}

export interface ColumnMapping {
  systemField: string;
  excelColumn: string;
}

export const SYSTEM_FIELDS = [
  'Product Name',
  'Barcode',
  'SKU Supplier',
  'SKU Retailer',
  'Quantity',
  'Pack Quantity',
  'Price',
  'VAT',
  'Amount',
  'Unit Type'
] as const;

export type SystemField = typeof SYSTEM_FIELDS[number];

export interface PoImportPayload {
  vendorId: string;
  file: File;
  headerRowIndex: number;
  mappings: ColumnMapping[];
  saveVendorMapping: boolean;
}
