# Invoice processing: Angular integration

The invoice workflow is asynchronous:

1. The user selects an existing purchase order and an invoice file.
2. Angular uploads both to the backend.
3. The backend stores the file and returns an invoice ID with status `Queued`.
4. A background job sends the file to the AI service.
5. The backend validates the extracted JSON and compares it with the selected PO.
6. Angular polls the invoice details and displays the final items, discrepancies,
   and processing history.

All requests require a JWT. Authenticate with `POST /api/auth/login`, copy
`data.token`, and send it as:

```http
Authorization: Bearer YOUR_JWT_TOKEN
```

## 1. Choose the purchase order

The purchase order must already exist and contain its imported items. Angular
must send its database ID with the invoice file. The backend uses that PO to:

- set the invoice vendor;
- find the expected supplier SKUs, quantities, prices, and amounts;
- calculate reconciliation discrepancies.

Do not send a PO number in place of the integer PO ID.

## 2. Upload the invoice

```http
POST /api/invoices/upload
Content-Type: multipart/form-data
```

Form fields:

| Field | Type | Meaning |
|---|---|---|
| `File` | file | PDF, JPG, JPEG, or PNG invoice |
| `PurchaseOrderId` | integer | Existing PO database ID |

File rules:

- maximum size: 10 MB;
- accepted extensions: `.pdf`, `.jpg`, `.jpeg`, `.png`;
- accepted media types: `application/pdf`, `image/jpeg`, `image/png`;
- the backend also checks the actual file signature, not only its extension.

Angular example:

```typescript
uploadInvoice(file: File, purchaseOrderId: number) {
  const formData = new FormData();
  formData.append('File', file, file.name);
  formData.append('PurchaseOrderId', purchaseOrderId.toString());

  return this.http.post<ApiResponse<InvoiceUploadResult>>(
    '/api/invoices/upload',
    formData
  );
}
```

Do not set the multipart `Content-Type` header manually. The browser must add
the boundary.

Example success response:

```json
{
  "success": true,
  "message": "Invoice uploaded and queued for processing.",
  "data": {
    "invoiceId": 17,
    "status": "Queued",
    "fileName": "invoice.pdf"
  },
  "errors": null
}
```

The response means the upload was accepted; it does not mean AI processing or
reconciliation has finished. Store `data.invoiceId` and begin polling.

## 3. Poll invoice details

```http
GET /api/invoices/{invoiceId}
```

Poll this endpoint every two or three seconds while the status is non-terminal.
The possible status sequence is:

```text
Uploaded -> Queued -> Processing -> Extracted -> Validated -> Compared -> Completed
```

Terminal statuses:

| Status | Meaning | Angular behavior |
|---|---|---|
| `Completed` | Extraction, validation, and comparison finished | Stop polling and show items and discrepancies |
| `NeedsReview` | Extracted data or its totals failed validation, or the PO could not be loaded | Stop polling and show the latest processing-log message |
| `Failed` | Processing failed unexpectedly | Stop polling and show the latest error log |

`Completed` does not mean that the invoice matches the PO. A completed invoice
can have zero or many discrepancies.

Suggested polling implementation:

```typescript
private readonly terminalStatuses = new Set([
  'Completed',
  'NeedsReview',
  'Failed'
]);

pollInvoice(invoiceId: number) {
  return timer(0, 2500).pipe(
    switchMap(() =>
      this.http.get<ApiResponse<InvoiceDetail>>(
        `/api/invoices/${invoiceId}`
      )
    ),
    takeWhile(
      response => !this.terminalStatuses.has(response.data.status),
      true
    )
  );
}
```

The example uses `timer`, `switchMap`, and `takeWhile` from RxJS.

## 4. Load the reconciliation result

Angular can load the comparison without requesting the full invoice details:

```http
GET /api/invoices/{invoiceId}/reconciliation
```

Example response:

```json
{
  "success": true,
  "message": null,
  "data": {
    "invoiceId": 17,
    "purchaseOrderId": 42,
    "invoiceNumber": "2602506000004",
    "status": "Completed",
    "isReconciled": true,
    "hasDiscrepancies": true,
    "discrepancyCount": 1,
    "discrepancies": [
      {
        "id": 7,
        "discrepancyType": "QuantityMismatch",
        "fieldName": "Quantity",
        "expectedValue": "6",
        "actualValue": "5",
        "isResolved": false
      }
    ]
  },
  "errors": null
}
```

Check `isReconciled` before interpreting the discrepancy list:

- `false` means comparison has not completed, even when the list is empty;
- `true` with `hasDiscrepancies: false` means the invoice matches the PO;
- `true` with `hasDiscrepancies: true` means Angular should display the
  discrepancy list.

The endpoint requires `Invoices.View`. It is limited to the invoice uploader or
an Admin, matching the invoice-detail access rules.

## 5. Render invoice results

Example completed detail response:

```json
{
  "success": true,
  "message": null,
  "data": {
    "id": 17,
    "invoiceNumber": "2602506000004",
    "vendorName": "شركة تكنولوجيا السرعة - تكنولوجيا السرعة",
    "vendorId": 1,
    "purchaseOrderId": 42,
    "status": "Completed",
    "invoiceDate": "2026-06-25T00:00:00",
    "currency": "SAR",
    "subtotal": 4603.62,
    "vat": 690.54,
    "totalAmount": 5294.16,
    "uploadedAt": "2026-07-28T10:00:00Z",
    "items": [
      {
        "id": 101,
        "supplierSku": "10711",
        "description": "Invoice item description",
        "quantity": 5,
        "unitPrice": 11,
        "lineTotal": 63.25
      }
    ],
    "discrepancies": [
      {
        "id": 7,
        "discrepancyType": "QuantityMismatch",
        "fieldName": "Quantity",
        "expectedValue": "6",
        "actualValue": "5",
        "isResolved": false
      }
    ],
    "processingLogs": [
      {
        "id": 33,
        "fromStatus": "Compared",
        "toStatus": "Completed",
        "eventType": "StatusChange",
        "message": "Invoice reconciliation completed with 1 discrepancies.",
        "timestamp": "2026-07-28T10:00:05Z"
      }
    ]
  },
  "errors": null
}
```

Display summary values from the invoice and use:

- `items` for the AI-extracted invoice lines;
- `discrepancies` for the PO-versus-invoice comparison;
- `processingLogs` for a progress timeline and failure/review reason.

The discrepancy values are strings because they can contain either a number or
`N/A`.

| Discrepancy type | Meaning |
|---|---|
| `MissingSku` | Invoice SKU was not found in the PO |
| `MissingFromInvoice` | PO SKU was not found in the invoice |
| `QuantityMismatch` | Invoice and PO quantities differ |
| `UnitPriceMismatch` | Invoice and PO unit prices differ |
| `AmountMismatch` | An item VAT-inclusive amount or the invoice total differs |

For every discrepancy, `expectedValue` is the PO value and `actualValue` is
the AI-extracted invoice value. Item and total amount comparisons allow a
difference of up to `0.01`.

The current detail DTO does not expose `invoiceItemId` on a discrepancy.
Render discrepancies in a separate table or summary. Angular cannot reliably
attach an item discrepancy to one row until the backend exposes that relation.

## 6. Download the original file

```http
GET /api/invoices/{invoiceId}/download
```

The response is the original uploaded file, not an `ApiResponse` JSON object.

```typescript
downloadInvoice(invoiceId: number, fileName: string): void {
  this.http.get(`/api/invoices/${invoiceId}/download`, {
    responseType: 'blob'
  }).subscribe(blob => {
    const url = URL.createObjectURL(blob);
    const anchor = document.createElement('a');
    anchor.href = url;
    anchor.download = fileName;
    anchor.click();
    URL.revokeObjectURL(url);
  });
}
```

## 7. Administrator invoice list

```http
GET /api/invoices?searchTerm=2602506&vendorId=1&status=Completed&hasDiscrepancies=true&pageNumber=1&pageSize=10
```

All filters are optional:

| Parameter | Behavior |
|---|---|
| `searchTerm` | Partial, case-insensitive search over invoice number, vendor name, and PO number |
| `vendorId` | Exact vendor ID |
| `purchaseOrderId` | Exact PO ID |
| `status` | Invoice status name |
| `hasDiscrepancies` | `true` for invoices with discrepancies; `false` for invoices without them |
| `pageNumber` | Normalized to at least `1` |
| `pageSize` | Limited to `1` through `50` |

Use one of the documented invoice status names for filtering. An unrecognized
status is currently ignored rather than returned as an error.

Example response shape:

```json
{
  "success": true,
  "message": null,
  "data": {
    "items": [
      {
        "id": 17,
        "invoiceNumber": "2602506000004",
        "purchaseOrderId": 42,
        "purchaseOrderNumber": "PO2600514032",
        "vendorName": "Vendor name",
        "status": "Completed",
        "totalAmount": 5294.16,
        "discrepancyCount": 1,
        "hasDiscrepancies": true,
        "invoiceDate": "2026-06-25T00:00:00",
        "uploadedAt": "2026-07-28T10:00:00Z",
        "uploadedByUserEmail": "user@example.com",
        "lastError": null
      }
    ],
    "pageNumber": 1,
    "pageSize": 10,
    "totalCount": 1,
    "totalPages": 1,
    "hasPreviousPage": false,
    "hasNextPage": false
  },
  "errors": null
}
```

This endpoint requires the `Invoices.ViewAll` policy. Use the detail endpoint
after selecting a row. Angular should reset `pageNumber` to `1` whenever the
search term or a filter changes.

## Suggested TypeScript contracts

```typescript
export interface ApiResponse<T> {
  success: boolean;
  message: string | null;
  data: T;
  errors: string[] | null;
}

export interface InvoiceUploadResult {
  invoiceId: number;
  status: string;
  fileName: string;
}

export interface InvoiceItem {
  id: number;
  supplierSku: string;
  description: string;
  quantity: number;
  unitPrice: number;
  lineTotal: number;
}

export interface InvoiceDiscrepancy {
  id: number;
  discrepancyType:
    | 'MissingSku'
    | 'MissingFromInvoice'
    | 'QuantityMismatch'
    | 'UnitPriceMismatch'
    | 'AmountMismatch';
  fieldName: string;
  expectedValue: string;
  actualValue: string;
  isResolved: boolean;
}

export interface InvoiceReconciliation {
  invoiceId: number;
  purchaseOrderId: number | null;
  invoiceNumber: string;
  status: string;
  isReconciled: boolean;
  hasDiscrepancies: boolean;
  discrepancyCount: number;
  discrepancies: InvoiceDiscrepancy[];
}

export interface InvoiceProcessingLog {
  id: number;
  fromStatus: string | null;
  toStatus: string;
  eventType: string;
  message: string | null;
  timestamp: string;
}

export interface InvoiceDetail {
  id: number;
  invoiceNumber: string;
  vendorName: string;
  vendorId: number;
  purchaseOrderId: number | null;
  status: string;
  invoiceDate: string;
  currency: string;
  subtotal: number;
  vat: number;
  totalAmount: number;
  uploadedAt: string;
  items: InvoiceItem[];
  discrepancies: InvoiceDiscrepancy[];
  processingLogs: InvoiceProcessingLog[];
}
```

## Permissions and ownership

| Operation | Policy |
|---|---|
| Upload | `Invoices.Upload` |
| View detail | `Invoices.View` |
| Download | `Invoices.Download` |
| List all invoices | `Invoices.ViewAll` |

Having the view or download policy is not sufficient by itself: detail and
download access are limited to the user who uploaded the invoice or an Admin.

## Backend and AI boundary

Angular communicates only with the SPIP backend. It must not call the AI
webhook directly or send an AI API key.

The backend background job sends the uploaded file to the configured AI
endpoint as multipart form data under the field name `file`. The AI service
returns the invoice header and items; the backend validates and stores them,
then performs the PO comparison.

## Failure handling

| Failure | Angular response |
|---|---|
| Invalid/missing PO ID or `Purchase Order not found.` | Keep the file selected and ask the user to choose a valid PO |
| Unsupported, empty, oversized, or disguised file | Show the API validation message and let the user replace the file |
| HTTP `401` | Reauthenticate |
| HTTP `403` | Show insufficient permission or ownership |
| HTTP `404` while loading/downloading | Show invoice or file not found |
| `NeedsReview` | Show the latest processing log and allow the user to contact an operator |
| `Failed` | Show the latest processing error and allow a later retry by uploading again |

There is currently no public API to retry processing, resolve a discrepancy,
delete an invoice, or edit AI-extracted fields. Do not show those actions as
working controls until matching backend endpoints are added.
