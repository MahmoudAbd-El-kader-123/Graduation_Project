export interface InvoiceTableColumn {
  field: string;
  header: string;
}

export const INVOICE_TABLE_COLUMNS: InvoiceTableColumn[] = [
  { field: 'invoiceNumber',       header: 'Invoice Number' },
  { field: 'vendorName',          header: 'Vendor'         },
  { field: 'status',              header: 'Status'         },
  { field: 'invoiceDate',         header: 'Invoice Date'   },
  { field: 'totalAmount',         header: 'Total Amount'   },
  { field: 'uploadedAt',          header: 'Uploaded At'    },
  { field: 'uploadedByUserEmail', header: 'Uploaded By'    }
];
