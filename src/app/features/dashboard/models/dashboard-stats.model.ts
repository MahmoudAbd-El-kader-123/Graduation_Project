export interface DashboardStats {
    totalUsers: number;
    totalAdmins?: number;
    activeUsers: number;
    inactiveUsers: number;
    usersPerRole: Record<string, number>;
    totalRoles: number;
    totalPermissions: number;
    totalVendors?: number;
    totalProducts?: number;
    totalPurchaseOrders?: number;
    totalInvoices?: number;
    totalReconciliationReports?: number;
    totalAuditLogs?: number;
    totalChatSessions?: number;
    businessData?: DashboardBusinessData;
}

export interface DashboardVendorSnapshot {
    name: string;
    isApproved: boolean;
}

export interface DashboardProductSnapshot {
    vendorName?: string | null;
}

export interface DashboardPurchaseOrderSnapshot {
    status: number | string;
    totalAmount: number;
    vendorName?: string | null;
}

export interface DashboardInvoiceSnapshot {
    status?: string | null;
    totalAmount: number;
    vendorName?: string | null;
    hasDiscrepancies: boolean;
}

export interface DashboardReportSnapshot {
    status?: string | null;
    hasDiscrepancies: boolean;
}

export interface DashboardBusinessData {
    vendors: DashboardVendorSnapshot[];
    products: DashboardProductSnapshot[];
    purchaseOrders: DashboardPurchaseOrderSnapshot[];
    invoices: DashboardInvoiceSnapshot[];
    reports: DashboardReportSnapshot[];
}
