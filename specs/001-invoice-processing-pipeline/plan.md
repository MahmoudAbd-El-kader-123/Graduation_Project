# Implementation Plan: Intelligent Invoice Processing Pipeline

**Branch**: `001-invoice-processing-pipeline` | **Date**: 2026-07-23 | **Spec**: [spec.md](spec.md)

**Input**: Feature specification from `specs/001-invoice-processing-pipeline/spec.md`

## Summary

Build the asynchronous invoice processing pipeline that enables authenticated users to upload invoice files linked to a Purchase Order, process them via Hangfire background jobs, extract structured data through an external AI service, validate the extracted data, persist invoice headers and line items, reconcile against the PO using Supplier SKU matching with exact comparison, generate discrepancy records, and provide query/download endpoints with data ownership enforcement.

## Technical Context

**Language/Version**: C# 12 / .NET 8 (SDK 8.0.404, `latestFeature` roll-forward)

**Primary Dependencies**: ASP.NET Core 8, Entity Framework Core, Hangfire 1.8.14, FluentValidation, AutoMapper, Serilog, Hangfire.SqlServer

**Storage**: SQL Server via EF Core. File storage on local filesystem (configurable directory, outside web root, GUID filenames).

**Testing**: Integration tests via `dotnet test`. Manual validation via Swagger/curl (see quickstart.md).

**Target Platform**: Windows/Linux server (ASP.NET Core Kestrel)

**Project Type**: Web API (Clean Architecture — SPIP.Domain / SPIP.Application / SPIP.Infrastructure / SPIP.API / SPIP.Shared)

**Performance Goals**: Upload response < 3s (SC-001), 95% pipeline completion < 60s (SC-002), admin list response < 10s (SC-008)

**Constraints**: 10MB max file size, 30s AI service timeout, 3 retry attempts, exact match pricing (no tolerance)

**Scale/Scope**: Single-tenant, standard procurement team usage (< 100 concurrent users)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| Principle | Status | Evidence |
|-----------|--------|----------|
| I. Clean Architecture | ✅ PASS | Entities in Domain, interfaces in Application, implementations in Infrastructure, thin controllers in API |
| II. SOLID | ✅ PASS | ISP: separate `IInvoiceService`, `IInvoiceProcessingService`, `IReconciliationService`. SRP: each service has one concern |
| III. .NET 8 Modern C# | ✅ PASS | File-scoped namespaces, record DTOs, pattern matching, nullable reference types |
| IV. Thin Controllers | ✅ PASS | Controller delegates to `IInvoiceService`, returns `ApiResponse<T>` |
| V. Dependency Injection | ✅ PASS | All services registered in `DependencyInjection` classes, constructor injection only |
| VI. Repository & UoW | ✅ PASS | `IInvoiceRepository`, `IInvoiceProcessingLogRepository` extend `IGenericRepository<T>`. UoW coordinates saves |
| VII. Async-First I/O | ✅ PASS | All service/repo methods are async with `CancellationToken` |
| VIII. Clean Code & Naming | ✅ PASS | Descriptive names, `Async` suffix, `I` prefix for interfaces, `Dto`/`Request` suffixes |
| IX. Input Validation | ✅ PASS | `UploadInvoiceRequestValidator` via FluentValidation. File validation (extension, MIME, magic number, size) |
| X. Exception Handling & Logging | ✅ PASS | `Result<T>` for expected failures, structured Serilog logging, global exception middleware |

**Post-Phase 1 Re-check**: All gates still pass. No complexity violations.

## Project Structure

### Documentation (this feature)

```text
specs/001-invoice-processing-pipeline/
├── plan.md              # This file
├── spec.md              # Feature specification
├── research.md          # Phase 0 research decisions
├── data-model.md        # Phase 1 data model
├── quickstart.md        # Phase 1 validation guide
├── contracts/
│   └── api-contracts.md # Phase 1 API contracts
├── checklists/
│   └── requirements.md  # Spec quality checklist
└── tasks.md             # Phase 2 output (created by /speckit-tasks)
```

### Source Code (repository root)

```text
SPIP.Domain/
├── Entities/
│   ├── Invoice.cs                    # MODIFY: add VendorName, Currency, Subtotal, Vat, UploadedByUserId
│   ├── InvoiceItem.cs                # MODIFY: add SupplierSku
│   ├── Discrepancy.cs                # MODIFY: add DiscrepancyType, InvoiceItemId
│   ├── UploadedFile.cs               # MODIFY: add StoredFileName, OriginalFileName
│   └── InvoiceProcessingLog.cs       # NEW
├── Enums/
│   ├── InvoiceStatus.cs              # MODIFY: replace with pipeline statuses
│   └── DiscrepancyType.cs            # NEW
└── Constants/
    └── Permissions.cs                # MODIFY: add Invoices permission group

SPIP.Application/
├── Interfaces/
│   ├── Services/
│   │   ├── IInvoiceService.cs        # NEW: upload, get detail, download, list
│   │   ├── IInvoiceProcessingService.cs  # NEW: orchestrate pipeline
│   │   └── IReconciliationService.cs     # NEW: PO comparison
│   ├── Repositories/
│   │   ├── IInvoiceRepository.cs     # NEW: extends IGenericRepository<Invoice>
│   │   └── IInvoiceProcessingLogRepository.cs  # NEW
│   ├── AI/
│   │   └── IAIExtractionService.cs   # MODIFY: return typed DTO
│   └── Storage/
│       └── IFileStorageService.cs    # MODIFY: add GUID rename, content type
├── DTOs/
│   ├── Invoice/
│   │   ├── InvoiceDto.cs             # MODIFY
│   │   ├── InvoiceDetailDto.cs       # NEW
│   │   ├── InvoiceItemDto.cs         # NEW
│   │   ├── InvoiceUploadResultDto.cs # NEW
│   │   ├── InvoiceListItemDto.cs     # NEW
│   │   ├── InvoiceListParameters.cs  # NEW
│   │   ├── DiscrepancyDto.cs         # NEW
│   │   ├── InvoiceProcessingLogDto.cs # NEW
│   │   └── UploadInvoiceRequest.cs   # NEW
│   └── AI/
│       ├── AIExtractionResponseDto.cs # NEW (typed AI response)
│       └── AIExtractionItemDto.cs    # NEW
├── Validators/
│   └── UploadInvoiceRequestValidator.cs  # NEW
├── Mapping/
│   └── InvoiceMappingProfile.cs      # NEW: AutoMapper profile
└── Services/
    └── (Application-layer services if needed — most logic in Infrastructure)

SPIP.Infrastructure/
├── Repositories/
│   ├── InvoiceRepository.cs          # NEW
│   ├── InvoiceProcessingLogRepository.cs  # NEW
│   └── UnitOfWork.cs                 # MODIFY: add invoice repos
├── Services/
│   ├── InvoiceService.cs             # NEW
│   ├── InvoiceProcessingService.cs   # NEW: pipeline orchestration
│   ├── ReconciliationService.cs      # NEW
│   ├── LocalFileStorageService.cs    # NEW
│   └── AIExtractionService.cs        # NEW (HttpClient-based)
├── BackgroundJobs/
│   └── InvoiceProcessingJob.cs       # NEW: Hangfire job
├── Persistence/
│   ├── Configurations/
│   │   ├── InvoiceConfiguration.cs           # MODIFY
│   │   ├── InvoiceItemConfiguration.cs       # MODIFY
│   │   ├── DiscrepancyConfiguration.cs       # NEW
│   │   ├── UploadedFileConfiguration.cs      # NEW
│   │   └── InvoiceProcessingLogConfiguration.cs  # NEW
│   ├── Context/
│   │   └── ApplicationDbContext.cs           # MODIFY: add DbSets
│   └── Migrations/
│       └── <timestamp>_AddInvoicePipeline.cs # NEW (auto-generated)
└── DependencyInjection/
    └── DependencyInjection.cs        # MODIFY: register new services/repos

SPIP.API/
└── Controllers/
    └── InvoicesController.cs         # NEW: upload, get, download, list
```

**Structure Decision**: Follows existing Clean Architecture project layout. All new code integrates into established directories. No new projects needed.

## Complexity Tracking

> No constitution violations. No complexity justifications needed.
