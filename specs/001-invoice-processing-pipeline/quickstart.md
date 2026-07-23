# Quickstart Validation Guide: Invoice Processing Pipeline

## Prerequisites

1. .NET 8 SDK (8.0.404+) installed
2. SQL Server instance running with the SPIP database
3. Hangfire tables provisioned (auto-created on first run)
4. Application running via `dotnet run --project SPIP.API`
5. A valid JWT token (obtain via `POST api/auth/login`)
6. At least one Purchase Order with items in the database (from Sprint 2 Excel import)
7. A sample invoice file (PDF, JPG, JPEG, or PNG)

## Scenario 1: Upload Invoice (Happy Path)

**Validates**: FR-001, FR-002, FR-003, FR-005, FR-006, FR-007, FR-008, SC-001

```bash
# Upload a valid PDF invoice linked to PO ID 1
curl -X POST "https://localhost:7xxx/api/invoices/upload" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -F "File=@sample-invoice.pdf" \
  -F "PurchaseOrderId=1"
```

**Expected Outcome**:
- HTTP 200 with `{ success: true, data: { invoiceId: N, status: "Queued" } }`
- File stored under configured storage directory with a GUID filename
- Invoice record in database with status `Queued`
- UploadedFile record linked to the invoice
- Hangfire job enqueued (visible in Hangfire Dashboard at `/hangfire`)
- Response time < 3 seconds (SC-001)

## Scenario 2: File Validation Rejection

**Validates**: FR-003, FR-004, SC-007

```bash
# Upload an .exe file renamed to .pdf
curl -X POST "https://localhost:7xxx/api/invoices/upload" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -F "File=@malicious.pdf" \
  -F "PurchaseOrderId=1"
```

**Expected Outcome**:
- HTTP 400 with structured error: file content does not match declared type
- No file stored, no invoice record created

## Scenario 3: Full Processing Pipeline

**Validates**: FR-009, FR-010, FR-011, FR-012, FR-013, FR-014, FR-016, FR-017, SC-002, SC-003, SC-005

1. Upload a valid invoice (Scenario 1)
2. Wait for Hangfire to process (check status endpoint or Hangfire Dashboard)
3. Query the invoice detail:

```bash
curl -X GET "https://localhost:7xxx/api/invoices/{invoiceId}" \
  -H "Authorization: Bearer <JWT_TOKEN>"
```

**Expected Outcome**:
- Invoice status is `Completed` (or `NeedsReview` if business validation failed)
- `items` array populated with extracted line items (supplierSku, quantity, unitPrice, lineTotal)
- `discrepancies` array populated if mismatches found against PO
- `processingLogs` shows complete lifecycle: Uploaded → Queued → Processing → Extracted → Validated → Compared → Completed
- Processing time < 60 seconds (SC-002)
- All quantity/price/SKU mismatches detected (SC-003)
- No gaps in processing log (SC-005)

## Scenario 4: Secure File Download

**Validates**: FR-019, FR-023, SC-006

```bash
# Authenticated download
curl -X GET "https://localhost:7xxx/api/invoices/{invoiceId}/download" \
  -H "Authorization: Bearer <JWT_TOKEN>" \
  -o downloaded-invoice.pdf

# Unauthenticated attempt (should fail)
curl -X GET "https://localhost:7xxx/api/invoices/{invoiceId}/download"
```

**Expected Outcome**:
- Authenticated: File returned with correct Content-Type
- Unauthenticated: HTTP 401

## Scenario 5: Data Ownership Enforcement

**Validates**: FR-024

```bash
# User A uploads an invoice (get invoiceId)
# User B tries to view it
curl -X GET "https://localhost:7xxx/api/invoices/{invoiceId}" \
  -H "Authorization: Bearer <USER_B_JWT_TOKEN>"
```

**Expected Outcome**:
- HTTP 403 Forbidden (User B does not own the invoice)

## Scenario 6: Admin Monitoring

**Validates**: FR-021, SC-008

```bash
# Admin filters for failed invoices
curl -X GET "https://localhost:7xxx/api/invoices?status=Failed&pageNumber=1&pageSize=10" \
  -H "Authorization: Bearer <ADMIN_JWT_TOKEN>"
```

**Expected Outcome**:
- HTTP 200 with paginated list of only Failed invoices
- Each item includes `lastError` field
- Response time < 10 seconds (SC-008)

## Key Verification Checklist

| # | Check | FR/SC | How to Verify |
|---|-------|-------|---------------|
| 1 | Upload returns < 3s | SC-001 | Time the curl request |
| 2 | Pipeline completes < 60s | SC-002 | Check timestamps in processingLogs |
| 3 | All discrepancies detected | SC-003 | Compare invoice items vs PO items manually |
| 4 | 3 retries on failure | SC-004 | Kill AI service, check Hangfire retry count |
| 5 | Complete processing log | SC-005 | Verify no status gaps in processingLogs |
| 6 | No unauthenticated file access | SC-006 | Attempt download without token |
| 7 | Magic number validation works | SC-007 | Upload renamed executable |
| 8 | Admin filter response < 10s | SC-008 | Time the admin list request |
