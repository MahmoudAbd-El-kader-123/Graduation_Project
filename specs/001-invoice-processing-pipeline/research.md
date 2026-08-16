# Research: Intelligent Invoice Processing Pipeline

## Decision 1: Entity Model Extensions

**Decision**: Extend existing `Invoice`, `InvoiceItem`, `Discrepancy`, `UploadedFile`, and `AIExtractionResult` entities. Add new `InvoiceProcessingLog` entity. Expand `InvoiceStatus` enum with pipeline lifecycle statuses.

**Rationale**: The project already has skeleton entities from Sprint 2 domain modeling. Extending them avoids breaking existing relationships (e.g., `PurchaseOrder.Invoices` navigation) and follows the established `BaseEntity` pattern. Adding `InvoiceProcessingLog` is the only net-new entity required — all others need field additions.

**Alternatives Considered**:
- Creating entirely new entities → Rejected: would duplicate relationships already wired in the DbContext.
- Using a JSON column for processing logs → Rejected: structured queryability is needed for admin filtering by status/error.

## Decision 2: SKU-Based Reconciliation via Product.Sku

**Decision**: Map AI-extracted `supplierSku` to existing `Product.Sku` field. Match invoice items to PO items via `Product.Sku` → `PurchaseOrderItem.ProductId` → `Product.Sku`.

**Rationale**: The existing domain already has `Product.Sku` (nullable string) and `PurchaseOrderItem.ProductId`. The AI returns `supplierSku` which maps to the product's SKU in the vendor catalog. Adding `SupplierSku` directly to `InvoiceItem` enables direct matching without Product lookup during reconciliation.

**Alternatives Considered**:
- Matching by ProductId directly → Rejected: the AI service doesn't know internal ProductIds.
- Matching by Description → Rejected: too fuzzy for precise reconciliation.

## Decision 3: Hangfire for Background Processing

**Decision**: Use Hangfire `BackgroundJob.Enqueue` for invoice processing jobs. The job class `InvoiceProcessingJob` is registered as a scoped service.

**Rationale**: Hangfire is already a project dependency (v1.8.14) with SQL Server storage. The `BackgroundJobs/` directory exists but is empty — this feature populates it. Hangfire provides built-in retry with exponential backoff, job persistence, and a dashboard.

**Alternatives Considered**:
- `IHostedService` / `BackgroundService` → Rejected: no built-in retry, no persistence, no dashboard.
- Message queue (RabbitMQ) → Rejected: adds unnecessary infrastructure; Hangfire with SQL Server is already configured.

## Decision 4: File Storage Strategy

**Decision**: Extend `IFileStorageService` with additional methods. Implement `LocalFileStorageService` storing files under a configurable directory outside the web root, using GUID filenames.

**Rationale**: `IFileStorageService` already exists with `SaveFileAsync`, `GetFileAsync`, `DeleteFileAsync`. The interface needs minor extension (return stored path with GUID filename). The local filesystem approach is the simplest for MVP with a clear upgrade path to Azure Blob Storage.

**Alternatives Considered**:
- Azure Blob Storage → Rejected for MVP: adds external dependency and configuration complexity.
- Database BLOB storage → Rejected: poor performance for large files, complicates backups.

## Decision 5: AI Service Integration Pattern

**Decision**: Extend `IAIExtractionService` to return a typed DTO (`AIExtractionResponseDto`) instead of raw string. The implementation uses `HttpClient` with a 30-second timeout.

**Rationale**: The current interface returns `Task<string>` (raw JSON). For the pipeline, we need typed deserialization with schema validation. Returning a typed DTO moves parsing responsibility to the infrastructure layer (where it belongs per Clean Architecture).

**Alternatives Considered**:
- Keep returning raw JSON and parse in Application layer → Rejected: violates Clean Architecture (Application layer shouldn't know about JSON serialization details of external services).

## Decision 6: Invoice-User Ownership via UploadedByUserId

**Decision**: Add `UploadedByUserId` (int) to the `Invoice` entity to track data ownership. Use `ICurrentUserService.UserId` at upload time.

**Rationale**: `ICurrentUserService` already exists and provides `UserId`. The ownership field enables FR-024 (data ownership enforcement) without adding a separate join table.

**Alternatives Considered**:
- Using `CreatedBy` from an audit interceptor → Rejected: `BaseEntity` doesn't have `CreatedBy`; the existing `AuditInterceptor` sets `CreatedAt`/`UpdatedAt` only.
