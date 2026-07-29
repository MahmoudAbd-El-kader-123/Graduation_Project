import { Routes } from '@angular/router';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { PERMISSIONS } from '../../core/auth/constants/permissions';
import { DashboardRouteData } from './models/navigation/dashboard-route-data.model';
import { dashboardHomeGuard } from './guards/dashboard-home.guard';
import { DashboardRedirectComponent } from './components/dashboard-redirect.component';

export const DASHBOARD_ROUTES: Routes = [
  {
    path: '',
    pathMatch: 'full',
    component: DashboardRedirectComponent,
    canActivate: [dashboardHomeGuard]
  },
  {
    path: 'home',
    canActivate: [accessGuard],
    data: {
      title: 'Dashboard',
      breadcrumb: 'Dashboard',
      permissions: [PERMISSIONS.dashboard.view]
    } as DashboardRouteData,
    loadComponent: () => import('./pages/home/home.component').then(m => m.HomeComponent)
  },
  {
    path: 'users',
    data: {
      title: 'Users',
      breadcrumb: 'Users'
    } as DashboardRouteData,
    loadChildren: () => import('../users/users.routes').then(m => m.USERS_ROUTES)
  },
  {
    path: 'roles',
    canActivate: [accessGuard],
    data: {
      title: 'Roles',
      permissions: [PERMISSIONS.roles.view]
    } as DashboardRouteData,
    loadComponent: () => import('../admin/roles/pages/roles-permissions-page/roles-permissions-page.component').then(m => m.RolesPermissionsPageComponent)
  },
  {
    path: 'vendors',
    data: {
      title: 'Vendors',
      breadcrumb: 'Vendors'
    } as DashboardRouteData,
    loadChildren: () => import('../vendors/vendors.routes').then(m => m.VENDORS_ROUTES)
  },
  {
    path: 'products',
    data: {
      title: 'Products',
      breadcrumb: 'Products'
    } as DashboardRouteData,
    loadChildren: () => import('../products/products.routes').then(m => m.PRODUCTS_ROUTES)
  },
  {
    path: 'purchase-orders',
    data: {
      title: 'Purchase Orders',
      breadcrumb: 'Purchase Orders'
    } as DashboardRouteData,
    loadChildren: () => import('../purchase-orders/purchase-orders.routes').then(m => m.PURCHASE_ORDERS_ROUTES)
  },

  {
    path: 'invoices',
    data: {
      title: 'Invoices',
      breadcrumb: 'Invoices'
    } as DashboardRouteData,
    loadChildren: () => import('../invoices/invoices.routes').then(m => m.INVOICES_ROUTES)
  },

  {
    path: 'reports',
    canActivate: [accessGuard],
    data: {
      title: 'Reports',
      permissions: [PERMISSIONS.reports.view]
    } as DashboardRouteData,
    loadComponent: () => import('./pages/reports/reports.component').then(m => m.ReportsComponent)
  }
];
