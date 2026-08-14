import { TestBed } from '@angular/core/testing';
import { of } from 'rxjs';
import { ApiService } from '../../../../core/services/api.service';
import { API_ENDPOINTS } from '../../../../core/constants/api.constants';
import { PERMISSIONS } from '../../../../core/auth/constants/permissions';
import { HomeService } from './home.service';

describe('HomeService', () => {
  const stats = {
    totalUsers: 1,
    activeUsers: 1,
    inactiveUsers: 0,
    usersPerRole: { Admin: 1 },
    totalRoles: 1,
    totalPermissions: 2
  };

  let apiService: { get: ReturnType<typeof vi.fn> };
  let service: HomeService;

  beforeEach(() => {
    apiService = { get: vi.fn((endpoint: string) => {
      if (endpoint === API_ENDPOINTS.dashboardStats) {
        return of({ success: true, data: stats, message: '' });
      }

      return of({
        success: true,
        data: {
          items: endpoint === API_ENDPOINTS.invoices
            ? [{ status: 'Completed', totalAmount: 100, vendorName: 'Vendor', hasDiscrepancies: false }]
            : [{ status: 'Completed', hasDiscrepancies: false }],
          pageNumber: 1,
          pageSize: 100,
          totalCount: 1,
          totalPages: 1,
          hasPreviousPage: false,
          hasNextPage: false
        },
        message: ''
      });
    }) };

    TestBed.configureTestingModule({
      providers: [
        HomeService,
        { provide: ApiService, useValue: apiService }
      ]
    });

    service = TestBed.inject(HomeService);
  });

  it('loads collections when stored permissions differ only by letter casing', () => {
    const permissions = [
      PERMISSIONS.invoices.viewAll.toLowerCase(),
      PERMISSIONS.reconciliationReports.viewAll.toLowerCase()
    ];

    service.getStats(permissions).subscribe(result => {
      expect(result.businessData?.invoices).toHaveLength(1);
      expect(result.businessData?.reports).toHaveLength(1);
      expect(result.totalInvoices).toBe(1);
      expect(result.totalReconciliationReports).toBe(1);
    });
  });
});
