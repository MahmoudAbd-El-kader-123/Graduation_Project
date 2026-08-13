import { PERMISSIONS } from '../../../core/auth/constants/permissions';
import { DashboardPage } from '../models/navigation/dashboard-page.model';

export const DASHBOARD_PAGES: DashboardPage[] = [
    {
        id: 'home',
        title: 'Dashboard',
        icon: 'LayoutDashboard',
        route: '/dashboard/home',
        group: 'General',
        showInSidebar: true,
        sidebarOrder: 0,
        homePriority: 0,
        isDashboardHome: true,
        permissions: [PERMISSIONS.dashboard.view]
    },
    {
        id: 'users',
        title: 'Users',
        icon: 'Users',
        route: '/dashboard/users',
        group: 'Management',
        showInSidebar: true,
        sidebarOrder: 1,
        homePriority: 1,
        permissions: [PERMISSIONS.users.view]
    },
    {
        id: 'roles',
        title: 'Roles',
        icon: 'Shield',
        route: '/dashboard/roles',
        group: 'Management',
        showInSidebar: true,
        sidebarOrder: 2,
        homePriority: 2,
        permissions: [PERMISSIONS.roles.view]
    },
    {
        id: 'vendors',
        title: 'Vendors',
        icon: 'Building2',
        route: '/dashboard/vendors',
        group: 'Inventory',
        showInSidebar: true,
        sidebarOrder: 3,
        homePriority: 3,
        permissions: [PERMISSIONS.vendors.view]
    },
    {
        id: 'products',
        title: 'Products',
        icon: 'Package',
        route: '/dashboard/products',
        group: 'Inventory',
        showInSidebar: true,
        sidebarOrder: 4,
        homePriority: 4,
        permissions: [PERMISSIONS.products.view]
    },
    {
        id: 'purchase-orders',
        title: 'Purchase Orders',
        icon: 'FileDown',
        route: '/dashboard/purchase-orders',
        group: 'Operations',
        showInSidebar: true,
        sidebarOrder: 5,
        homePriority: 5,
        permissions: [PERMISSIONS.poImports.view]
    },

    {
        id: 'invoices',
        title: 'Invoices',
        icon: 'Receipt',
        route: '/dashboard/invoices',
        group: 'Operations',
        showInSidebar: true,
        sidebarOrder: 6,
        homePriority: 6,
        permissions: [PERMISSIONS.invoices.view]
    },
    {
        id: 'reports',
        title: 'Reports',
        icon: 'ChartColumn',
        route: '/dashboard/reports',
        group: 'Analytics',
        showInSidebar: true,
        sidebarOrder: 7,
        homePriority: 7,
        permissions: [PERMISSIONS.reconciliationReports.viewAll]
    },
    {
        id: 'assistant',
        title: 'Assistant',
        icon: 'Bot',
        route: '/dashboard/assistant',
        group: 'Analytics',
        showInSidebar: true,
        sidebarOrder: 8,
        homePriority: 8,
        permissions: [PERMISSIONS.aiChat.use]
    }
];
