import { Injectable, inject } from '@angular/core';
import { Observable, catchError, forkJoin, map, of } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { ApiResponse } from '../../../../core/models/api-response.model';
import {
  DashboardBusinessData,
  DashboardInvoiceSnapshot,
  DashboardProductSnapshot,
  DashboardPurchaseOrderSnapshot,
  DashboardReportSnapshot,
  DashboardStats,
  DashboardVendorSnapshot
} from '../../models/dashboard-stats.model';
import { API_ENDPOINTS } from '../../../../core/constants/api.constants';
import { PagedResult } from '../../../../shared/api/models/paged-result.model';
import { PERMISSIONS } from '../../../../core/auth/constants/permissions';

interface CollectionSnapshot<T> {
  totalCount: number;
  items: T[];
}

@Injectable({
  providedIn: 'root'
})
export class HomeService {
  private readonly apiService = inject(ApiService);

  getStats(permissions: readonly string[]): Observable<DashboardStats> {
    return forkJoin({
      statsResponse: this.apiService.get<ApiResponse<DashboardStats>>(API_ENDPOINTS.dashboardStats),
      vendors: this.getPermittedCollection<DashboardVendorSnapshot>(permissions, PERMISSIONS.vendors.view, API_ENDPOINTS.vendors),
      products: this.getPermittedCollection<DashboardProductSnapshot>(permissions, PERMISSIONS.products.view, API_ENDPOINTS.products),
      purchaseOrders: this.getPermittedCollection<DashboardPurchaseOrderSnapshot>(permissions, PERMISSIONS.poImports.view, API_ENDPOINTS.purchaseOrders),
      invoices: this.getPermittedCollection<DashboardInvoiceSnapshot>(permissions, PERMISSIONS.invoices.viewAll, API_ENDPOINTS.invoices),
      reports: this.getPermittedCollection<DashboardReportSnapshot>(permissions, PERMISSIONS.reconciliationReports.viewAll, API_ENDPOINTS.reconciliationReports)
    }).pipe(
      map(({ statsResponse, vendors, products, purchaseOrders, invoices, reports }) => {
        if (!statsResponse.success || !statsResponse.data) {
          throw new Error(statsResponse.message || 'Failed to load dashboard statistics.');
        }

        const businessData: DashboardBusinessData = {
          vendors: vendors?.items ?? [],
          products: products?.items ?? [],
          purchaseOrders: purchaseOrders?.items ?? [],
          invoices: invoices?.items ?? [],
          reports: reports?.items ?? []
        };

        return {
          ...statsResponse.data,
          totalAdmins: Object.entries(statsResponse.data.usersPerRole)
            .filter(([role]) => role.toLowerCase() === 'admin')
            .reduce((total, [, count]) => total + count, 0),
          totalVendors: vendors?.totalCount,
          totalProducts: products?.totalCount,
          totalPurchaseOrders: purchaseOrders?.totalCount,
          totalInvoices: invoices?.totalCount,
          totalReconciliationReports: reports?.totalCount,
          businessData
        };
      })
    );
  }

  private getPermittedCollection<T>(
    permissions: readonly string[],
    requiredPermission: string,
    endpoint: string
  ): Observable<CollectionSnapshot<T> | null> {
    const normalizedRequiredPermission = requiredPermission.toLowerCase();
    const hasPermission = permissions.some(
      permission => permission.toLowerCase() === normalizedRequiredPermission
    );

    if (!hasPermission) return of(null);

    return this.apiService.get<ApiResponse<PagedResult<T>>>(endpoint, {
      pageNumber: 1,
      pageSize: 50
    }).pipe(
      map(response => {
        if (!response.success || !response.data) {
          throw new Error(response.message || `Failed to load ${endpoint}.`);
        }
        return { totalCount: response.data.totalCount, items: response.data.items };
      }),
      catchError(() => of(null))
    );
  }
}
