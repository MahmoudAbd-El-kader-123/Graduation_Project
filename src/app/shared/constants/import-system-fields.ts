export interface SystemField {
  id: string;
  label: string;
  required: boolean;
  type?: 'string' | 'number' | 'date';
}

export const IMPORT_SYSTEM_FIELDS: SystemField[] = [
  { id: 'SkuSupplier', label: 'SkuSupplier', required: true, type: 'string' },
  { id: 'Quantity', label: 'Quantity', required: true, type: 'number' },
  { id: 'UnitPrice', label: 'UnitPrice', required: true, type: 'number' },
  { id: 'VatAmount', label: 'VatAmount', required: true, type: 'number' },
  { id: 'Barcode', label: 'Barcode', required: false, type: 'string' },
  { id: 'Description', label: 'Description', required: false, type: 'string' },
  { id: 'Unit', label: 'Unit', required: false, type: 'string' },
  { id: 'VAT Rate', label: 'VAT Rate', required: false, type: 'number' },
  { id: 'Subtotal', label: 'Subtotal', required: false, type: 'number' },
  { id: 'Total', label: 'Total', required: false, type: 'number' }
];
