import { Routes } from '@angular/router';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { PERMISSIONS } from '../../core/auth/constants/permissions';
import { ProductService } from './api/services/product.service';
import { ProductFacade } from './facades/product.facade';
import { ProductStore } from './stores/product.store';
import { PRODUCT_TABLE_STORE, PRODUCT_CRUD_STORE } from './stores/product.tokens';
import { BaseTableStore } from '../../shared/table/services/base-table.store';
import { CrudStateStore } from '../../shared/table/services/crud-state.store';
import { dirtyCheckGuard } from '../../core/guards/dirty-check.guard';

export const PRODUCTS_ROUTES: Routes = [
  {
    path: '',
    providers: [
      ProductService,
      {
        provide: PRODUCT_TABLE_STORE,
        useClass: BaseTableStore
      },
      {
        provide: PRODUCT_CRUD_STORE,
        useClass: CrudStateStore
      },
      ProductStore,
      ProductFacade
    ],
    children: [
      {
        path: '',
        loadComponent: () => import('./pages/product-list/product-list').then(m => m.ProductListComponent),
        canActivate: [accessGuard],
        data: { permissions: [PERMISSIONS.products.view] }
      },
      {
        path: 'create',
        loadComponent: () => import('./pages/product-create/product-create').then(m => m.ProductCreateComponent),
        canActivate: [accessGuard],
        canDeactivate: [dirtyCheckGuard],
        data: { permissions: [PERMISSIONS.products.create] }
      },
      {
        path: ':id/edit',
        loadComponent: () => import('./pages/product-edit/product-edit').then(m => m.ProductEditComponent),
        canActivate: [accessGuard],
        canDeactivate: [dirtyCheckGuard],
        data: { permissions: [PERMISSIONS.products.update] }
      }
    ]
  }
];
