import { AppIcon } from '../../../core/icons/lucide-icons';
import { DashboardStats } from '../models/dashboard-stats.model';

export interface DashboardWidget {
    id: string;
    title: string;
    icon: AppIcon;
    field: keyof DashboardStats;
}

export const DASHBOARD_WIDGETS: DashboardWidget[] = [
    {
        id: 'total-users',
        title: 'Total Users',
        icon: 'users',
        field: 'totalUsers'
    },
    {
        id: 'active-users',
        title: 'Active Users',
        icon: 'user-check',
        field: 'activeUsers'
    },
    {
        id: 'inactive-users',
        title: 'Inactive Users',
        icon: 'user-x',
        field: 'inactiveUsers'
    },
    {
        id: 'roles',
        title: 'Roles',
        icon: 'shield',
        field: 'totalRoles'
    },
    {
        id: 'permissions',
        title: 'Permissions',
        icon: 'key',
        field: 'totalPermissions'
    },
    {
        id: 'vendors',
        title: 'Vendors',
        icon: 'building-2',
        field: 'totalVendors'
    },
    {
        id: 'products',
        title: 'Products',
        icon: 'package',
        field: 'totalProducts'
    },
    {
        id: 'purchase-orders',
        title: 'Purchase Orders',
        icon: 'file-text',
        field: 'totalPurchaseOrders'
    },
    {
        id: 'invoices',
        title: 'Invoices',
        icon: 'receipt',
        field: 'totalInvoices'
    },
    {
        id: 'reconciliation-reports',
        title: 'Reconciliation Reports',
        icon: 'git-merge',
        field: 'totalReconciliationReports'
    },
    {
        id: 'audit-logs',
        title: 'Audit Logs',
        icon: 'activity',
        field: 'totalAuditLogs'
    },
    {
        id: 'chat-sessions',
        title: 'AI Chat Sessions',
        icon: 'bot',
        field: 'totalChatSessions'
    }
];
