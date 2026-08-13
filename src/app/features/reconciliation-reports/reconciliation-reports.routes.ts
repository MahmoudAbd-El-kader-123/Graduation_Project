import { Routes } from '@angular/router';
import { PERMISSIONS } from '../../core/auth/constants/permissions';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { DashboardRouteData } from '../dashboard/models/navigation/dashboard-route-data.model';
import { ReconciliationReportService } from './services/reconciliation-report.service';

const reportAccessData = {
  permissions: [PERMISSIONS.reconciliationReports.viewAll]
};

export const RECONCILIATION_REPORT_ROUTES: Routes = [
  {
    path: '',
    providers: [ReconciliationReportService],
    children: [
      {
        path: '',
        canActivate: [accessGuard],
        data: { ...reportAccessData, title: 'Reports' } as DashboardRouteData,
        loadComponent: () =>
          import('../dashboard/pages/reports/reports.component').then(m => m.ReportsComponent)
      },
      {
        path: 'reconciliation',
        canActivate: [accessGuard],
        data: { ...reportAccessData, title: 'Invoice Reconciliation Reports' } as DashboardRouteData,
        loadComponent: () =>
          import('./pages/reconciliation-report-list/reconciliation-report-list.component')
            .then(m => m.ReconciliationReportListComponent)
      },
      {
        path: 'reconciliation/:invoiceId',
        canActivate: [accessGuard],
        data: { ...reportAccessData, title: 'Reconciliation Report' } as DashboardRouteData,
        loadComponent: () =>
          import('./pages/reconciliation-report-detail/reconciliation-report-detail.component')
            .then(m => m.ReconciliationReportDetailComponent)
      }
    ]
  }
];
