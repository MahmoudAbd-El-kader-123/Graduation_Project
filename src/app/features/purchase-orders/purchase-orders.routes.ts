import { Routes } from '@angular/router';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { PERMISSIONS } from '../../core/auth/constants/permissions';

export const PURCHASE_ORDERS_ROUTES: Routes = [
  {
    path: '',
    canActivate: [accessGuard],
    data: {
      title: 'Purchase Orders',
      breadcrumb: 'Purchase Orders',
      permissions: [PERMISSIONS.poImports.view] 
    },
    loadComponent: () => import('./pages/purchase-order-list/purchase-order-list.component').then(m => m.PurchaseOrderListComponent)
  },
  {
    path: 'import',
    canActivate: [accessGuard],
    data: {
      title: 'Import Purchase Order',
      breadcrumb: 'Import',
      permissions: [PERMISSIONS.poImports.view]
    },
    loadComponent: () => import('./import/pages/po-import-page/po-import-page.component').then(m => m.PoImportPageComponent)
  },
  {
    path: ':id',
    canActivate: [accessGuard],
    data: {
      title: 'Purchase Order Details',
      breadcrumb: 'Details',
      permissions: [PERMISSIONS.poImports.view]
    },
    loadComponent: () => import('./pages/purchase-order-details/purchase-order-details.component').then(m => m.PurchaseOrderDetailsComponent)
  }
];
