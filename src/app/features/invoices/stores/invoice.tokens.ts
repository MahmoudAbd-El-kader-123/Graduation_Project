import { InjectionToken } from '@angular/core';
import { BaseTableStore } from '../../../shared/table/services/base-table.store';
import { InvoiceListItemDto } from '../models/invoice.model';

export const INVOICE_TABLE_STORE = new InjectionToken<BaseTableStore<InvoiceListItemDto>>(
  'INVOICE_TABLE_STORE'
);
