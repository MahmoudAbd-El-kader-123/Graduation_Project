type NestedValues<T> = T extends object ? NestedValues<T[keyof T]> : T;

export const PERMISSIONS = {
    dashboard: {
        view: 'dashboard.view'
    },
    users: {
        view: 'users.view',
        create: 'users.create',
        update: 'users.update',
        activate: 'users.activate',
        deactivate: 'users.deactivate'
    },
    roles: {
        view: 'roles.view',
        create: 'roles.create',
        update: 'roles.update',
        delete: 'roles.delete'
    },
    vendors: {
        view: 'vendors.view',
        create: 'vendors.create',
        update: 'vendors.update',
        delete: 'vendors.delete'
    },
    products: {
        view: 'products.view',
        create: 'products.create',
        update: 'products.update',
        delete: 'products.delete'
    },
    poImports: {
        view: 'poimports.view',
        import: 'poimports.import',
        delete: 'poimports.delete'
    },
    vendorMappings: {
        view: 'vendormappings.view',
        manage: 'vendormappings.manage'
    },
    reports: {
        view: 'reports.view'
    },
    reconciliationReports: {
        viewAll: 'ReconciliationReports.ViewAll'
    },
    invoices: {
        view: 'Invoices.View',
        upload: 'Invoices.Upload',
        download: 'Invoices.Download',
        viewAll: 'Invoices.ViewAll'
    },
    aiChat: {
        use: 'AIChat.Use'
    }
} as const;

export type Permission = NestedValues<typeof PERMISSIONS>;
