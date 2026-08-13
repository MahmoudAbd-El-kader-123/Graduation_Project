import { PERMISSIONS } from '../../core/auth/constants/permissions';
import { accessGuard } from '../../core/auth/guards/access.guard';
import { RECONCILIATION_REPORT_ROUTES } from './reconciliation-reports.routes';

describe('reconciliation report routes', () => {
  it('protects the hub, list, and detail with the dedicated report permission', () => {
    const children = RECONCILIATION_REPORT_ROUTES[0].children ?? [];

    expect(children.map(route => route.path)).toEqual(['', 'reconciliation', 'reconciliation/:invoiceId']);
    for (const route of children) {
      expect(route.canActivate).toContain(accessGuard);
      expect(route.data?.['permissions']).toEqual([PERMISSIONS.reconciliationReports.viewAll]);
    }
  });
});
