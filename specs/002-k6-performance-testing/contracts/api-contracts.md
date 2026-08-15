# API Contracts: k6 Performance Testing Suite

**Feature**: `002-k6-performance-testing` | **Date**: 2026-08-15

## Overview

This document defines the HTTP API contracts that k6 test scripts will exercise. All contracts are derived from the existing SPIP backend controllers. No new endpoints are created.

## Common Contract Elements

### Base URL

All requests use `${BASE_URL}` prefix (from `__ENV.BASE_URL`).

### Authentication Header

All protected endpoints require:
```
Authorization: Bearer <jwt-token>
```

### Response Wrapper

All responses follow `ApiResponse<T>`:
```json
{
  "success": boolean,
  "message": string | null,
  "data": T | null,
  "errors": string[] | null
}
```

### Pagination Query Parameters

All paginated list endpoints accept:
- `pageNumber` (int, default: 1)
- `pageSize` (int, default: 10, max: 100)

---

## Auth Endpoints

### POST /api/auth/login

**Rate Limited**: Yes (`AuthPolicy`)

**Request**:
```json
{
  "email": "string",
  "password": "string"
}
```

**Response** (200): `ApiResponse<AuthResponseDto>`
```json
{
  "success": true,
  "data": {
    "userId": "guid",
    "fullName": "string",
    "userName": "string",
    "email": "string",
    "token": "jwt-string",
    "expiresAt": "datetime",
    "refreshToken": "string",
    "roles": ["string"],
    "permissions": ["string"]
  }
}
```

### POST /api/auth/refresh-token

**Request**:
```json
{
  "token": "current-jwt",
  "refreshToken": "refresh-token-string"
}
```

**Response** (200): `ApiResponse<AuthResponseDto>` (same structure as login)

---

## Vendor Endpoints

### GET /api/vendors

**Auth**: Required (Vendors.View)
**Query**: `pageNumber`, `pageSize`, `searchTerm`, `isApproved`

**Response**: `ApiResponse<PagedResult<VendorDto>>`

### GET /api/vendors/{id}

**Auth**: Required (Vendors.View)
**Response**: `ApiResponse<VendorDto>`

### POST /api/vendors

**Auth**: Required (Vendors.Create)

**Request**:
```json
{
  "erpId": "string?",
  "name": "string (required)",
  "taxRegistrationNumber": "string?",
  "contactEmail": "string?",
  "contactPhone": "string?",
  "address": "string?"
}
```

**Response** (201): `ApiResponse<VendorDto>`

### PUT /api/vendors/{id}

**Auth**: Required (Vendors.Update)
**Request**: Same as POST
**Response** (200): `ApiResponse<VendorDto>`

### DELETE /api/vendors/{id}

**Auth**: Required (Vendors.Delete)
**Response** (200): `ApiResponse<bool>`

### PUT /api/vendors/{id}/toggle-approval

**Auth**: Required (Vendors.Update)
**Request**: Empty body
**Response** (200): `ApiResponse<bool>`

---

## Product Endpoints

### GET /api/products

**Auth**: Required (Products.View)
**Query**: `pageNumber`, `pageSize`, `searchTerm`, `vendorId`

**Response**: `ApiResponse<PagedResult<ProductDto>>`

### GET /api/products/{id}

**Auth**: Required (Products.View)
**Response**: `ApiResponse<ProductDto>`

### POST /api/products

**Auth**: Required (Products.Create)

**Request**:
```json
{
  "erpId": "string?",
  "name": "string (required)",
  "skuSupplier": "string?",
  "skuRetailer": "string?",
  "barcode": "string?",
  "description": "string?",
  "unitPrice": 0.00,
  "uom": "PCS",
  "vendorId": 1
}
```

**Response** (201): `ApiResponse<ProductDto>`

### PUT /api/products/{id}

**Auth**: Required (Products.Update)
**Request**: Same as POST
**Response** (200): `ApiResponse<ProductDto>`

### DELETE /api/products/{id}

**Auth**: Required (Products.Delete)
**Response** (200): `ApiResponse<bool>`

---

## Purchase Order Endpoints

### GET /api/purchase-orders

**Auth**: Required (POImports.View)
**Query**: `pageNumber`, `pageSize`, `searchTerm`, `orderNumber`, `vendorId`, `status`

**Response**: `ApiResponse<PagedResult<PurchaseOrderDto>>`

### GET /api/purchase-orders/{id}

**Auth**: Required (POImports.View)
**Response**: `ApiResponse<PurchaseOrderDto>`

### DELETE /api/purchase-orders/{id}

**Auth**: Required (POImports.Delete)
**Response** (200): `ApiResponse<bool>`

---

## Vendor Mapping Endpoints

### GET /api/vendor-mappings/vendor/{vendorId}

**Auth**: Required (VendorMappings.View)
**Response**: `ApiResponse<IReadOnlyList<VendorColumnMappingDto>>`

### POST /api/vendor-mappings

**Auth**: Required (VendorMappings.Manage)

**Request**:
```json
{
  "vendorId": 1,
  "systemField": "string",
  "excelColumn": "string"
}
```

**Response** (200): `ApiResponse<VendorColumnMappingDto>`

### DELETE /api/vendor-mappings/{id}

**Auth**: Required (VendorMappings.Manage)
**Response** (200): `ApiResponse<bool>`

---

## User Endpoints (Read-Only)

### GET /api/users

**Auth**: Required (Users.View)
**Query**: `pageNumber`, `pageSize`
**Response**: `ApiResponse<PagedResult<UserDto>>`

### GET /api/users/{id}

**Auth**: Required (Users.View)
**Response**: `ApiResponse<UserDto>`

---

## Role Endpoints (Read-Only)

### GET /api/roles

**Auth**: Required (Roles.View)
**Query**: `pageNumber`, `pageSize`
**Response**: `ApiResponse<PagedResult<RoleDto>>`

### GET /api/roles/{id}

**Auth**: Required (Roles.View)
**Response**: `ApiResponse<RoleDto>` (id is GUID)

---

## Permission Endpoints (Read-Only)

### GET /api/permissions

**Auth**: Required (Roles.View)
**Response**: `ApiResponse<List<PermissionGroupDto>>`

```json
{
  "success": true,
  "data": [
    {
      "moduleName": "string",
      "permissions": [
        { "id": 1, "systemName": "string", "displayName": "string" }
      ]
    }
  ]
}
```

---

## Audit Log Endpoints (Read-Only)

### GET /api/audit-logs

**Auth**: Required (Reports.View)
**Query**: `pageNumber`, `pageSize`
**Response**: `ApiResponse<PagedResult<AuditLogDto>>`

### GET /api/audit-logs/{id}

**Auth**: Required (Reports.View)
**Response**: `ApiResponse<AuditLogDto>`

---

## Excluded Endpoints

The following are **NOT** tested by the k6 suite:

| Endpoint | Reason |
|----------|--------|
| `POST /api/auth/register` | Creates user accounts — excluded to avoid test data pollution |
| All `AIChatController` | AI functionality — out of scope |
| `POST /api/invoices/upload` | Triggers AI extraction via background job |
| `GET /api/invoices/*` | All invoice endpoints excluded per user request |
| `GET /api/dashboard/stats` | Excluded per user request |
| `GET /api/reconciliation-reports/*` | Excluded per user request |
| `POST /api/purchase-orders/import` | Excel file import — not a JSON API |
| `POST /api/purchase-orders/import-preview` | Excel file preview — not a JSON API |
| `PUT /api/users/{id}` | User modification — excluded to avoid test data corruption |
| `PUT /api/users/{id}/toggle-status` | User status change — excluded to avoid test data corruption |
| `DELETE /api/users/{id}` | User deletion — excluded to avoid test data corruption |
| All role write operations | Role CRUD — excluded to avoid permission system corruption |
