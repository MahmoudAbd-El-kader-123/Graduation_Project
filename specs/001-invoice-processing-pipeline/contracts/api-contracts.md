# API Contracts: Invoice Processing Pipeline

Base URL: `api/invoices`

All endpoints require JWT authentication (`Authorization: Bearer <token>`).
All responses use `ApiResponse<T>` wrapper.

---

## POST `api/invoices/upload`

**Description**: Upload an invoice file and enqueue it for processing.

**Authorization**: `Permissions.Invoices.Upload`

**Request**: `multipart/form-data`

| Field | Type | Required | Constraints |
|-------|------|----------|-------------|
| `File` | IFormFile | Yes | PDF, JPG, JPEG, PNG only. Max 10MB. Magic number validation. |
| `PurchaseOrderId` | int | Yes | Must reference existing PO |

**Success Response** (200):
```json
{
  "success": true,
  "message": "Invoice uploaded and queued for processing.",
  "data": {
    "invoiceId": 42,
    "status": "Queued",
    "fileName": "invoice-2026-001.pdf"
  }
}
```

**Error Responses**:
| Status | Condition |
|--------|-----------|
| 400 | Invalid file type, size exceeded, content mismatch, or PO not found |
| 401 | Unauthenticated |
| 403 | Missing `Invoices.Upload` permission |

---

## GET `api/invoices/{id}`

**Description**: Get invoice details including status, extracted data, discrepancies, and processing history.

**Authorization**: `Permissions.Invoices.View` + data ownership check (user must own the invoice, or be Administrator)

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | int | Invoice ID |

**Success Response** (200):
```json
{
  "success": true,
  "data": {
    "id": 42,
    "invoiceNumber": "97349579",
    "vendorName": "Acosta Group",
    "vendorId": 5,
    "purchaseOrderId": 10,
    "status": "Completed",
    "invoiceDate": "2017-10-20",
    "currency": "$",
    "subtotal": 21.31,
    "vat": 2.13,
    "totalAmount": 23.44,
    "uploadedAt": "2026-07-23T10:30:00Z",
    "items": [
      {
        "id": 1,
        "supplierSku": "1",
        "description": "The Blooding by Joseph Wambaugh",
        "quantity": 2,
        "unitPrice": 4.49,
        "lineTotal": 9.88
      }
    ],
    "discrepancies": [
      {
        "id": 1,
        "discrepancyType": "QuantityMismatch",
        "fieldName": "Quantity",
        "expectedValue": "1",
        "actualValue": "2",
        "isResolved": false
      }
    ],
    "processingLogs": [
      {
        "id": 1,
        "fromStatus": null,
        "toStatus": "Uploaded",
        "eventType": "StatusChange",
        "message": "Invoice uploaded successfully.",
        "timestamp": "2026-07-23T10:30:00Z"
      }
    ]
  }
}
```

**Error Responses**:
| Status | Condition |
|--------|-----------|
| 401 | Unauthenticated |
| 403 | User does not own invoice and is not Administrator |
| 404 | Invoice not found |

---

## GET `api/invoices/{id}/download`

**Description**: Download the original uploaded invoice file.

**Authorization**: `Permissions.Invoices.View` + data ownership check

**Path Parameters**:
| Parameter | Type | Description |
|-----------|------|-------------|
| `id` | int | Invoice ID |

**Success Response** (200): File stream with correct `Content-Type` and `Content-Disposition` headers.

**Error Responses**:
| Status | Condition |
|--------|-----------|
| 401 | Unauthenticated |
| 403 | User does not own invoice and is not Administrator |
| 404 | Invoice or file not found |

---

## GET `api/invoices` (Admin)

**Description**: List all invoices with filtering and pagination. Accessible only by Administrators.

**Authorization**: `Permissions.Invoices.ViewAll`

**Query Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `status` | string | No | Filter by InvoiceStatus name (e.g., "Failed", "NeedsReview") |
| `pageNumber` | int | No | Default: 1 |
| `pageSize` | int | No | Default: 10, Max: 50 |

**Success Response** (200):
```json
{
  "success": true,
  "data": {
    "items": [
      {
        "id": 42,
        "invoiceNumber": "97349579",
        "vendorName": "Acosta Group",
        "status": "Failed",
        "totalAmount": 23.44,
        "invoiceDate": "2017-10-20",
        "uploadedAt": "2026-07-23T10:30:00Z",
        "uploadedByUserEmail": "user@example.com",
        "lastError": "AI service timeout after 30 seconds."
      }
    ],
    "totalCount": 150,
    "pageNumber": 1,
    "pageSize": 10
  }
}
```

**Error Responses**:
| Status | Condition |
|--------|-----------|
| 401 | Unauthenticated |
| 403 | Not an Administrator |

---

## DTOs

### Request DTOs

```text
SPIP.Application/DTOs/Invoice/
├── UploadInvoiceRequest.cs        # File + PurchaseOrderId
└── InvoiceListParameters.cs       # Status filter + pagination
```

### Response DTOs

```text
SPIP.Application/DTOs/Invoice/
├── InvoiceDto.cs                  # MODIFY: add all new fields
├── InvoiceDetailDto.cs            # Full detail with items, discrepancies, logs
├── InvoiceItemDto.cs              # Line item with SupplierSku
├── InvoiceUploadResultDto.cs      # Upload confirmation (invoiceId, status, fileName)
├── InvoiceListItemDto.cs          # Admin list item (summary + last error)
├── DiscrepancyDto.cs              # Discrepancy detail
└── InvoiceProcessingLogDto.cs     # Processing log entry
```

### AI DTOs

```text
SPIP.Application/DTOs/AI/
├── AIExtractionResponseDto.cs     # Typed AI response (replaces raw JSON)
└── AIExtractionItemDto.cs         # Single extracted line item
```
