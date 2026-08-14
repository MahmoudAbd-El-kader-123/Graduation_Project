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
        icon: 'Users',
        field: 'totalUsers'
    },
    {
        id: 'active-users',
        title: 'Active Users',
        icon: 'UserCheck',
        field: 'activeUsers'
    },
    {
        id: 'inactive-users',
        title: 'Inactive Users',
        icon: 'UserX',
        field: 'inactiveUsers'
    },
    {
        id: 'roles',
        title: 'Roles',
        icon: 'Shield',
        field: 'totalRoles'
    },
    {
        id: 'permissions',
        title: 'Permissions',
        icon: 'Key',
        field: 'totalPermissions'
    },
    {
        id: 'vendors',
        title: 'Vendors',
        icon: 'Building2',
        field: 'totalVendors'
    },
    {
        id: 'products',
        title: 'Products',
        icon: 'Package',
        field: 'totalProducts'
    },
    {
        id: 'purchase-orders',
        title: 'Purchase Orders',
        icon: 'FileText',
        field: 'totalPurchaseOrders'
    },
    {
        id: 'invoices',
        title: 'Invoices',
        icon: 'Receipt',
        field: 'totalInvoices'
    },
    {
        id: 'reconciliation-reports',
        title: 'Reconciliation Reports',
        icon: 'GitMerge',
        field: 'totalReconciliationReports'
    },
    {
        id: 'audit-logs',
        title: 'Audit Logs',
        icon: 'Activity',
        field: 'totalAuditLogs'
    },
    {
        id: 'chat-sessions',
        title: 'AI Chat Sessions',
        icon: 'Bot',
        field: 'totalChatSessions'
    }
];
