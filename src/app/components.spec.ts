import { RoleDialogComponent } from './features/admin/roles/components/role-dialog/role-dialog.component';
import { DeleteRoleDialogComponent } from './features/admin/roles/components/delete-role-dialog/delete-role-dialog.component';
import { PermissionsMatrixComponent } from './features/admin/roles/components/permissions-matrix/permissions-matrix.component';
import { RoleListComponent } from './features/admin/roles/components/role-list/role-list.component';
import { RolesPermissionsPageComponent } from './features/admin/roles/pages/roles-permissions-page/roles-permissions-page.component';
import { DashboardRedirectComponent } from './features/dashboard/components/dashboard-redirect.component';
import { HomeComponent } from './features/dashboard/pages/home/home.component';
import { PoImportsComponent } from './features/dashboard/pages/po-imports/po-imports.component';
import { ReportsComponent } from './features/dashboard/pages/reports/reports.component';
import { RolesComponent } from './features/dashboard/pages/roles/roles.component';
import { VendorMappingsComponent } from './features/dashboard/pages/vendor-mappings/vendor-mappings.component';
import { LineChartComponent } from './features/dashboard/shared/charts/line-chart/line-chart.component';
import { PageHeaderComponent } from './features/dashboard/shared/components/page-header/page-header.component';
import { StatCardComponent } from './features/dashboard/shared/widgets/stat-card/stat-card.component';
import { NotFoundComponent } from './features/errors/not-found/not-found.component';
import { ColumnMappingRowComponent } from './features/purchase-orders/import/components/column-mapping-row/column-mapping-row.component';
import { ConfirmImportStepComponent } from './features/purchase-orders/import/components/confirm-import-step/confirm-import-step.component';
import { ExcelPreviewPanelComponent } from './features/purchase-orders/import/components/excel-preview-panel/excel-preview-panel.component';
import { MapColumnsStepComponent } from './features/purchase-orders/import/components/map-columns-step/map-columns-step.component';
import { PurchaseOrderStepperComponent } from './features/purchase-orders/import/components/purchase-order-stepper/purchase-order-stepper.component';
import { UploadStepComponent } from './features/purchase-orders/import/components/upload-step/upload-step.component';
import { PoImportPageComponent } from './features/purchase-orders/import/pages/po-import-page/po-import-page.component';
import { UnauthorizedComponent } from './features/unauthorized/unauthorized.component';
import { UserEditComponent } from './features/users/pages/user-edit/user-edit.component';
import { UserListComponent } from './features/users/pages/user-list/user-list.component';
import { AuthenticatedLayoutComponent } from './layouts/authenticated-layout/authenticated-layout.component';
import { BreadcrumbComponent } from './layouts/authenticated-layout/breadcrumb/breadcrumb.component';
import { SidebarComponent } from './layouts/authenticated-layout/sidebar/sidebar.component';
import { TopbarComponent } from './layouts/authenticated-layout/topbar/topbar.component';
import { UserMenuComponent } from './layouts/authenticated-layout/user-menu/user-menu.component';

const components = [
  ['AuthenticatedLayoutComponent', AuthenticatedLayoutComponent],
  ['BreadcrumbComponent', BreadcrumbComponent],
  ['ColumnMappingRowComponent', ColumnMappingRowComponent],
  ['ConfirmImportStepComponent', ConfirmImportStepComponent],
  ['DashboardRedirectComponent', DashboardRedirectComponent],
  ['DeleteRoleDialogComponent', DeleteRoleDialogComponent],
  ['ExcelPreviewPanelComponent', ExcelPreviewPanelComponent],
  ['HomeComponent', HomeComponent],
  ['LineChartComponent', LineChartComponent],
  ['MapColumnsStepComponent', MapColumnsStepComponent],
  ['NotFoundComponent', NotFoundComponent],
  ['PageHeaderComponent', PageHeaderComponent],
  ['PermissionsMatrixComponent', PermissionsMatrixComponent],
  ['PoImportPageComponent', PoImportPageComponent],
  ['PoImportsComponent', PoImportsComponent],
  ['PurchaseOrderStepperComponent', PurchaseOrderStepperComponent],
  ['ReportsComponent', ReportsComponent],
  ['RoleDialogComponent', RoleDialogComponent],
  ['RoleListComponent', RoleListComponent],
  ['RolesComponent', RolesComponent],
  ['RolesPermissionsPageComponent', RolesPermissionsPageComponent],
  ['SidebarComponent', SidebarComponent],
  ['StatCardComponent', StatCardComponent],
  ['TopbarComponent', TopbarComponent],
  ['UnauthorizedComponent', UnauthorizedComponent],
  ['UploadStepComponent', UploadStepComponent],
  ['UserEditComponent', UserEditComponent],
  ['UserListComponent', UserListComponent],
  ['UserMenuComponent', UserMenuComponent],
  ['VendorMappingsComponent', VendorMappingsComponent],
] as const;

describe('All components', () => {
  it.each(components)('%s should compile', (_name, component) => {
    expect(component).toBeDefined();
  });
});
