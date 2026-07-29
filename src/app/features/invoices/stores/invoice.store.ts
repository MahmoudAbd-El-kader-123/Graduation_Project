import { Injectable, inject } from '@angular/core';
import { INVOICE_TABLE_STORE } from './invoice.tokens';

@Injectable()
export class InvoiceStore {
  readonly table = inject(INVOICE_TABLE_STORE);
}
