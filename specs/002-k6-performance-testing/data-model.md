# Data Model: k6 Performance Testing Suite

**Feature**: `002-k6-performance-testing` | **Date**: 2026-08-15

## Overview

This feature does not introduce new persistent data entities. The data model documents the existing SPIP backend entities that the performance tests interact with, the request/response contracts used by k6 scripts, and the test data structures needed for virtual user workflows.

## Existing Entities Under Test

These are the SPIP backend entities that k6 virtual users will read and/or write during test execution. This section documents the fields relevant to test script construction.

### Authentication

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `email` | string | Yes | Login credential |
| `password` | string | Yes | Login credential |

**Response** (`AuthResponseDto`):

| Field | Type | Notes |
|-------|------|-------|
| `userId` | GUID | Identity user ID |
| `token` | string | JWT access token — used for `Authorization: Bearer` header |
| `expiresAt` | datetime | Token expiry timestamp |
| `refreshToken` | string | Used with `POST api/auth/refresh-token` |
| `roles` | string[] | Assigned role names |
| `permissions` | string[] | Assigned permission system names |

### Vendor

**Create/Update** (`CreateVendorDto`):

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `erpId` | string | No | External ERP identifier |
| `name` | string | Yes | Vendor name — must be unique per VU for write isolation |
| `taxRegistrationNumber` | string | No | Tax ID |
| `contactEmail` | string | No | |
| `contactPhone` | string | No | |
| `address` | string | No | |

**Read** (`VendorDto`): Extends above with `id` (int) and `isApproved` (bool).

**List Parameters** (`VendorParameters`): `pageNumber` (int, default 1), `pageSize` (int, default 10, max 100), `searchTerm` (string), `isApproved` (bool).

### Product

**Create/Update** (`CreateProductDto`):

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `erpId` | string | No | |
| `name` | string | Yes | Product name — must be unique per VU |
| `skuSupplier` | string | No | |
| `skuRetailer` | string | No | |
| `barcode` | string | No | |
| `description` | string | No | |
| `unitPrice` | decimal | Yes | |
| `uom` | string | Yes | Default "PCS" |
| `vendorId` | int | Yes | Must reference existing vendor |

**List Parameters** (`ProductParameters`): `pageNumber`, `pageSize`, `searchTerm`, `vendorId`.

### Purchase Order

**Read only** — no create endpoint in scope (import via Excel is excluded).

**Read** (`PurchaseOrderDto`):

| Field | Type | Notes |
|-------|------|-------|
| `id` | int | |
| `orderNumber` | string | |
| `vendorId` | int | |
| `vendorName` | string | |
| `status` | enum | PurchaseOrderStatus |
| `orderDate` | datetime | |
| `totalAmount` | decimal | |
| `items` | PurchaseOrderItemDto[] | Line items |

**List Parameters** (`PurchaseOrderParameters`): `pageNumber`, `pageSize`, `searchTerm`, `orderNumber`, `vendorId`, `status`.

### Vendor Column Mapping

**Create/Update** (`CreateVendorColumnMappingDto`):

| Field | Type | Required | Notes |
|-------|------|----------|-------|
| `vendorId` | int | Yes | Must reference existing vendor |
| `systemField` | string | Yes | Target SPIP field name |
| `excelColumn` | string | Yes | Source Excel column name |

### Audit Log, User, Role, Permission

All read-only via paginated list + get-by-ID endpoints. No create/update operations in test workflows for these entities.

## API Response Wrapper

All SPIP API responses use the `ApiResponse<T>` wrapper:

```json
{
  "success": true|false,
  "message": "string or null",
  "data": { ... },
  "errors": ["string"] | null
}
```

k6 checks must validate `success === true` (not just HTTP status 2xx) to catch application-level errors.

## Pagination Response Wrapper

Paginated endpoints return `ApiResponse<PagedResult<T>>` where `PagedResult<T>` contains:

| Field | Type | Notes |
|-------|------|-------|
| `items` | T[] | Current page of results |
| `totalCount` | int | Total matching records |
| `pageNumber` | int | Current page |
| `pageSize` | int | Items per page |
| `totalPages` | int | Calculated total pages |

## Test Data Configuration

k6 test scripts use a `test-config.json` file containing pre-seeded entity IDs for read operations:

```json
{
  "vendorIds": [1, 2, 3],
  "productIds": [1, 2, 3],
  "purchaseOrderIds": [1, 2, 3],
  "auditLogIds": [1],
  "userIds": [1, 2],
  "roleIds": ["guid-1", "guid-2"]
}
```

These IDs must exist in the target environment. The README documents how to configure them per environment.

## Test Data Isolation (Write Operations)

Virtual users generate unique test data using VU-specific naming:

```
k6-vendor-{VU_ID}-{ITER}-{TIMESTAMP}
k6-product-{VU_ID}-{ITER}-{TIMESTAMP}
```

This ensures no write conflicts between concurrent virtual users. Created entities are cleaned up (deleted) within the same workflow iteration when possible.
