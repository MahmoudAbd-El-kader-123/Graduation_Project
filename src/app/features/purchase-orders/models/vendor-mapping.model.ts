export interface VendorMapping {
  vendorId: string;
  systemField: string;
  excelColumn: string;
}

export interface ColumnMapping {
  systemField: string;
  excelColumn: string;
  isCustom: boolean;
}
