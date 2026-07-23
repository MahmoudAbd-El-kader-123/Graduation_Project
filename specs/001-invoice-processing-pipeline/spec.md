# Feature Specification: Intelligent Invoice Processing Pipeline

**Feature Branch**: `001-invoice-processing-pipeline`

**Created**: 2026-07-22

**Status**: Draft

## Clarifications

### Session 2026-07-23

- Q: How does the system determine which Purchase Order to reconcile against? → A: The user provides the Purchase Order ID at upload time. The AI response does not contain a PO reference.
- Q: Should the system flag PO line items not present in the invoice as discrepancies? → A: Yes. Generate a "Missing from Invoice" discrepancy for each PO item not found in the invoice.
- Q: Should unit price comparison use exact match or allow a tolerance threshold? → A: Exact match. Any price difference generates a discrepancy. Tolerance is out of scope for MVP.
- Q: What is the AI service timeout threshold? → A: 30 seconds. After timeout, the attempt is marked as failed and the retry policy applies.
- Q: Should Procurement Users see only their own invoices or all invoices? → A: Procurement Users see only invoices they uploaded. Administrators see all invoices.

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Upload Invoice for Processing (Priority: P1)

A Procurement User navigates to the invoice upload section, selects an invoice file (PDF, JPG, JPEG, or PNG) from their local machine, provides the Purchase Order ID to reconcile against, and submits it. The system validates the file for type, size, and content safety, stores it securely, creates the invoice record linked to the specified Purchase Order, and queues it for asynchronous processing. The user receives immediate confirmation that the upload succeeded and processing has been queued.

**Why this priority**: This is the entry point for the entire pipeline. Without upload capability, no downstream processing can occur. It delivers immediate value by replacing manual invoice intake.

**Independent Test**: Upload a valid PDF invoice and verify the system returns a success response with the invoice identifier and status "Queued". Verify the file exists in secure storage and the invoice record exists in the database.

**Acceptance Scenarios**:

1. **Given** an authenticated Procurement User with a valid 2MB PDF invoice and a valid Purchase Order ID, **When** the user uploads the file, **Then** the system returns HTTP 200 with the invoice identifier, the file is stored securely outside the web root with a GUID filename, an Invoice record is created linked to the specified PO with status "Uploaded" (transitioning to "Queued"), an UploadedFile record is created with file metadata, and a background processing job is enqueued.

2. **Given** an authenticated Procurement User with a valid 5MB JPG invoice image, **When** the user uploads the file, **Then** the system accepts the file, stores it securely, creates all required records, and queues processing.

3. **Given** an authenticated Procurement User with a .exe file renamed to .pdf, **When** the user uploads the file, **Then** the system rejects the upload with a validation error indicating the file content does not match the declared type, no file is stored, and no invoice records are created.

4. **Given** an unauthenticated user, **When** the user attempts to upload an invoice, **Then** the system returns HTTP 401 Unauthorized.

5. **Given** an authenticated user without invoice upload permissions, **When** the user attempts to upload an invoice, **Then** the system returns HTTP 403 Forbidden.

6. **Given** an authenticated Procurement User with a file exceeding the maximum allowed size, **When** the user uploads the file, **Then** the system rejects the upload with a validation error indicating the file is too large.

---

### User Story 2 - Automated Invoice Data Extraction (Priority: P1)

After an invoice is uploaded and queued, the background processing system picks up the job, sends the invoice to an external AI service for document understanding, receives the structured extraction response, validates the response format and content, maps the AI field names to the internal domain model, and persists the extracted invoice data (header and line items).

**Why this priority**: AI-powered extraction is the core differentiator of the platform. Without it, the system is merely a file upload tool. This story transforms a static document into structured, actionable procurement data.

**Independent Test**: Upload a valid invoice, wait for background processing to complete, and verify that the invoice record has extracted header data (invoice number, date, supplier, total) and line items (SKU, quantity, unit price, description) persisted in the database. Verify the invoice status progresses through "Processing" → "Extracted" → "Validated".

**Acceptance Scenarios**:

1. **Given** an invoice in "Queued" status, **When** the background processor picks up the job, **Then** the invoice status transitions to "Processing", the system sends the invoice to the AI service, receives a structured JSON response, validates the JSON schema, validates business rules on the extracted data, maps AI field names to internal domain fields, persists the invoice header and line items, and the status transitions through "Extracted" → "Validated".

2. **Given** an invoice in "Processing" status, **When** the AI service returns an invalid JSON response (missing required fields), **Then** the system marks the invoice as "Failed", logs the validation errors in the processing history, and does not persist any extracted data.

3. **Given** an invoice in "Processing" status, **When** the AI service is unreachable or times out, **Then** the system marks the invoice as "Failed", logs the error, and the background job system retries according to the configured retry policy.

4. **Given** an invoice in "Processing" status, **When** the AI service returns data with negative quantities or zero unit prices, **Then** the system marks the invoice as "NeedsReview", logs the specific business validation failures, and does not proceed to reconciliation.

---

### User Story 3 - Purchase Order Reconciliation (Priority: P1)

After invoice data is validated and persisted, the system automatically compares each extracted invoice line item against the corresponding Purchase Order using Supplier SKU as the matching key. The system detects discrepancies in quantity and unit price, generates discrepancy records for each mismatch, and marks the invoice as reconciled.

**Why this priority**: Reconciliation is the primary business value — it automates the tedious manual process of comparing invoices to POs and flags procurement fraud or errors. Without reconciliation, extracted data has limited actionable value.

**Independent Test**: Upload an invoice whose line items have known differences from the linked Purchase Order (e.g., quantity mismatch on one item, price mismatch on another). After processing completes, verify that the correct discrepancy records are generated and the invoice reaches "Compared" → "Completed" status.

**Acceptance Scenarios**:

1. **Given** a validated invoice with 3 line items matching a Purchase Order exactly (same SKU, quantity, and unit price), **When** reconciliation runs, **Then** no discrepancy records are generated, and the invoice status transitions to "Compared" → "Completed".

2. **Given** a validated invoice with a line item where the quantity is 100 but the PO specifies 80 for the same SKU, **When** reconciliation runs, **Then** a "Quantity Mismatch" discrepancy record is generated with the expected value (80), actual value (100), and the difference (20).

3. **Given** a validated invoice with a line item whose SKU does not exist in the linked Purchase Order, **When** reconciliation runs, **Then** a "Missing SKU" discrepancy record is generated identifying the unmatched SKU.

4. **Given** a validated invoice with a unit price of $12.50 for a SKU where the PO specifies $10.00, **When** reconciliation runs, **Then** a "Unit Price Mismatch" discrepancy record is generated with the expected price ($10.00) and actual price ($12.50).

5. **Given** a validated invoice with multiple discrepancies across different line items, **When** reconciliation runs, **Then** each discrepancy is recorded separately, and the invoice status reaches "Completed" (discrepancies are informational, not blocking).

6. **Given** a validated invoice with 3 line items but the linked PO has 5 line items, **When** reconciliation runs, **Then** a "Missing from Invoice" discrepancy record is generated for each of the 2 PO items not present in the invoice.

---

### User Story 4 - View Invoice Processing Status (Priority: P2)

A Procurement User or Administrator views the current processing status of an invoice, including its position in the lifecycle, any discrepancies found, and the complete processing history log showing every status transition and significant event.

**Why this priority**: Users need visibility into what happened to their uploaded invoices. Without status tracking, users cannot determine whether processing succeeded, failed, or is still in progress.

**Independent Test**: Upload an invoice, allow it to complete processing, then query the invoice status endpoint. Verify the response includes the current status, a list of discrepancies (if any), and a chronological processing history log.

**Acceptance Scenarios**:

1. **Given** an invoice that has completed processing with 2 discrepancies, **When** the Procurement User who uploaded it queries the invoice status, **Then** the system returns the current status "Completed", the list of discrepancy records with types and values, and the full processing history showing each status transition with timestamps.

2. **Given** an invoice currently in "Processing" status, **When** the Procurement User who uploaded it queries the invoice status, **Then** the system returns "Processing" status and the processing history up to the current point.

3. **Given** an invoice that failed during AI extraction, **When** the Procurement User who uploaded it queries the invoice status, **Then** the system returns "Failed" status with the error description in the processing history.

4. **Given** an invoice uploaded by a different Procurement User, **When** a Procurement User queries that invoice, **Then** the system returns HTTP 403 Forbidden.

---

### User Story 5 - Secure Invoice File Download (Priority: P2)

An authenticated user downloads the original uploaded invoice file through a secured endpoint. The system verifies the user's identity and authorization before serving the file. Files are never directly accessible via a public URL.

**Why this priority**: Users need to reference the original invoice document when reviewing discrepancies or verifying extracted data. Direct file access must be prevented for security.

**Independent Test**: Upload an invoice, then request the file download endpoint with valid credentials. Verify the file is returned with the correct content type. Attempt to access the file without authentication and verify it is rejected.

**Acceptance Scenarios**:

1. **Given** an authenticated Procurement User who uploaded the invoice, **When** the user requests to download the invoice file by invoice identifier, **Then** the system returns the file with the correct MIME type and original filename.

2. **Given** an unauthenticated request to the download endpoint, **When** the request is received, **Then** the system returns HTTP 401 Unauthorized.

3. **Given** a request with a non-existent invoice identifier, **When** the request is received, **Then** the system returns HTTP 404 Not Found.

4. **Given** a Procurement User who did NOT upload the invoice, **When** the user requests to download the file, **Then** the system returns HTTP 403 Forbidden.

---

### User Story 6 - Administrator Invoice Monitoring (Priority: P3)

An Administrator views a list of all invoices with their processing statuses, filters by status (e.g., show all "Failed" or "NeedsReview" invoices), and reviews the processing history and discrepancies for any invoice. This enables proactive monitoring and intervention for problematic invoices.

**Why this priority**: Administrative oversight is essential for production operations but is not required for the core upload-process-reconcile flow to function.

**Independent Test**: As an Administrator, query the invoice list endpoint with a status filter for "Failed". Verify the response includes only invoices with "Failed" status and includes enough detail (invoice identifier, upload date, error description) for the administrator to investigate.

**Acceptance Scenarios**:

1. **Given** an Administrator, **When** they request all invoices filtered by "Failed" status, **Then** the system returns only invoices with "Failed" status, including invoice identifier, upload timestamp, and the failure reason from the processing history.

2. **Given** an Administrator, **When** they request all invoices without a status filter, **Then** the system returns a paginated list of all invoices with their current statuses.

3. **Given** a non-Administrator user, **When** they attempt to access the administrative invoice monitoring endpoints, **Then** the system returns HTTP 403 Forbidden.

---

### Edge Cases

- What happens when the same invoice file is uploaded twice? The system MUST accept duplicate uploads as separate invoice records. Duplicate detection is out of scope for the MVP.
- What happens when the Purchase Order referenced by the invoice does not exist in the system? The system MUST mark the invoice as "NeedsReview" and log a processing history entry indicating the PO was not found.
- What happens when the AI service returns an empty line items array? The system MUST mark the invoice as "NeedsReview" with a processing log indicating no line items were extracted.
- What happens when the uploaded file is a valid PDF but contains no invoice content (e.g., a blank page)? The AI service may return empty or incomplete data. The backend MUST validate the response and mark the invoice as "NeedsReview" if required fields are missing.
- What happens when a background job fails after all retry attempts are exhausted? The invoice MUST remain in "Failed" status with the final error logged in processing history.
- What happens when a file passes extension and MIME validation but fails magic number validation? The upload MUST be rejected entirely. Partial validation passes do not count.
- What happens when the AI extraction succeeds but the invoice total does not match the sum of line item amounts? The system MUST generate an "Amount Mismatch" discrepancy record.
- What happens when the user provides a Purchase Order ID that does not exist in the system? The system MUST reject the upload with a validation error indicating the PO was not found. No file is stored and no invoice record is created.
- What happens when the invoice contains fewer items than the PO? The system MUST generate a "Missing from Invoice" discrepancy for each PO line item not matched by any invoice line item.

### AI Service Response Contract

The external AI service returns a structured JSON response with the following schema:

```json
{
  "vendorName": "string",
  "invoiceNumber": "string",
  "invoiceDate": "YYYY-MM-DD",
  "currency": "string",
  "subtotal": 0.00,
  "vat": 0.00,
  "total": 0.00,
  "items": [
    {
      "supplierSku": "string",
      "description": "string",
      "quantity": 0,
      "unitPrice": 0.00,
      "amount": 0.00
    }
  ]
}
```

**Required header fields**: vendorName, invoiceNumber, invoiceDate, total.
**Required item fields**: supplierSku, quantity, unitPrice, amount.
**Optional fields**: currency (defaults to USD if absent), subtotal, vat, description.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The system MUST allow authenticated users with the appropriate role/permission to upload invoice files. The upload request MUST include the Purchase Order ID to reconcile against.
- **FR-002**: The system MUST accept only PDF, JPG, JPEG, and PNG file formats.
- **FR-003**: The system MUST validate uploaded files by extension, MIME type, magic number (file signature), and file size before accepting them.
- **FR-004**: The system MUST reject any file that fails any validation check and return a structured error response explaining the rejection reason.
- **FR-005**: The system MUST store accepted files outside the web root directory, renaming them with a GUID to prevent path traversal and filename collision.
- **FR-006**: The system MUST create an Invoice record and an UploadedFile record for each accepted upload.
- **FR-007**: The system MUST enqueue a background processing job for each accepted invoice immediately after upload.
- **FR-008**: The system MUST process invoices asynchronously — the upload response MUST NOT wait for processing to complete.
- **FR-009**: The system MUST send the uploaded invoice to an external AI service for data extraction during background processing. The system MUST enforce a 30-second timeout for AI service responses.
- **FR-010**: The system MUST validate the AI service response against a defined schema (required fields, data types, value ranges) before persisting any extracted data.
- **FR-011**: The system MUST map AI response field names to internal domain model field names before persistence.
- **FR-012**: The system MUST persist extracted invoice header data (vendor name, invoice number, invoice date, currency, subtotal, VAT, total amount) and line items (supplier SKU, description, quantity, unit price, line amount).
- **FR-013**: The system MUST compare each extracted invoice line item against the linked Purchase Order using Supplier SKU as the matching key. Comparisons for quantity and unit price MUST use exact match (no tolerance threshold).
- **FR-014**: The system MUST generate discrepancy records for: Missing SKU (invoice item SKU not in PO), Missing from Invoice (PO item not in invoice), Quantity Mismatch, Unit Price Mismatch, and Amount Mismatch.
- **FR-015**: The system MUST persist all discrepancy records for later review.
- **FR-016**: The system MUST maintain a processing history log for each invoice, recording every status transition and significant event with timestamps.
- **FR-017**: The system MUST transition invoice status through the defined lifecycle: Uploaded → Queued → Processing → Extracted → Validated → Compared → Completed.
- **FR-018**: The system MUST support failure statuses: "Failed" for technical errors and "NeedsReview" for business validation issues requiring human intervention.
- **FR-019**: The system MUST provide a secured endpoint for downloading the original uploaded invoice file.
- **FR-020**: The system MUST provide endpoints for querying invoice status, processing history, and discrepancy records. Procurement Users MUST only access invoices they uploaded.
- **FR-021**: The system MUST provide administrative endpoints for listing invoices with status filtering and pagination. Administrators MUST have access to all invoices.
- **FR-022**: The system MUST retry failed background processing jobs according to a configurable retry policy.
- **FR-023**: The system MUST NOT allow any file to be accessed directly via a public URL — all file access MUST go through authenticated endpoints.
- **FR-024**: The system MUST enforce data ownership — Procurement Users MUST NOT be able to view, query, or download invoices uploaded by other users. Administrators are exempt from this restriction.

### Key Entities

- **Invoice**: Represents an uploaded invoice document. Contains header information (vendor name, invoice number, invoice date, currency, subtotal, VAT, total), processing status, link to uploaded file, link to Purchase Order (provided by user at upload), the uploading user's identity, and timestamps.
- **InvoiceItem**: Represents a single line item extracted from an invoice. Contains supplier SKU, description, quantity, unit price, and line amount.
- **UploadedFile**: Represents the physical file stored on the server. Contains original filename, stored filename (GUID), file path, content type, file size, and upload timestamp.
- **InvoiceProcessingLog**: Represents a single event in the processing history. Contains the event type, status transition, message/description, and timestamp.
- **Discrepancy**: Represents a mismatch found during PO reconciliation. Contains discrepancy type, field name, expected value, actual value, and the linked invoice item.

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: Users can upload an invoice and receive confirmation within 3 seconds (excluding network latency).
- **SC-002**: 95% of invoices complete the full processing pipeline (upload through reconciliation) within 60 seconds.
- **SC-003**: The system correctly identifies 100% of quantity, price, and SKU discrepancies when comparing extracted data against Purchase Orders.
- **SC-004**: Failed processing jobs are automatically retried at least 3 times before marking the invoice as permanently failed.
- **SC-005**: Every invoice has a complete, auditable processing history log with no gaps in status transitions.
- **SC-006**: Zero uploaded invoice files are accessible without authentication.
- **SC-007**: The system rejects 100% of files with mismatched content (e.g., executables renamed to .pdf) through multi-layer validation.
- **SC-008**: Administrators can identify all failed or needs-review invoices within 10 seconds through filtered listing.

## Assumptions

- Users have already been authenticated and authorized through the existing JWT-based authentication system before interacting with invoice features.
- Each uploaded invoice is associated with exactly one Purchase Order. The Purchase Order identifier is provided by the user at upload time. The AI response does not contain a PO reference.
- The external AI service is a third-party service that accepts document files and returns structured JSON. The service contract (request/response format) is defined and stable.
- The AI service handles OCR internally when needed (e.g., for scanned PDF or image-based invoices). The backend does not perform OCR.
- Purchase Orders and their line items already exist in the system (from Sprint 2 Excel import functionality) before invoice reconciliation runs.
- Duplicate invoice detection (identifying if the same physical invoice is uploaded more than once) is out of scope for this sprint.
- Bulk invoice upload (multiple files in a single request) is out of scope for this sprint.
- Real-time notifications (e.g., SignalR) for processing completion are out of scope for this sprint. Users poll or refresh to check status.
- The maximum file size for uploads is 10MB (reasonable default for invoice documents).
- The file storage location is configurable but defaults to a local filesystem directory outside the web root.
