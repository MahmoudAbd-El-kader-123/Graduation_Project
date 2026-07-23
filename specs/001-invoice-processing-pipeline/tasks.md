# Tasks: Intelligent Invoice Processing Pipeline

**Input**: Design documents from `specs/001-invoice-processing-pipeline/`

**Prerequisites**: plan.md, spec.md, research.md, data-model.md, contracts/api-contracts.md, quickstart.md

**Organization**: Tasks grouped by user story. Each task includes exact file paths, detailed implementation instructions, and code-level specifications so that an implementing agent (Codex) can complete each task without ambiguity.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3)
- Include exact file paths in descriptions

## Path Conventions

- **Domain**: `SPIP.Domain/`
- **Application**: `SPIP.Application/`
- **Infrastructure**: `SPIP.Infrastructure/`
- **API**: `SPIP.API/`
- **Shared**: `SPIP.Shared/`

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Project-wide configuration changes that enable all subsequent phases

- [X] T001 Add Hangfire configuration in `SPIP.API/Program.cs` — Register Hangfire services with `builder.Services.AddHangfire(config => config.UseSqlServerStorage(connectionString))` and `builder.Services.AddHangfireServer()`. Add the Hangfire dashboard endpoint `app.UseHangfireDashboard("/hangfire")` behind admin authorization. The connection string should come from `appsettings.json` under `"ConnectionStrings:HangfireConnection"` (can reuse the same DB connection string as the main database). Also add `"HangfireConnection"` key to `appsettings.json` and `appsettings.Development.json`.

- [X] T002 Add file storage configuration in `appsettings.json` — Add a `"FileStorage"` section with `"BasePath": "App_Data/Invoices"` (a directory outside wwwroot). This path is used by `LocalFileStorageService`. Also add the `"AIService"` section with `"BaseUrl": "https://your-ai-service-url"`, `"TimeoutSeconds": 30`, and `"ApiKey": ""` (placeholder). Example:
  ```json
  "FileStorage": {
    "BasePath": "App_Data/Invoices"
  },
  "AIService": {
    "BaseUrl": "https://your-ai-service-url",
    "TimeoutSeconds": 30,
    "ApiKey": ""
  }
  ```

- [X] T002b Register authorization policies for Invoice permissions in `SPIP.API/Program.cs` (or wherever policies are configured) — Add policy registrations following the existing pattern used for `Permissions.POImports.*`. Register: `Permissions.Invoices.Upload`, `Permissions.Invoices.View`, `Permissions.Invoices.Download`, `Permissions.Invoices.ViewAll`. This MUST be done in Setup so policies exist before any controller endpoints are tested in later phases.

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Domain entities, enums, DTOs, interfaces, EF configurations, and DI registrations that ALL user stories depend on

**⚠️ CRITICAL**: No user story work can begin until this phase is complete

### Enums

- [X] T003 [P] Modify `InvoiceStatus` enum in `SPIP.Domain/Enums/InvoiceStatus.cs` — Replace the current enum values (`Pending=1, Matched=2, Discrepant=3, Approved=4, Paid=5, Rejected=6`) with the pipeline lifecycle statuses. The new enum must be:
  ```csharp
  namespace SPIP.Domain.Enums;

  public enum InvoiceStatus
  {
      Uploaded = 1,
      Queued = 2,
      Processing = 3,
      Extracted = 4,
      Validated = 5,
      Compared = 6,
      Completed = 7,
      Failed = 8,
      NeedsReview = 9
  }
  ```

- [X] T004 [P] Create `DiscrepancyType` enum in `SPIP.Domain/Enums/DiscrepancyType.cs` — New file. Define:
  ```csharp
  namespace SPIP.Domain.Enums;

  public enum DiscrepancyType
  {
      MissingSku = 1,
      MissingFromInvoice = 2,
      QuantityMismatch = 3,
      UnitPriceMismatch = 4,
      AmountMismatch = 5
  }
  ```

### Domain Entities

- [X] T005 [P] Modify `Invoice` entity in `SPIP.Domain/Entities/Invoice.cs` — Add the following properties to the existing class. Keep ALL existing properties and navigation properties unchanged. Add:
  ```csharp
  public string VendorName { get; set; } = string.Empty;  // AI-extracted vendor name
  public string Currency { get; set; } = "USD";            // AI-extracted currency
  public decimal Subtotal { get; set; }                    // AI-extracted subtotal
  public decimal Vat { get; set; }                         // AI-extracted VAT
  public int UploadedByUserId { get; set; }                // FK to User who uploaded
  public User? UploadedByUser { get; set; }                // Navigation property
  public ICollection<InvoiceProcessingLog> ProcessingLogs { get; set; } = new List<InvoiceProcessingLog>();
  ```
  Also add `using SPIP.Domain.Enums;` if not already present. Change the default `Status` from `InvoiceStatus.Pending` to `InvoiceStatus.Uploaded`. **IMPORTANT**: Change `PurchaseOrderId` from `int?` (nullable) to `int` (non-nullable) since the spec requires a PO ID at upload time. Update the navigation property accordingly: `public PurchaseOrder? PurchaseOrder { get; set; }` can remain nullable for EF navigation, but the FK `PurchaseOrderId` must be `int`.

- [X] T006 [P] Modify `InvoiceItem` entity in `SPIP.Domain/Entities/InvoiceItem.cs` — Add a `SupplierSku` property:
  ```csharp
  public string SupplierSku { get; set; } = string.Empty;  // AI-extracted supplier SKU, used for PO matching
  ```
  Keep all existing properties unchanged.

- [X] T007 [P] Modify `Discrepancy` entity in `SPIP.Domain/Entities/Discrepancy.cs` — Add `DiscrepancyType` enum property and optional FK to `InvoiceItem`:
  ```csharp
  using SPIP.Domain.Enums;
  // ... existing usings ...

  public DiscrepancyType DiscrepancyType { get; set; }  // Type of mismatch
  public int? InvoiceItemId { get; set; }                // Null for MissingFromInvoice type
  public InvoiceItem? InvoiceItem { get; set; }          // Navigation property
  ```
  Keep all existing properties unchanged.

- [X] T008 [P] Modify `UploadedFile` entity in `SPIP.Domain/Entities/UploadedFile.cs` — Add two properties for distinguishing original and stored filenames:
  ```csharp
  public string OriginalFileName { get; set; } = string.Empty;  // User's original filename
  public string StoredFileName { get; set; } = string.Empty;    // GUID-based filename on disk
  ```
  Keep all existing properties unchanged. The existing `FileName` property remains for backward compatibility.

- [X] T009 [P] Create `InvoiceProcessingLog` entity in `SPIP.Domain/Entities/InvoiceProcessingLog.cs` — New file:
  ```csharp
  using SPIP.Domain.Common;
  using SPIP.Domain.Enums;

  namespace SPIP.Domain.Entities;

  public class InvoiceProcessingLog : BaseEntity
  {
      public int InvoiceId { get; set; }
      public Invoice? Invoice { get; set; }
      public InvoiceStatus? FromStatus { get; set; }   // Null for initial "Uploaded" event
      public InvoiceStatus ToStatus { get; set; }
      public string EventType { get; set; } = string.Empty;  // e.g., "StatusChange", "ValidationError", "AIExtractionComplete"
      public string? Message { get; set; }             // Human-readable description or error
  }
  ```

### Permissions

- [X] T010 [P] Add `Invoices` permission group in `SPIP.Domain/Constants/Permissions.cs` — Add a new static class inside the `Permissions` class:
  ```csharp
  public static class Invoices
  {
      public const string Upload = "Invoices.Upload";
      public const string View = "Invoices.View";
      public const string Download = "Invoices.Download";
      public const string ViewAll = "Invoices.ViewAll";  // Admin-only: list all invoices
  }
  ```
  Keep all existing permission groups unchanged. Also seed these permissions in the database seed file at `SPIP.Infrastructure/Persistence/Seed/` if a permission seeder exists, or add them to `SPIP.Domain/Entities/PermissionCatalog.cs` if that's how permissions are registered.

### DTOs

- [X] T011 [P] Create request DTOs in `SPIP.Application/DTOs/Invoice/` — Create the following new files:

  **`UploadInvoiceRequest.cs`**:
  ```csharp
  using Microsoft.AspNetCore.Http;

  namespace SPIP.Application.DTOs.Invoice;

  public class UploadInvoiceRequest
  {
      public IFormFile File { get; set; } = null!;
      public int PurchaseOrderId { get; set; }
  }
  ```

  **`InvoiceListParameters.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class InvoiceListParameters
  {
      public string? Status { get; set; }
      public int PageNumber { get; set; } = 1;
      public int PageSize { get; set; } = 10;
  }
  ```

- [X] T012 [P] Create response DTOs in `SPIP.Application/DTOs/Invoice/` — Create the following new files:

  **`InvoiceUploadResultDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public record InvoiceUploadResultDto(int InvoiceId, string Status, string FileName);
  ```

  **`InvoiceDetailDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class InvoiceDetailDto
  {
      public int Id { get; set; }
      public string InvoiceNumber { get; set; } = string.Empty;
      public string VendorName { get; set; } = string.Empty;
      public int VendorId { get; set; }
      public int? PurchaseOrderId { get; set; }
      public string Status { get; set; } = string.Empty;
      public DateTime InvoiceDate { get; set; }
      public string Currency { get; set; } = string.Empty;
      public decimal Subtotal { get; set; }
      public decimal Vat { get; set; }
      public decimal TotalAmount { get; set; }
      public DateTime UploadedAt { get; set; }
      public List<InvoiceItemDto> Items { get; set; } = [];
      public List<DiscrepancyDto> Discrepancies { get; set; } = [];
      public List<InvoiceProcessingLogDto> ProcessingLogs { get; set; } = [];
  }
  ```

  **`InvoiceItemDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class InvoiceItemDto
  {
      public int Id { get; set; }
      public string SupplierSku { get; set; } = string.Empty;
      public string Description { get; set; } = string.Empty;
      public int Quantity { get; set; }
      public decimal UnitPrice { get; set; }
      public decimal LineTotal { get; set; }
  }
  ```

  **`DiscrepancyDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class DiscrepancyDto
  {
      public int Id { get; set; }
      public string DiscrepancyType { get; set; } = string.Empty;
      public string FieldName { get; set; } = string.Empty;
      public string ExpectedValue { get; set; } = string.Empty;
      public string ActualValue { get; set; } = string.Empty;
      public bool IsResolved { get; set; }
  }
  ```

  **`InvoiceProcessingLogDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class InvoiceProcessingLogDto
  {
      public int Id { get; set; }
      public string? FromStatus { get; set; }
      public string ToStatus { get; set; } = string.Empty;
      public string EventType { get; set; } = string.Empty;
      public string? Message { get; set; }
      public DateTime Timestamp { get; set; }
  }
  ```

  **`InvoiceListItemDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.Invoice;

  public class InvoiceListItemDto
  {
      public int Id { get; set; }
      public string InvoiceNumber { get; set; } = string.Empty;
      public string VendorName { get; set; } = string.Empty;
      public string Status { get; set; } = string.Empty;
      public decimal TotalAmount { get; set; }
      public DateTime InvoiceDate { get; set; }
      public DateTime UploadedAt { get; set; }
      public string? UploadedByUserEmail { get; set; }
      public string? LastError { get; set; }
  }
  ```

- [X] T013 [P] Create AI extraction DTOs in `SPIP.Application/DTOs/AI/` — Create the following new files (do NOT modify existing `AIExtractionResultDto.cs`):

  **`AIExtractionResponseDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.AI;

  public class AIExtractionResponseDto
  {
      public string VendorName { get; set; } = string.Empty;
      public string InvoiceNumber { get; set; } = string.Empty;
      public string InvoiceDate { get; set; } = string.Empty;  // "YYYY-MM-DD" format
      public string? Currency { get; set; }
      public decimal? Subtotal { get; set; }
      public decimal? Vat { get; set; }
      public decimal Total { get; set; }
      public List<AIExtractionItemDto> Items { get; set; } = [];
  }
  ```

  **`AIExtractionItemDto.cs`**:
  ```csharp
  namespace SPIP.Application.DTOs.AI;

  public class AIExtractionItemDto
  {
      public string SupplierSku { get; set; } = string.Empty;
      public string? Description { get; set; }
      public int Quantity { get; set; }
      public decimal UnitPrice { get; set; }
      public decimal Amount { get; set; }
  }
  ```

### Modify existing InvoiceDto

- [X] T014 [P] Update existing `InvoiceDto` in `SPIP.Application/DTOs/Invoice/InvoiceDto.cs` — Add the new fields to the existing class:
  ```csharp
  public string VendorName { get; set; } = string.Empty;
  public int? PurchaseOrderId { get; set; }
  public string Currency { get; set; } = string.Empty;
  public decimal Subtotal { get; set; }
  public decimal Vat { get; set; }
  public DateTime UploadedAt { get; set; }
  ```
  Keep all existing properties.

### Service Interfaces

- [X] T015 [P] Create `IInvoiceService` interface in `SPIP.Application/Interfaces/Services/IInvoiceService.cs` — New file:
  ```csharp
  using SPIP.Application.DTOs.Invoice;
  using SPIP.Shared.Pagination;
  using SPIP.Shared.Result;

  namespace SPIP.Application.Interfaces.Services;

  public interface IInvoiceService
  {
      Task<Result<InvoiceUploadResultDto>> UploadInvoiceAsync(UploadInvoiceRequest request, CancellationToken cancellationToken = default);
      Task<Result<InvoiceDetailDto>> GetByIdAsync(int invoiceId, CancellationToken cancellationToken = default);
      Task<Result<(Stream FileStream, string ContentType, string FileName)>> DownloadFileAsync(int invoiceId, CancellationToken cancellationToken = default);
      Task<Result<PagedResult<InvoiceListItemDto>>> GetAllPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default);
  }
  ```

- [X] T016 [P] Create `IInvoiceProcessingService` interface in `SPIP.Application/Interfaces/Services/IInvoiceProcessingService.cs` — New file. This service orchestrates the entire background pipeline (extraction → validation → reconciliation):
  ```csharp
  namespace SPIP.Application.Interfaces.Services;

  public interface IInvoiceProcessingService
  {
      Task ProcessInvoiceAsync(int invoiceId, CancellationToken cancellationToken = default);
  }
  ```

- [X] T017 [P] Create `IReconciliationService` interface in `SPIP.Application/Interfaces/Services/IReconciliationService.cs` — New file:
  ```csharp
  namespace SPIP.Application.Interfaces.Services;

  public interface IReconciliationService
  {
      Task ReconcileAsync(int invoiceId, CancellationToken cancellationToken = default);
  }
  ```

- [X] T018 [P] Modify `IAIExtractionService` interface in `SPIP.Application/Interfaces/AI/IAIExtractionService.cs` — Change the return type from `Task<string>` to return a typed DTO. The new interface:
  ```csharp
  using SPIP.Application.DTOs.AI;

  namespace SPIP.Application.Interfaces.AI;

  public interface IAIExtractionService
  {
      Task<AIExtractionResponseDto> ExtractInvoiceDataAsync(Stream fileStream, string fileName, CancellationToken cancellationToken = default);
  }
  ```

- [X] T019 [P] Modify `IFileStorageService` interface in `SPIP.Application/Interfaces/Storage/IFileStorageService.cs` — Add a method that saves with a GUID filename and returns both the stored path and GUID filename:
  ```csharp
  namespace SPIP.Application.Interfaces.Storage;

  public interface IFileStorageService
  {
      Task<string> SaveFileAsync(Stream fileStream, string fileName);
      Task<(string StoredPath, string StoredFileName)> SaveFileWithGuidAsync(Stream fileStream, string originalFileName, string contentType, CancellationToken cancellationToken = default);
      Task<Stream> GetFileAsync(string path);
      Task DeleteFileAsync(string path);
  }
  ```

### Repository Interfaces

- [X] T020 [P] Create `IInvoiceRepository` interface in `SPIP.Application/Interfaces/Repositories/IInvoiceRepository.cs` — New file:
  ```csharp
  using SPIP.Application.DTOs.Invoice;
  using SPIP.Domain.Entities;

  namespace SPIP.Application.Interfaces.Repositories;

  public interface IInvoiceRepository : IGenericRepository<Invoice>
  {
      Task<Invoice?> GetWithDetailsByIdAsync(int id, CancellationToken cancellationToken = default);
      Task<(IReadOnlyList<Invoice> Items, int TotalCount)> GetPagedAsync(InvoiceListParameters parameters, CancellationToken cancellationToken = default);
      Task<IReadOnlyList<Invoice>> GetByUserIdAsync(int userId, CancellationToken cancellationToken = default);
  }
  ```

- [X] T021 [P] Create `IInvoiceProcessingLogRepository` interface in `SPIP.Application/Interfaces/Repositories/IInvoiceProcessingLogRepository.cs` — New file:
  ```csharp
  using SPIP.Domain.Entities;

  namespace SPIP.Application.Interfaces.Repositories;

  public interface IInvoiceProcessingLogRepository : IGenericRepository<InvoiceProcessingLog>
  {
      Task<IReadOnlyList<InvoiceProcessingLog>> GetByInvoiceIdAsync(int invoiceId, CancellationToken cancellationToken = default);
  }
  ```

### FluentValidation

- [X] T022 [P] Create `UploadInvoiceRequestValidator` in `SPIP.Application/Validators/UploadInvoiceRequestValidator.cs` — New file. Validates the upload request DTO:
  ```csharp
  using FluentValidation;
  using SPIP.Application.DTOs.Invoice;

  namespace SPIP.Application.Validators;

  public class UploadInvoiceRequestValidator : AbstractValidator<UploadInvoiceRequest>
  {
      private static readonly string[] AllowedExtensions = [".pdf", ".jpg", ".jpeg", ".png"];
      private static readonly string[] AllowedContentTypes = ["application/pdf", "image/jpeg", "image/png"];
      private const long MaxFileSizeBytes = 10 * 1024 * 1024; // 10MB

      public UploadInvoiceRequestValidator()
      {
          RuleFor(x => x.File)
              .NotNull().WithMessage("File is required.")
              .Must(f => f != null && f.Length > 0).WithMessage("File must not be empty.")
              .Must(f => f != null && f.Length <= MaxFileSizeBytes).WithMessage("File size must not exceed 10MB.")
              .Must(f => f != null && AllowedExtensions.Contains(Path.GetExtension(f.FileName).ToLowerInvariant()))
                  .WithMessage("Only PDF, JPG, JPEG, and PNG files are accepted.")
              .Must(f => f != null && AllowedContentTypes.Contains(f.ContentType.ToLowerInvariant()))
                  .WithMessage("File content type is not allowed.");

          RuleFor(x => x.PurchaseOrderId)
              .GreaterThan(0).WithMessage("A valid Purchase Order ID is required.");
      }
  }
  ```

### AutoMapper Profile

- [X] T023 [P] Create `InvoiceMappingProfile` in `SPIP.Application/Mapping/InvoiceMappingProfile.cs` — New file. Define AutoMapper mappings for all invoice-related entities → DTOs:
  ```csharp
  using AutoMapper;
  using SPIP.Application.DTOs.Invoice;
  using SPIP.Domain.Entities;

  namespace SPIP.Application.Mapping;

  public class InvoiceMappingProfile : Profile
  {
      public InvoiceMappingProfile()
      {
          CreateMap<Invoice, InvoiceDto>()
              .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
              .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt));

          CreateMap<Invoice, InvoiceDetailDto>()
              .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
              .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt));

          CreateMap<Invoice, InvoiceListItemDto>()
              .ForMember(d => d.Status, opt => opt.MapFrom(s => s.Status.ToString()))
              .ForMember(d => d.UploadedAt, opt => opt.MapFrom(s => s.CreatedAt))
              .ForMember(d => d.UploadedByUserEmail, opt => opt.MapFrom(s => s.UploadedByUser != null ? s.UploadedByUser.Email : null))
              .ForMember(d => d.LastError, opt => opt.Ignore()); // Populated manually from processing logs

          CreateMap<InvoiceItem, InvoiceItemDto>();

          CreateMap<Discrepancy, DiscrepancyDto>()
              .ForMember(d => d.DiscrepancyType, opt => opt.MapFrom(s => s.DiscrepancyType.ToString()));

          CreateMap<InvoiceProcessingLog, InvoiceProcessingLogDto>()
              .ForMember(d => d.FromStatus, opt => opt.MapFrom(s => s.FromStatus.HasValue ? s.FromStatus.Value.ToString() : null))
              .ForMember(d => d.ToStatus, opt => opt.MapFrom(s => s.ToStatus.ToString()))
              .ForMember(d => d.Timestamp, opt => opt.MapFrom(s => s.CreatedAt));
      }
  }
  ```

### EF Core Configurations

- [X] T024 [P] Update `InvoiceConfiguration` in `SPIP.Infrastructure/Persistence/Configurations/InvoiceConfiguration.cs` — Add precision for new decimal columns, max lengths for string columns, and indexes:
  ```csharp
  builder.Property(i => i.InvoiceNumber).HasMaxLength(100);
  builder.Property(i => i.VendorName).HasMaxLength(200);
  builder.Property(i => i.Currency).HasMaxLength(10).HasDefaultValue("USD");
  builder.Property(i => i.Subtotal).HasPrecision(18, 2);
  builder.Property(i => i.Vat).HasPrecision(18, 2);
  // Existing: builder.Property(i => i.TotalAmount).HasPrecision(18, 2);
  builder.HasIndex(i => i.UploadedByUserId);
  builder.HasIndex(i => i.Status);
  builder.HasOne(i => i.UploadedByUser).WithMany().HasForeignKey(i => i.UploadedByUserId).OnDelete(DeleteBehavior.Restrict);
  ```

- [X] T025 [P] Update `InvoiceItemConfiguration` in `SPIP.Infrastructure/Persistence/Configurations/InvoiceItemConfiguration.cs` — Add `SupplierSku` max length:
  ```csharp
  builder.Property(ii => ii.SupplierSku).HasMaxLength(50).IsRequired();
  ```

- [X] T026 [P] Create `DiscrepancyConfiguration` in `SPIP.Infrastructure/Persistence/Configurations/DiscrepancyConfiguration.cs` — New file:
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using SPIP.Domain.Entities;

  namespace SPIP.Infrastructure.Persistence.Configurations;

  public class DiscrepancyConfiguration : IEntityTypeConfiguration<Discrepancy>
  {
      public void Configure(EntityTypeBuilder<Discrepancy> builder)
      {
          builder.Property(d => d.FieldName).HasMaxLength(100);
          builder.Property(d => d.ExpectedValue).HasMaxLength(200);
          builder.Property(d => d.ActualValue).HasMaxLength(200);
          builder.HasOne(d => d.InvoiceItem).WithMany().HasForeignKey(d => d.InvoiceItemId).OnDelete(DeleteBehavior.SetNull);
      }
  }
  ```

- [X] T027 [P] Create `UploadedFileConfiguration` in `SPIP.Infrastructure/Persistence/Configurations/UploadedFileConfiguration.cs` — New file:
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using SPIP.Domain.Entities;

  namespace SPIP.Infrastructure.Persistence.Configurations;

  public class UploadedFileConfiguration : IEntityTypeConfiguration<UploadedFile>
  {
      public void Configure(EntityTypeBuilder<UploadedFile> builder)
      {
          builder.Property(u => u.OriginalFileName).HasMaxLength(255);
          builder.Property(u => u.StoredFileName).HasMaxLength(100);
          builder.Property(u => u.FileName).HasMaxLength(255);
          builder.Property(u => u.ContentType).HasMaxLength(100);
      }
  }
  ```

- [X] T028 [P] Create `InvoiceProcessingLogConfiguration` in `SPIP.Infrastructure/Persistence/Configurations/InvoiceProcessingLogConfiguration.cs` — New file:
  ```csharp
  using Microsoft.EntityFrameworkCore;
  using Microsoft.EntityFrameworkCore.Metadata.Builders;
  using SPIP.Domain.Entities;

  namespace SPIP.Infrastructure.Persistence.Configurations;

  public class InvoiceProcessingLogConfiguration : IEntityTypeConfiguration<InvoiceProcessingLog>
  {
      public void Configure(EntityTypeBuilder<InvoiceProcessingLog> builder)
      {
          builder.Property(l => l.EventType).HasMaxLength(100).IsRequired();
          builder.Property(l => l.Message).HasMaxLength(2000);
          builder.HasIndex(l => new { l.InvoiceId, l.CreatedAt });
      }
  }
  ```

### DbContext & Migration

- [X] T029 Add DbSets to `ApplicationDbContext` in `SPIP.Infrastructure/Persistence/Context/ApplicationDbContext.cs` — Add the following DbSet properties (if not already present):
  ```csharp
  public DbSet<InvoiceProcessingLog> InvoiceProcessingLogs => Set<InvoiceProcessingLog>();
  ```
  Ensure `DbSet<Invoice>`, `DbSet<InvoiceItem>`, `DbSet<Discrepancy>`, `DbSet<UploadedFile>`, `DbSet<AIExtractionResult>` also exist. If any are missing, add them. Make sure the `OnModelCreating` method calls `ApplyConfigurationsFromAssembly` or explicitly applies each new configuration.

- [X] T030 Generate EF Core migration — Run from repository root:
  ```bash
  dotnet ef migrations add AddInvoicePipeline --project SPIP.Infrastructure --startup-project SPIP.API
  ```
  Then apply: `dotnet ef database update --project SPIP.Infrastructure --startup-project SPIP.API`. Verify the migration includes all new columns, enums, indexes, and the new `InvoiceProcessingLog` table.

### Repository Implementations

- [X] T031 [P] Create `InvoiceRepository` in `SPIP.Infrastructure/Repositories/InvoiceRepository.cs` — New file. Extend `GenericRepository<Invoice>` and implement `IInvoiceRepository`. Use `.Include()` for eager loading in detail methods:
  - `GetWithDetailsByIdAsync`: Include `Items`, `Discrepancies`, `ProcessingLogs`, `UploadedFiles`, `UploadedByUser`. Order `ProcessingLogs` by `CreatedAt`.
  - `GetPagedAsync`: Support optional `Status` filter (parse string to `InvoiceStatus` enum). Include `UploadedByUser` for email. Order by `CreatedAt` descending. Return `(items, totalCount)` for pagination.
  - `GetByUserIdAsync`: Filter by `UploadedByUserId`, order by `CreatedAt` descending.

- [X] T032 [P] Create `InvoiceProcessingLogRepository` in `SPIP.Infrastructure/Repositories/InvoiceProcessingLogRepository.cs` — New file. Extend `GenericRepository<InvoiceProcessingLog>` and implement `IInvoiceProcessingLogRepository`:
  - `GetByInvoiceIdAsync`: Filter by `InvoiceId`, order by `CreatedAt` ascending.

### UnitOfWork Update

- [X] T033 Update `IUnitOfWork` in `SPIP.Application/Interfaces/Repositories/IUnitOfWork.cs` and `UnitOfWork` in `SPIP.Infrastructure/Repositories/UnitOfWork.cs` — Add invoice-related repository properties:
  ```csharp
  // In IUnitOfWork interface:
  IInvoiceRepository Invoices { get; }
  IInvoiceProcessingLogRepository InvoiceProcessingLogs { get; }

  // In UnitOfWork class - add lazy fields:
  private IInvoiceRepository? _invoices;
  private IInvoiceProcessingLogRepository? _invoiceProcessingLogs;

  public IInvoiceRepository Invoices => _invoices ??= new InvoiceRepository(_context);
  public IInvoiceProcessingLogRepository InvoiceProcessingLogs => _invoiceProcessingLogs ??= new InvoiceProcessingLogRepository(_context);
  ```

### DI Registration

- [X] T034 Update DI registrations in `SPIP.Infrastructure/DependencyInjection/DependencyInjection.cs` — Add registrations for all new services and repositories. Add these lines in the `AddInfrastructure` method:
  ```csharp
  // Repositories
  services.AddScoped<IInvoiceRepository, InvoiceRepository>();
  services.AddScoped<IInvoiceProcessingLogRepository, InvoiceProcessingLogRepository>();

  // Services
  services.AddScoped<IInvoiceService, InvoiceService>();
  services.AddScoped<IInvoiceProcessingService, InvoiceProcessingService>();
  services.AddScoped<IReconciliationService, ReconciliationService>();
  services.AddScoped<IFileStorageService, LocalFileStorageService>();

  // HttpClient for AI service (NOTE: AddHttpClient registers the service automatically — do NOT also call AddScoped for IAIExtractionService)
  services.AddHttpClient<IAIExtractionService, AIExtractionService>((sp, client) =>
  {
      var config = sp.GetRequiredService<IConfiguration>();
      client.BaseAddress = new Uri(config["AIService:BaseUrl"]!);
      client.Timeout = TimeSpan.FromSeconds(config.GetValue<int>("AIService:TimeoutSeconds", 30));
  });
  ```
  Add the necessary `using` statements for all new interfaces and implementations.

**Checkpoint**: Foundation ready — all entities, DTOs, interfaces, configurations, repositories, and DI wiring are in place. User story implementation can now begin.

---

## Phase 3: User Story 1 — Upload Invoice for Processing (Priority: P1) 🎯 MVP

**Goal**: Authenticated users upload an invoice file with a PO ID, the system validates the file (extension, MIME, magic number, size), stores it securely, creates Invoice + UploadedFile records, and enqueues a Hangfire background job.

**Independent Test**: `POST api/invoices/upload` with a valid PDF and PO ID → returns 200 with invoiceId and "Queued" status. File exists on disk with GUID name. Invoice record in DB.

### Implementation for User Story 1

- [X] T035 [US1] Implement `LocalFileStorageService` in `SPIP.Infrastructure/Services/LocalFileStorageService.cs` — New file. Implements `IFileStorageService`. Constructor takes `IConfiguration` to read `FileStorage:BasePath`. Implement `SaveFileWithGuidAsync`:
  1. Generate a GUID filename: `$"{Guid.NewGuid()}{Path.GetExtension(originalFileName)}"`.
  2. Create the base directory if it doesn't exist.
  3. Write the stream to `Path.Combine(basePath, guidFileName)`.
  4. Return `(fullPath, guidFileName)`.
  Also implement `GetFileAsync` (return `new FileStream(path, FileMode.Open, FileAccess.Read)`) and `DeleteFileAsync` (`File.Delete(path)`). Keep existing `SaveFileAsync` for backward compatibility (delegate to `SaveFileWithGuidAsync` internally).

- [X] T036 [US1] Create file validation utility in `SPIP.Application/Helpers/FileValidationHelper.cs` — New file. Static class with a method `ValidateFileSignature(Stream fileStream, string extension)` that validates magic numbers (file signatures):
  - PDF: first 4 bytes = `0x25 0x50 0x44 0x46` (`%PDF`)
  - JPEG: first 2 bytes = `0xFF 0xD8`
  - PNG: first 4 bytes = `0x89 0x50 0x4E 0x47`
  Returns `bool`. The method must reset the stream position to 0 after reading. Also add a method `GetContentTypeFromExtension(string extension)` returning the MIME type string.

- [X] T037 [US1] Implement `InvoiceService.UploadInvoiceAsync` in `SPIP.Infrastructure/Services/InvoiceService.cs` — New file. Implements `IInvoiceService`. Constructor takes: `IInvoiceRepository`, `IInvoiceProcessingLogRepository`, `IFileStorageService`, `ICurrentUserService`, `IPurchaseOrderRepository`, `IUnitOfWork`, `IMapper`, `Hangfire.IBackgroundJobClient`, `ILogger<InvoiceService>`. Implement `UploadInvoiceAsync`:
  1. Get current user ID from `ICurrentUserService.UserId`. If null, return `Result<>.Failure("User not authenticated.")`.
  2. Validate PO exists: `await _purchaseOrderRepo.GetByIdAsync(request.PurchaseOrderId)`. If null, return `Result<>.Failure("Purchase Order not found.")`.
  3. Validate file signature using `FileValidationHelper.ValidateFileSignature`. If invalid, return `Result<>.Failure("File content does not match the declared type.")`.
  4. Save file: `var (storedPath, storedFileName) = await _fileStorageService.SaveFileWithGuidAsync(...)`.
  5. Create `Invoice` entity with `Status = InvoiceStatus.Uploaded`, set `PurchaseOrderId`, `UploadedByUserId`, `VendorId` from PO's VendorId.
  6. Create `UploadedFile` entity with `OriginalFileName`, `StoredFileName`, `StoragePath`, `ContentType`, `FileSizeBytes`, linked to Invoice.
  7. Create initial `InvoiceProcessingLog` with `FromStatus = null`, `ToStatus = InvoiceStatus.Uploaded`, `EventType = "StatusChange"`, `Message = "Invoice uploaded successfully."`.
  8. Save via UoW: `await _unitOfWork.SaveChangesAsync()`.
  9. Transition status to `Queued`, add another processing log.
  10. Save again.
  11. Enqueue Hangfire job: `_backgroundJobClient.Enqueue<InvoiceProcessingJob>(job => job.ProcessAsync(invoice.Id, CancellationToken.None))`.
  12. Return `Result<InvoiceUploadResultDto>.Success(new InvoiceUploadResultDto(invoice.Id, "Queued", request.File.FileName))`.
  Log at each significant step using `_logger.LogInformation(...)`.

- [X] T038 [US1] Create `InvoiceProcessingJob` in `SPIP.Infrastructure/BackgroundJobs/InvoiceProcessingJob.cs` — New file. This is the Hangfire job class that delegates to `IInvoiceProcessingService`:
  ```csharp
  using Hangfire;
  using SPIP.Application.Interfaces.Services;

  namespace SPIP.Infrastructure.BackgroundJobs;

  public class InvoiceProcessingJob
  {
      private readonly IInvoiceProcessingService _processingService;

      public InvoiceProcessingJob(IInvoiceProcessingService processingService)
      {
          _processingService = processingService;
      }

      [AutomaticRetry(Attempts = 3, DelaysInSeconds = [10, 30, 60])]
      public async Task ProcessAsync(int invoiceId, CancellationToken cancellationToken)
      {
          await _processingService.ProcessInvoiceAsync(invoiceId, cancellationToken);
      }
  }
  ```

- [X] T039 [US1] Create `InvoicesController` in `SPIP.API/Controllers/InvoicesController.cs` — New file. Thin controller following the existing pattern in `PurchaseOrdersController.cs`:
  ```csharp
  [ApiController]
  [Route("api/invoices")]
  [Authorize]
  public class InvoicesController : ControllerBase
  {
      private readonly IInvoiceService _invoiceService;

      public InvoicesController(IInvoiceService invoiceService)
      {
          _invoiceService = invoiceService;
      }

      [HttpPost("upload")]
      [Authorize(Policy = Permissions.Invoices.Upload)]
      public async Task<ActionResult<ApiResponse<InvoiceUploadResultDto>>> Upload([FromForm] UploadInvoiceRequest request)
      {
          var result = await _invoiceService.UploadInvoiceAsync(request);
          return result.Succeeded
              ? Ok(ApiResponse<InvoiceUploadResultDto>.SuccessResponse(result.Data!, "Invoice uploaded and queued for processing."))
              : BadRequest(ApiResponse<InvoiceUploadResultDto>.FailureResponse(result.Error!));
      }
  }
  ```
  Add necessary `using` statements for DTOs, services, permissions, shared responses.

**Checkpoint**: User Story 1 complete. Users can upload invoices, files are securely stored, and background jobs are enqueued.

---

## Phase 4: User Story 2 — Automated Invoice Data Extraction (Priority: P1)

**Goal**: Background job sends the invoice to the AI service, receives structured JSON, validates the response schema and business rules, maps AI fields to domain entities, and persists extracted data.

**Independent Test**: Upload an invoice → Hangfire processes it → Invoice record has extracted header data and line items in DB. Status progresses through Processing → Extracted → Validated.

### Implementation for User Story 2

- [X] T040 [US2] Implement `AIExtractionService` in `SPIP.Infrastructure/Services/AIExtractionService.cs` — New file. Implements `IAIExtractionService`. Constructor takes `HttpClient` (injected by `IHttpClientFactory`), `ILogger<AIExtractionService>`. Implement `ExtractInvoiceDataAsync`:
  1. Create a `MultipartFormDataContent` with the file stream as `StreamContent`.
  2. Send `POST` to the AI service endpoint (path configured or hardcoded as `/extract` — adjust as needed).
  3. Read the response body as string.
  4. Deserialize using `System.Text.Json.JsonSerializer.Deserialize<AIExtractionResponseDto>` with `PropertyNameCaseInsensitive = true`.
  5. If deserialization returns null or response status is not success, throw an `HttpRequestException` with details.
  6. Return the typed `AIExtractionResponseDto`.
  7. Log the request/response (without sensitive data). Handle `TaskCanceledException` (timeout) by logging and rethrowing.

- [X] T041 [US2] Implement `InvoiceProcessingService` in `SPIP.Infrastructure/Services/InvoiceProcessingService.cs` — New file. Implements `IInvoiceProcessingService`. Constructor takes `IInvoiceRepository`, `IInvoiceProcessingLogRepository`, `IAIExtractionService`, `IFileStorageService`, `IReconciliationService`, `IUnitOfWork`, `ILogger<InvoiceProcessingService>`. Implement `ProcessInvoiceAsync(int invoiceId, CancellationToken ct)`:
  1. Load invoice with details: `await _invoiceRepo.GetWithDetailsByIdAsync(invoiceId)`. If null, throw `InvalidOperationException`.
  2. **Transition to Processing**: Update `invoice.Status = InvoiceStatus.Processing`. Add processing log. Save.
  3. **Call AI service**: Get the file stream via `_fileStorageService.GetFileAsync(uploadedFile.StoragePath)`. Call `_aiService.ExtractInvoiceDataAsync(stream, uploadedFile.OriginalFileName, ct)`.
  4. **Transition to Extracted**: Update status. Add log. Save.
  5. **Validate AI response** (business rules):
     - Check required fields: `VendorName`, `InvoiceNumber`, `InvoiceDate`, `Total` must not be null/empty.
     - Check `Items` is not null and has at least one item.
     - Check each item: `Quantity > 0`, `UnitPrice > 0`, `SupplierSku` not empty.
     - If validation fails: set `Status = NeedsReview`, add log with error details, save, and return.
  6. **Map AI response to domain**: Set `invoice.InvoiceNumber`, `invoice.VendorName`, `invoice.InvoiceDate` (parse "YYYY-MM-DD"), `invoice.Currency`, `invoice.Subtotal`, `invoice.Vat`, `invoice.TotalAmount`. For each AI item, create an `InvoiceItem` with `SupplierSku`, `Description`, `Quantity`, `UnitPrice`, `LineTotal = item.Amount`. Add to `invoice.Items`.
  7. **Save raw JSON** in `AIExtractionResult`: Create entity with `RawExtractedJson = JsonSerializer.Serialize(aiResponse)`, `ConfidenceScore = 1.0`, `ModelUsed = "external-ai"`.
  8. **Transition to Validated**: Update status. Add log. Save.
  9. **Call reconciliation**: `await _reconciliationService.ReconcileAsync(invoiceId, ct)`.
  10. Wrap the entire method in try-catch. On exception: set `Status = Failed`, add log with `ex.Message`, save, and rethrow (so Hangfire retries).

**Checkpoint**: User Story 2 complete. Invoices are extracted and validated automatically.

---

## Phase 5: User Story 3 — Purchase Order Reconciliation (Priority: P1)

**Goal**: Compare each extracted invoice line item against the PO using Supplier SKU. Generate discrepancy records for mismatches (exact match, no tolerance). Flag PO items missing from invoice.

**Independent Test**: Upload an invoice with known differences from a PO → discrepancy records are generated → invoice reaches Completed status.

### Implementation for User Story 3

- [X] T042 [US3] Implement `ReconciliationService` in `SPIP.Infrastructure/Services/ReconciliationService.cs` — New file. Implements `IReconciliationService`. Constructor takes `IInvoiceRepository`, `IPurchaseOrderRepository`, `IInvoiceProcessingLogRepository`, `IUnitOfWork`, `ILogger<ReconciliationService>`. Implement `ReconcileAsync(int invoiceId, CancellationToken ct)`:
  1. Load invoice with items: `await _invoiceRepo.GetWithDetailsByIdAsync(invoiceId)`.
  2. Load PO with items: `await _poRepo.GetWithItemsByIdAsync(invoice.PurchaseOrderId.Value)`. If PO is null, set `Status = NeedsReview`, log "Purchase Order not found", save, return.
  3. **Transition to Compared**: Update status. Add log. Save.
  4. Load PO items with their Products (need `Product.Sku` for matching): For each `PurchaseOrderItem`, eagerly load `Product`.
  5. **Compare invoice items against PO items**:
     - For each `InvoiceItem`:
       - Find matching PO item where `poItem.Product.Sku == invoiceItem.SupplierSku`.
       - If no match found: Create `Discrepancy` with `DiscrepancyType = MissingSku`, `FieldName = "SupplierSku"`, `ExpectedValue = "N/A"`, `ActualValue = invoiceItem.SupplierSku`, `InvoiceItemId = invoiceItem.Id`.
       - If match found:
         - If `invoiceItem.Quantity != poItem.Quantity`: Create `Discrepancy` with `DiscrepancyType = QuantityMismatch`, `FieldName = "Quantity"`, `ExpectedValue = poItem.Quantity.ToString()`, `ActualValue = invoiceItem.Quantity.ToString()`.
         - If `invoiceItem.UnitPrice != poItem.UnitPrice`: Create `Discrepancy` with `DiscrepancyType = UnitPriceMismatch`, `FieldName = "UnitPrice"`, `ExpectedValue = poItem.UnitPrice.ToString()`, `ActualValue = invoiceItem.UnitPrice.ToString()`.
  6. **Check for PO items missing from invoice** (FR-014: MissingFromInvoice):
     - For each `PurchaseOrderItem`, check if any `InvoiceItem` has `SupplierSku == poItem.Product.Sku`.
     - If no match: Create `Discrepancy` with `DiscrepancyType = MissingFromInvoice`, `FieldName = "SupplierSku"`, `ExpectedValue = poItem.Product.Sku`, `ActualValue = "N/A"`, `InvoiceItemId = null`.
  7. **Check invoice total vs sum of line items** (Amount Mismatch):
     - If `invoice.TotalAmount != invoice.Items.Sum(i => i.LineTotal)`: Create `Discrepancy` with `DiscrepancyType = AmountMismatch`, `FieldName = "TotalAmount"`, `ExpectedValue = sum.ToString()`, `ActualValue = invoice.TotalAmount.ToString()`.
  8. Add all discrepancies to `invoice.Discrepancies`.
  9. **Transition to Completed**: Update status. Add log with discrepancy count. Save.

**Checkpoint**: User Story 3 complete. Full reconciliation pipeline works end-to-end.

---

## Phase 6: User Story 4 — View Invoice Processing Status (Priority: P2)

**Goal**: Users query invoice details including status, extracted data, discrepancies, and processing history. Data ownership enforced (users see only their own invoices).

**Independent Test**: Upload and process an invoice → `GET api/invoices/{id}` returns full details including items, discrepancies, and logs. Another user gets 403.

### Implementation for User Story 4

- [X] T043 [US4] Implement `InvoiceService.GetByIdAsync` in `SPIP.Infrastructure/Services/InvoiceService.cs` — Add to the existing `InvoiceService` class:
  1. Get current user ID from `ICurrentUserService`.
  2. Load invoice with details: `await _invoiceRepo.GetWithDetailsByIdAsync(invoiceId)`. If null, return `Result<>.Failure("Invoice not found.")`.
  3. **Enforce data ownership (FR-024)**: If `invoice.UploadedByUserId != currentUserId` and the current user does NOT have the `Invoices.ViewAll` permission, return `Result<>.Failure("Access denied.")`. (Check permissions via claims or role check.)
  4. Map to `InvoiceDetailDto` using AutoMapper. For `LastError`, manually extract the most recent `ProcessingLog` with `EventType` containing "Error" or `ToStatus == Failed`.
  5. Return `Result<InvoiceDetailDto>.Success(dto)`.

- [X] T044 [US4] Add `GetById` endpoint to `InvoicesController` in `SPIP.API/Controllers/InvoicesController.cs`:
  ```csharp
  [HttpGet("{id:int}")]
  [Authorize(Policy = Permissions.Invoices.View)]
  public async Task<ActionResult<ApiResponse<InvoiceDetailDto>>> GetById(int id)
  {
      var result = await _invoiceService.GetByIdAsync(id);
      if (!result.Succeeded)
      {
          if (result.Error == "Access denied.")
              return Forbid();
          return NotFound(ApiResponse<InvoiceDetailDto>.FailureResponse(result.Error!));
      }
      return Ok(ApiResponse<InvoiceDetailDto>.SuccessResponse(result.Data!));
  }
  ```

**Checkpoint**: User Story 4 complete. Users can view invoice status and details with ownership enforcement.

---

## Phase 7: User Story 5 — Secure Invoice File Download (Priority: P2)

**Goal**: Authenticated users download the original uploaded invoice file through a secured endpoint. Data ownership enforced.

**Independent Test**: `GET api/invoices/{id}/download` returns the file with correct MIME type. Unauthenticated gets 401. Non-owner gets 403.

### Implementation for User Story 5

- [X] T045 [US5] Implement `InvoiceService.DownloadFileAsync` in `SPIP.Infrastructure/Services/InvoiceService.cs` — Add to the existing `InvoiceService`:
  1. Get current user ID.
  2. Load invoice with `UploadedFiles` included.
  3. Enforce data ownership (same as T043).
  4. Get the first `UploadedFile`. If none, return failure.
  5. Get file stream: `await _fileStorageService.GetFileAsync(uploadedFile.StoragePath)`.
  6. Return `Result<(Stream, string, string)>.Success((stream, uploadedFile.ContentType ?? "application/octet-stream", uploadedFile.OriginalFileName))`.

- [X] T046 [US5] Add `Download` endpoint to `InvoicesController` in `SPIP.API/Controllers/InvoicesController.cs`:
  ```csharp
  [HttpGet("{id:int}/download")]
  [Authorize(Policy = Permissions.Invoices.Download)]
  public async Task<IActionResult> Download(int id)
  {
      var result = await _invoiceService.DownloadFileAsync(id);
      if (!result.Succeeded)
      {
          if (result.Error == "Access denied.")
              return Forbid();
          return NotFound(ApiResponse<string>.FailureResponse(result.Error!));
      }
      var (stream, contentType, fileName) = result.Data!;
      return File(stream, contentType, fileName);
  }
  ```

**Checkpoint**: User Story 5 complete. Secure file download works with ownership checks.

---

## Phase 8: User Story 6 — Administrator Invoice Monitoring (Priority: P3)

**Goal**: Administrators list all invoices with status filtering and pagination. Non-admins get 403.

**Independent Test**: Admin calls `GET api/invoices?status=Failed` → returns only Failed invoices with pagination. Non-admin gets 403.

### Implementation for User Story 6

- [X] T047 [US6] Implement `InvoiceService.GetAllPagedAsync` in `SPIP.Infrastructure/Services/InvoiceService.cs` — Add to the existing `InvoiceService`:
  1. Call `await _invoiceRepo.GetPagedAsync(parameters)`.
  2. Map to `InvoiceListItemDto` using AutoMapper.
  3. For each item, populate `LastError` by querying the most recent `InvoiceProcessingLog` with `ToStatus == InvoiceStatus.Failed` for that invoice.
  4. Return `Result<PagedResult<InvoiceListItemDto>>.Success(pagedResult)`.

- [X] T048 [US6] Add admin list endpoint to `InvoicesController` in `SPIP.API/Controllers/InvoicesController.cs`:
  ```csharp
  [HttpGet]
  [Authorize(Policy = Permissions.Invoices.ViewAll)]
  public async Task<ActionResult<ApiResponse<PagedResult<InvoiceListItemDto>>>> GetAll([FromQuery] InvoiceListParameters parameters)
  {
      var result = await _invoiceService.GetAllPagedAsync(parameters);
      return result.Succeeded
          ? Ok(ApiResponse<PagedResult<InvoiceListItemDto>>.SuccessResponse(result.Data!))
          : BadRequest(ApiResponse<PagedResult<InvoiceListItemDto>>.FailureResponse(result.Error!));
  }
  ```

**Checkpoint**: User Story 6 complete. Administrators can monitor all invoices.

---

## Phase 9: Polish & Cross-Cutting Concerns

**Purpose**: Security hardening, logging completeness, and final validation

- [X] T050 [P] Seed Invoice permissions into the database — Add `Invoices.Upload`, `Invoices.View`, `Invoices.Download`, `Invoices.ViewAll` to the permission seed data in `SPIP.Infrastructure/Persistence/Seed/` or `SPIP.Domain/Entities/PermissionCatalog.cs` (follow the existing pattern for how permissions like `POImports.View` are seeded).

- [X] T051 [P] Add Swagger/OpenAPI documentation attributes to all `InvoicesController` endpoints — Add `[ProducesResponseType]` attributes for 200, 400, 401, 403, 404 as appropriate. Add `[Consumes("multipart/form-data")]` for the upload endpoint. Add XML doc comments describing each endpoint.

- [X] T052 Verify build compiles with zero warnings — Run `dotnet build` from the repository root and fix any warnings or errors. Ensure all `using` statements are correct and no unused imports exist.

- [ ] T053 Run quickstart.md validation scenarios — Execute the 6 validation scenarios described in `specs/001-invoice-processing-pipeline/quickstart.md` to verify end-to-end functionality. Document results.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies — can start immediately
- **Foundational (Phase 2)**: Depends on Setup completion — BLOCKS all user stories
- **User Story 1 (Phase 3)**: Depends on Phase 2
- **User Story 2 (Phase 4)**: Depends on Phase 3 (needs the Hangfire job and upload flow)
- **User Story 3 (Phase 5)**: Depends on Phase 4 (needs extracted data to reconcile)
- **User Story 4 (Phase 6)**: Depends on Phase 2 (can start in parallel with US1-US3 if needed, but richer with processed data)
- **User Story 5 (Phase 7)**: Depends on Phase 2 (can start in parallel with US1-US3)
- **User Story 6 (Phase 8)**: Depends on Phase 2 (can start in parallel)
- **Polish (Phase 9)**: Depends on all user stories being complete

### User Story Dependencies

- **US1 (Upload)**: After Phase 2 — no story dependencies
- **US2 (Extraction)**: After US1 — needs upload flow and Hangfire job
- **US3 (Reconciliation)**: After US2 — needs extracted data
- **US4 (View Status)**: After Phase 2 — can be done in parallel but richer after US1-US3
- **US5 (Download)**: After Phase 2 — can be done in parallel but needs uploaded files
- **US6 (Admin Monitor)**: After Phase 2 — can be done in parallel but needs invoice data

### Within Each User Story

- Models/entities before services
- Services before controllers/endpoints
- Core implementation before integration

### Parallel Opportunities

- **Phase 2**: All T003–T034 tasks marked [P] can run in parallel (they touch different files)
- **Phase 3**: T035 and T036 can run in parallel
- **Phase 6–8**: US4, US5, US6 can run in parallel with each other after US3 is done

---

## Parallel Example: Phase 2

```bash
# All these touch different files and can run simultaneously:
Task T003: Modify InvoiceStatus enum in SPIP.Domain/Enums/InvoiceStatus.cs
Task T004: Create DiscrepancyType enum in SPIP.Domain/Enums/DiscrepancyType.cs
Task T005: Modify Invoice entity in SPIP.Domain/Entities/Invoice.cs
Task T006: Modify InvoiceItem entity in SPIP.Domain/Entities/InvoiceItem.cs
Task T007: Modify Discrepancy entity in SPIP.Domain/Entities/Discrepancy.cs
Task T008: Modify UploadedFile entity in SPIP.Domain/Entities/UploadedFile.cs
Task T009: Create InvoiceProcessingLog entity in SPIP.Domain/Entities/InvoiceProcessingLog.cs
Task T010: Add Invoices permissions in SPIP.Domain/Constants/Permissions.cs
Task T011-T014: Create DTOs (all in different files)
Task T015-T021: Create interfaces (all in different files)
Task T022-T028: Create validators, mappers, EF configs (all in different files)
```

---

## Implementation Strategy

### MVP First (User Stories 1-3)

1. Complete Phase 1: Setup
2. Complete Phase 2: Foundational (CRITICAL — blocks all stories)
3. Complete Phase 3: User Story 1 — **STOP and VALIDATE**: Test upload works
4. Complete Phase 4: User Story 2 — **STOP and VALIDATE**: Test extraction works
5. Complete Phase 5: User Story 3 — **STOP and VALIDATE**: Test reconciliation works
6. Deploy/demo the core pipeline

### Incremental Delivery

1. Setup + Foundational → Foundation ready
2. US1 (Upload) → Test → Deploy
3. US2 (Extraction) → Test → Deploy
4. US3 (Reconciliation) → Test → Deploy (MVP complete!)
5. US4 (View Status) → US5 (Download) → US6 (Admin) → Polish → Full feature

---

## Notes

- [P] tasks = different files, no dependencies
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate story independently
- All code must follow the constitution: Clean Architecture, SOLID, async/await, FluentValidation, `Result<T>`, thin controllers, Serilog structured logging
- Use file-scoped namespaces, nullable reference types, record types for immutable DTOs
