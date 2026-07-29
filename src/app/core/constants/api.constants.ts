import { environment } from '../../../environments/environment';

export const API_BASE_URL = environment.apiBaseUrl;

export const API_ENDPOINTS = {
    auth: '/auth',
    users: '/users',
    roles: '/roles',
    vendors: '/vendors',
    products: '/products',
    purchaseOrders: '/purchase-orders',
    invoices: '/invoices'
};
