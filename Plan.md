# Sprint 3 - Intelligent Invoice Processing Pipeline

## Overview

Sprint 3 introduces the intelligent invoice processing pipeline for the Smart Procurement Intelligence Platform (SPIP).

The objective of this sprint is to automate the invoice verification process by integrating secure document handling, AI-powered invoice extraction, business validation, and Purchase Order reconciliation into a single end-to-end workflow.

Instead of manually reviewing supplier invoices, authorized users upload invoice documents to the platform. The backend validates and securely stores the uploaded files, processes them asynchronously, communicates with an external AI service for invoice understanding, validates the returned data, reconciles the extracted information against Purchase Orders, detects discrepancies, and records the complete processing lifecycle.

The AI service is responsible only for document understanding and structured data extraction. All business rules, validation, persistence, reconciliation, workflow orchestration, and security remain the responsibility of the ASP.NET Core backend.

---

# Goals

The system should:

- Provide secure invoice uploads.
- Store uploaded invoices securely.
- Process invoices asynchronously.
- Integrate with an external AI service.
- Validate AI responses.
- Normalize extracted invoice data.
- Compare invoices against Purchase Orders.
- Detect procurement discrepancies.
- Maintain complete processing history.
- Ensure all business rules are enforced inside the backend.
- Follow Clean Architecture principles.

---

# Actors

## Procurement User

Responsible for:

- Uploading invoices
- Viewing processing status
- Reviewing discrepancies
- Downloading invoice files

---

## Administrator

Responsible for:

- Monitoring invoice processing
- Reviewing failed invoices
- Auditing processing history

---

## Hangfire Background Worker

Responsible for:

- Processing invoices asynchronously
- Calling the AI service
- Updating invoice status
- Retrying failed jobs

---

## AI Service

Responsible only for:

- OCR (when required)
- Text extraction
- Invoice understanding
- Structured JSON generation

The AI service must never:

- Update the database
- Perform reconciliation
- Execute business rules
- Access application repositories

---

# End-to-End Workflow

```text
User Uploads Invoice

↓

Authentication

↓

Authorization

↓

File Validation

    • Extension Validation
    • MIME Type Validation
    • Magic Number Validation
    • File Size Validation

↓

Secure File Storage

↓

Create Invoice Record

↓

Create UploadedFile Record

↓

Status = Uploaded

↓

Create Hangfire Job

↓

Status = Queued

↓

Background Processing Begins

↓

Status = Processing

↓

Call AI Service

↓

Receive Structured JSON

↓

JSON Schema Validation

↓

Business Validation

↓

Map AI Response to Internal Domain Model

↓

Store Extracted Invoice Data

↓

Compare Against Purchase Order

↓

Generate Discrepancies

↓

Store Processing Logs

↓

Status = Completed
```

---

# Functional Requirements

## Invoice Upload

The system shall:

- Allow authenticated users to upload invoices.
- Accept PDF, JPG, JPEG and PNG.
- Reject unsupported file types.
- Reject executable files.
- Store uploaded files outside the web root.
- Rename uploaded files using GUIDs.
- Persist uploaded file metadata.
- Create an Invoice record.
- Create an UploadedFile record.
- Queue invoice processing using Hangfire.

---

## Secure File Validation

Before processing begins the backend shall validate:

- File Extension
- MIME Type
- File Signature (Magic Number)
- File Size

If validation fails:

- Reject upload
- Return validation error
- Do not store the file
- Do not create invoice records

---

## Background Processing

Invoice processing must execute asynchronously.

Hangfire is responsible for:

- Queueing jobs
- Executing jobs
- Retry policy
- Failure recovery
- Monitoring

The frontend must never wait for processing completion.

---

## AI Integration

The backend shall:

- Send invoice identifier
- Send secure file location
- Wait for structured JSON response

The backend must never delegate business logic to the AI service.

---

## JSON Validation

After receiving the AI response the backend shall validate:

- JSON schema
- Required fields
- Data types
- Invalid values
- Missing items

Invalid responses must not continue to reconciliation.

---

## Data Mapping

The backend shall map AI field names into the internal domain model before persistence.

Example:

| AI Field | Internal Field |
|----------|----------------|
| supplier_item | SupplierSKU |
| qty | Quantity |
| unit_price | UnitPrice |
| description | Description |

---

## Invoice Persistence

The backend shall:

- Store invoice header
- Store invoice items
- Store AI extraction results
- Store uploaded file metadata
- Store processing logs

---

## Purchase Order Reconciliation

The backend shall compare:

- Supplier SKU
- Quantity
- Unit Price

The MVP shall not compare product names.

---

## Discrepancy Detection

The backend shall generate discrepancy records for:

- Missing SKU
- Quantity mismatch
- Unit price mismatch
- Amount mismatch

Each discrepancy must be stored for later review.

---

## Processing History

Every processing step must be logged.

Examples:

- Upload Completed
- AI Processing Started
- AI Processing Completed
- JSON Validation Completed
- Comparison Completed
- Processing Failed

---

## Invoice Download

Invoice files must be downloaded only through secured API endpoints.

Files must never be publicly accessible.

---

# Business Rules

- Only authenticated users may upload invoices.
- Only supported file types are accepted.
- Uploaded files must be stored securely.
- Every uploaded invoice must have a processing status.
- Every status transition must be persisted.
- Every processing step must be logged.
- AI is responsible only for data extraction.
- Business validation belongs exclusively to the backend.
- Purchase Order comparison uses Supplier SKU only.
- Discrepancies must always be stored.
- Invoice processing must execute asynchronously.

---

# Invoice Lifecycle

```text
Uploaded

↓

Queued

↓

Processing

↓

Extracted

↓

Validated

↓

Compared

↓

Completed
```

Failure scenarios:

```text
Processing

├── Failed

└── NeedsReview
```

---

# Status Definitions

| Status | Description |
|----------|-------------|
| Uploaded | Invoice uploaded successfully |
| Queued | Waiting for background processing |
| Processing | Invoice processing has started |
| Extracted | AI extraction completed |
| Validated | JSON and business validation completed |
| Compared | Purchase Order comparison completed |
| Completed | Processing finished successfully |
| Failed | Technical failure occurred |
| NeedsReview | Manual review is required |

---

# Security Requirements

The backend shall enforce:

- JWT Authentication
- Role-based Authorization
- File Extension Validation
- MIME Validation
- Magic Number Validation
- Secure Storage
- Secure Download Endpoints

Uploaded invoices must never be directly accessible.

---

# Non-Functional Requirements

The solution shall:

- Follow Clean Architecture.
- Follow SOLID principles.
- Use Repository Pattern.
- Use asynchronous processing.
- Support retry mechanisms.
- Maintain auditability.
- Be scalable for future AI providers.
- Keep AI isolated from business logic.

---

# Out of Scope

The following are not part of Sprint 3:

- AI model training
- OCR implementation details
- Frontend implementation
- ERP integration
- Barcode reconciliation
- SignalR notifications
- Automatic approval workflows
- Bulk invoice upload

---

# Acceptance Criteria

Sprint 3 is complete when:

- Users can upload invoices securely.
- Uploaded files are validated.
- Files are stored securely.
- Invoice metadata is persisted.
- Background jobs process invoices asynchronously.
- AI extracts structured invoice data.
- JSON is validated successfully.
- Extracted data is mapped correctly.
- Invoice data is stored.
- Purchase Orders are reconciled.
- Discrepancies are generated.
- Invoice status is updated throughout processing.
- Processing history is maintained.
- Invoice files can be downloaded securely.
- The solution follows Clean Architecture principles.