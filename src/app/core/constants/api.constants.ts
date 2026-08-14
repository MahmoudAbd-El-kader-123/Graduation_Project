import { environment } from '../../../environments/environment';

export const API_BASE_URL = environment.apiBaseUrl;

export const API_ENDPOINTS = {
    auth: '/auth',
    users: '/users',
    roles: '/roles',
    permissions: '/permissions',
    vendors: '/vendors',
    products: '/products',
    purchaseOrders: '/purchase-orders',
    invoices: '/invoices',
    reconciliationReports: '/reconciliation-reports',
    auditLogs: '/audit-logs',
    aiChatSessions: '/ai-chat/sessions',
    dashboardStats: '/dashboard/stats'
};
