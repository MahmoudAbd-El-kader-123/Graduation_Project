<!-- Sync Impact Report
  Version change: 0.0.0 → 1.0.0 (initial ratification)
  Modified principles: N/A (first version)
  Added sections:
    - Core Principles (10 principles: Clean Architecture, SOLID, .NET 8 Modern C#,
      Thin Controllers, Dependency Injection, Repository & Unit of Work,
      Async-First I/O, Clean Code & Naming, Input Validation, Exception Handling & Logging)
    - Technology & Infrastructure Constraints
    - Development Workflow & Quality Gates
    - Governance
  Removed sections: N/A
  Templates requiring updates:
    - plan-template.md ✅ (Constitution Check section is generic; aligned)
    - spec-template.md ✅ (Requirements/functional sections are generic; aligned)
    - tasks-template.md ✅ (Phase structure is generic; aligned)
  Follow-up TODOs: None
-->

# SPIP Constitution

## Core Principles

### I. Clean Architecture

All code MUST follow the established Clean Architecture layer separation:

- **SPIP.Domain** — Pure domain entities, enums, constants, and interfaces. MUST NOT reference any other project.
- **SPIP.Application** — Service interfaces, DTOs, validators, mapping profiles, and application-level services. MUST depend only on `SPIP.Domain` and `SPIP.Shared`.
- **SPIP.Infrastructure** — EF Core persistence, Identity, repositories, external integrations, and concrete service implementations. MUST depend on `SPIP.Domain`, `SPIP.Application`, and `SPIP.Shared`.
- **SPIP.API** — Controllers, middleware, filters, Swagger configuration, and DI wiring. MUST depend on `SPIP.Application` and `SPIP.Infrastructure`.
- **SPIP.Shared** — Cross-cutting types (`Result<T>`, `ApiResponse<T>`, `PagedResult<T>`, utilities). MUST NOT depend on any project-specific layer.

Business logic MUST reside in the Application layer. Infrastructure concerns (database access, file I/O, email, AI integrations) MUST NOT leak into Application or Domain layers.

**Rationale**: Layer isolation enables independent testing, enforces separation of concerns, and allows infrastructure to be swapped without touching business rules.

### II. SOLID Principles

All classes and modules MUST adhere to the SOLID principles:

- **Single Responsibility**: Each class MUST have one reason to change. Services, repositories, validators, and controllers MUST each focus on a single concern.
- **Open/Closed**: Classes MUST be open for extension but closed for modification. Use interfaces and abstractions to allow behavior extension without altering existing code.
- **Liskov Substitution**: Derived types MUST be substitutable for their base types without altering program correctness.
- **Interface Segregation**: Interfaces MUST be focused and cohesive. Clients MUST NOT be forced to depend on methods they do not use. Prefer multiple small interfaces over one large interface.
- **Dependency Inversion**: High-level modules MUST NOT depend on low-level modules. Both MUST depend on abstractions defined in `SPIP.Application` or `SPIP.Domain`.

**Rationale**: SOLID principles produce code that is maintainable, testable, and resilient to change.

### III. .NET 8 and Modern C# Features

All code MUST target .NET 8 (SDK 8.0.404+) and leverage modern C# language features where they improve clarity and safety:

- Use file-scoped namespaces.
- Use primary constructors where appropriate.
- Use `required` properties for mandatory initialization.
- Use pattern matching (`is`, `switch` expressions) instead of verbose conditionals.
- Use `record` types for immutable DTOs and value objects where applicable.
- Use collection expressions and LINQ where they improve readability.
- Use nullable reference types (`#nullable enable`) and handle nullability explicitly.

Code MUST NOT introduce features from preview or unsupported .NET versions.

**Rationale**: Modern language features reduce boilerplate, improve type safety, and align with the .NET ecosystem's direction.

### IV. Thin Controllers

Controllers MUST be thin orchestrators that delegate to Application-layer services:

- Controllers MUST NOT contain business logic, data access, or complex conditionals.
- Controllers MUST accept a request, call the appropriate service method, and return the result wrapped in `ApiResponse<T>` or the appropriate HTTP response.
- All request validation MUST occur via FluentValidation validators, not inline in controllers.
- Controllers MUST use `[Authorize]` attributes and policy-based authorization, not manual token inspection.

**Rationale**: Thin controllers keep the presentation layer focused on HTTP concerns and make business logic independently testable.

### V. Dependency Injection

All dependencies MUST be injected through constructor injection using the built-in ASP.NET Core DI container:

- Services MUST be registered in dedicated `DependencyInjection` classes within their respective projects (`SPIP.Application/DependencyInjection`, `SPIP.Infrastructure/DependencyInjection`).
- Service lifetimes MUST be chosen deliberately: `Scoped` for request-bound services (repositories, UoW, DbContext), `Singleton` for stateless utilities, `Transient` only when a new instance per resolution is required.
- Service locator anti-pattern (resolving from `IServiceProvider` directly) MUST NOT be used except in factory scenarios with explicit justification.
- All service contracts MUST be defined as interfaces in `SPIP.Application/Interfaces`.

**Rationale**: Proper DI ensures loose coupling, testability, and clear ownership of component lifetimes.

### VI. Repository Pattern and Unit of Work

All data access MUST go through the established Repository and Unit of Work patterns:

- `IGenericRepository<T>` provides standard CRUD operations. Entity-specific repositories (e.g., `IVendorRepository`, `IPurchaseOrderRepository`) extend it with domain-specific queries.
- `IUnitOfWork` coordinates transactional commits across multiple repositories.
- Repository interfaces MUST be defined in `SPIP.Application/Interfaces/Repositories`. Concrete implementations MUST reside in `SPIP.Infrastructure/Repositories`.
- Direct `DbContext` usage outside of repository implementations MUST NOT occur in Application or API layers.
- New entities MUST have a corresponding repository interface and implementation following the existing naming conventions.

**Rationale**: The Repository and UoW patterns abstract persistence, enable unit testing with mocks, and centralize query logic.

### VII. Async-First I/O

All I/O-bound operations MUST use `async/await`:

- Database queries, file operations, HTTP calls, and email sending MUST be asynchronous.
- Async methods MUST use the `Async` suffix (e.g., `GetByIdAsync`, `SaveChangesAsync`).
- `Task.Result`, `Task.Wait()`, and `.GetAwaiter().GetResult()` MUST NOT be used — they risk deadlocks.
- `ConfigureAwait(false)` SHOULD be used in library/infrastructure code where no synchronization context is needed.
- `CancellationToken` SHOULD be accepted and propagated in service and repository method signatures.

**Rationale**: Async I/O prevents thread starvation under load, improves scalability, and is the standard pattern in ASP.NET Core.

### VIII. Clean Code and Naming

All code MUST follow clean code practices with meaningful, consistent naming:

- Classes, methods, properties, and interfaces MUST use descriptive names that convey intent without requiring comments.
- Interfaces MUST use the `I` prefix (e.g., `IVendorService`, `IGenericRepository<T>`).
- DTOs MUST use descriptive suffixes: `Request`, `Response`, `Dto` as established in the project.
- Methods MUST be focused on a single responsibility and kept short enough to understand at a glance.
- Duplicated code MUST be extracted into shared methods, base classes, or utilities in `SPIP.Shared`.
- Magic numbers and strings MUST be replaced with named constants defined in `Constants` directories.
- Code MUST be self-documenting; comments SHOULD only explain *why*, not *what*.

**Rationale**: Clean naming and focused methods reduce cognitive load, accelerate onboarding, and minimize bugs.

### IX. Input Validation

All external inputs MUST be validated before reaching business logic:

- FluentValidation MUST be used for request DTO validation. Validators MUST reside in `SPIP.Application/Validators`.
- Validators MUST follow the naming convention `{RequestDto}Validator` (e.g., `LoginRequestValidator`, `RegisterRequestValidator`).
- Validation MUST cover required fields, format constraints, length limits, and business rules that can be checked without database access.
- Validation errors MUST return structured error responses via the standard `ApiResponse<T>` pattern with appropriate HTTP 400 status codes.
- Domain-level invariants MUST be enforced within entity constructors or domain methods, not only at the API boundary.

**Rationale**: Early, consistent validation prevents invalid data from propagating through the system and provides clear error feedback to API consumers.

### X. Exception Handling and Logging

All code MUST implement proper exception handling and structured logging:

- Global exception handling middleware MUST catch unhandled exceptions and return consistent `ApiResponse<T>` error responses.
- Application-specific exceptions MUST be defined in `SPIP.Application/Exceptions` and thrown from service methods for known error conditions.
- Exceptions MUST NOT be used for flow control. Use `Result<T>` for expected failure paths.
- Serilog MUST be used for all logging. Log messages MUST use structured logging with message templates (not string interpolation).
- Log levels MUST be used appropriately: `Information` for significant events, `Warning` for recoverable issues, `Error` for failures requiring attention, `Debug`/`Verbose` for development diagnostics.
- Sensitive data (passwords, tokens, PII) MUST NOT appear in log output.

**Rationale**: Consistent exception handling and structured logging are essential for production diagnostics, incident response, and system reliability.

## Technology and Infrastructure Constraints

- **Runtime**: .NET 8 (C# 12). The `global.json` pins SDK version `8.0.404` with `latestFeature` roll-forward.
- **Database**: SQL Server via Entity Framework Core. Migrations MUST reside in `SPIP.Infrastructure/Migrations`.
- **Identity**: ASP.NET Core Identity with `ApplicationUser` and `IdentityRole<Guid>`. JWT authentication with refresh token support.
- **Mapping**: AutoMapper for entity-to-DTO transformations. Mapping profiles MUST reside in `SPIP.Application/Mapping`.
- **Validation**: FluentValidation registered via DI. Validators MUST reside in `SPIP.Application/Validators`.
- **Logging**: Serilog with structured logging and appropriate sinks.
- **Excel**: ClosedXML for Excel import/parsing operations.
- **Rate Limiting**: Built-in ASP.NET Core `RateLimiter`.
- **New Libraries**: MUST NOT be introduced without explicit justification and team approval. Prefer built-in .NET capabilities and existing dependencies.

## Development Workflow and Quality Gates

- All code MUST compile without warnings (treat warnings as errors in CI).
- All public API endpoints MUST be documented via Swagger/OpenAPI attributes.
- All new services MUST have corresponding interface definitions before implementation.
- All new repository methods MUST follow the established async patterns with `CancellationToken` support.
- Code MUST respect the existing project structure and naming conventions. New files MUST be placed in the appropriate project and folder.
- Every pull request MUST demonstrate that existing functionality is not broken.
- Generated code MUST be production-ready — no TODO placeholders, no sample/demo patterns, no hardcoded secrets.

## Governance

- This constitution supersedes all ad-hoc coding practices for the SPIP backend.
- Amendments require: (1) documented rationale, (2) team review, (3) version increment following semantic versioning, and (4) a migration plan for any breaking changes.
- Semantic versioning for this constitution: MAJOR for principle removals or redefinitions, MINOR for new principles or material expansions, PATCH for clarifications and wording fixes.
- All code reviews MUST verify compliance with these principles. Non-compliance MUST be flagged and resolved before merge.
- Complexity beyond what these principles prescribe MUST be justified in writing (e.g., in the plan's Complexity Tracking table).

**Version**: 1.0.0 | **Ratified**: 2026-07-22 | **Last Amended**: 2026-07-22
