import { authenticate } from '../helpers/auth.js';
import { get } from '../helpers/http-wrapper.js';
import { checkEntityResponse, checkPagedResponse } from '../helpers/checks.js';
import { thinkTime } from '../helpers/data-utils.js';

for (const envVar of ['BASE_URL', 'TEST_USER_EMAIL', 'TEST_USER_PASSWORD']) {
  if (!__ENV[envVar]) {
    throw new Error(`Missing required environment variable: ${envVar}. Set it with -e ${envVar}=<value>.`);
  }
}

let cachedToken = null;

function getToken() {
  if (!cachedToken) {
    cachedToken = authenticate();
  }
  return cachedToken;
}

function request(path, endpointName, label, checker) {
  let token = getToken();
  if (!token) {
    return null;
  }

  let response = get(path, token, endpointName);
  if (response.status === 401) {
    cachedToken = null;
    token = getToken();
    if (!token) {
      return null;
    }
    response = get(path, token, endpointName);
  }

  checker(response, label);
  return response;
}

function extractIdsFromListResponse(response) {
  if (!response || response.status < 200 || response.status >= 300) {
    return [];
  }

  try {
    const body = response.json();
    const items = body?.data?.items;
    if (!Array.isArray(items)) {
      return [];
    }

    return items
      .map((item) => item?.id)
      .filter((id) => id !== undefined && id !== null && id !== '');
  } catch (_) {
    return [];
  }
}

function maybeRequestDetail(listPath, detailPath, listEndpointName, detailEndpointName, listLabel, detailLabel, checker) {
  const listResponse = request(listPath, listEndpointName, listLabel, checkPagedResponse);
  if (!listResponse) {
    return;
  }

  const ids = extractIdsFromListResponse(listResponse);
  if (ids.length === 0) {
    return;
  }

  const id = ids[0];
  request(detailPath.replace('{id}', encodeURIComponent(id)), detailEndpointName, detailLabel, checker);
}

export function userJourney() {
  if (!getToken()) {
    return;
  }

  const vendorsResponse = request('/api/vendors?pageNumber=1&pageSize=20', 'GET /api/vendors', 'GET vendors list', checkPagedResponse);
  const vendorIds = vendorsResponse ? extractIdsFromListResponse(vendorsResponse) : [];
  if (vendorIds.length > 0) {
    request(`/api/vendors/${vendorIds[0]}`, 'GET /api/vendors/{id}', 'GET vendor', checkEntityResponse);
  }
  thinkTime();

  const productsResponse = request('/api/products?pageNumber=1&pageSize=20', 'GET /api/products', 'GET products list', checkPagedResponse);
  const productIds = productsResponse ? extractIdsFromListResponse(productsResponse) : [];
  if (productIds.length > 0) {
    request(`/api/products/${productIds[0]}`, 'GET /api/products/{id}', 'GET product', checkEntityResponse);
  }
  thinkTime();

  const purchaseOrdersResponse = request('/api/purchase-orders?pageNumber=1&pageSize=20', 'GET /api/purchase-orders', 'GET purchase orders list', checkPagedResponse);
  const purchaseOrderIds = purchaseOrdersResponse ? extractIdsFromListResponse(purchaseOrdersResponse) : [];
  if (purchaseOrderIds.length > 0) {
    request(`/api/purchase-orders/${purchaseOrderIds[0]}`, 'GET /api/purchase-orders/{id}', 'GET purchase order', checkEntityResponse);
  }
  thinkTime();

  const usersResponse = request('/api/users?pageNumber=1&pageSize=20', 'GET /api/users', 'GET users list', checkPagedResponse);
  const userIds = usersResponse ? extractIdsFromListResponse(usersResponse) : [];
  if (userIds.length > 0) {
    request(`/api/users/${userIds[0]}`, 'GET /api/users/{id}', 'GET user', checkEntityResponse);
  }
  thinkTime();

  const rolesResponse = request('/api/roles?pageNumber=1&pageSize=20', 'GET /api/roles', 'GET roles list', checkPagedResponse);
  const roleIds = rolesResponse ? extractIdsFromListResponse(rolesResponse) : [];
  if (roleIds.length > 0) {
    request(`/api/roles/${roleIds[0]}`, 'GET /api/roles/{id}', 'GET role', checkEntityResponse);
  }
  thinkTime();

  request('/api/permissions', 'GET /api/permissions', 'GET permissions', checkEntityResponse);
  thinkTime();

  const auditLogsResponse = request('/api/audit-logs?pageNumber=1&pageSize=20', 'GET /api/audit-logs', 'GET audit logs list', checkPagedResponse);
  const auditLogIds = auditLogsResponse ? extractIdsFromListResponse(auditLogsResponse) : [];
  if (auditLogIds.length > 0) {
    request(`/api/audit-logs/${auditLogIds[0]}`, 'GET /api/audit-logs/{id}', 'GET audit log', checkEntityResponse);
  }
  thinkTime();

  if (vendorIds.length > 0) {
    request(`/api/vendor-mappings/vendor/${vendorIds[0]}`, 'GET /api/vendor-mappings/vendor/{vendorId}', 'GET vendor mappings', checkEntityResponse);
  }
}
