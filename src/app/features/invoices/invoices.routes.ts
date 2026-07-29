import { Routes } from '@angular/router';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { PERMISSIONS } from '../../core/auth/constants/permissions';
import { DashboardRouteData } from '../dashboard/models/navigation/dashboard-route-data.model';
import { InvoiceService } from './services/invoice.service';
import { InvoiceStore } from './stores/invoice.store';
import { InvoiceFacade } from './facades/invoice.facade';
import { INVOICE_TABLE_STORE } from './stores/invoice.tokens';
import { BaseTableStore } from '../../shared/table/services/base-table.store';

export const INVOICES_ROUTES: Routes = [
  {
    path: '',
    providers: [
      InvoiceService,
      { provide: INVOICE_TABLE_STORE, useClass: BaseTableStore },
      InvoiceStore,
      InvoiceFacade
    ],
    children: [
      {
        path: '',
        loadComponent: () =>
          import('./pages/invoice-list/invoice-list').then(m => m.InvoiceListComponent),
        canActivate: [accessGuard],
        data: {
          permissions: [PERMISSIONS.invoices.view],
          title: 'Invoices'
        } as DashboardRouteData
      },
      {
        path: ':id',
        loadComponent: () =>
          import('./pages/invoice-detail/invoice-detail').then(m => m.InvoiceDetailComponent),
        canActivate: [accessGuard],
        data: {
          permissions: [PERMISSIONS.invoices.view],
          title: 'Invoice Detail'
        } as DashboardRouteData
      }
    ]
  }
];
