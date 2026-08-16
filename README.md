# SPIP - Smart Procurement Intelligence Platform (Backend)

Welcome to the backend API for SPIP. This repository is built using **.NET 8 Clean Architecture** and provides the RESTful APIs required for the Angular frontend.

## 🚀 For the Frontend (Angular) Team

If you are just consuming the APIs to build the UI, you don't need to dig into the backend code. Just run the project to get access to the Swagger documentation.

### 1. Running the API
1. Open `SPIP.sln` in **Visual Studio 2022**.
2. Press **F5** or click the green "Start" button (Ensure `SPIP.API` is the startup project).
3. The API will start, and your browser will open to the Swagger UI page automatically.

### 2. API Documentation & Testing
- **Swagger URL**: `http://localhost:5236/swagger`
- You can see all available endpoints, required payloads, and test them directly from this page.

### 3. Authentication (Important!)
The APIs are secured using JWT (JSON Web Tokens). To access secured endpoints, you must first log in. 

On startup, the system automatically seeds a default Admin account:
- **Email**: `admin@spip.com`
- **Password**: `Admin@123`
- **Role**: `Admin`

**How to authenticate in Swagger:**
1. Call the `POST /api/auth/login` endpoint using the credentials above.
2. Copy the `data.token` string from the response.
3. Scroll to the top of the Swagger page, click the green **Authorize** button.
4. Type `Bearer <paste-your-token-here>` and click Authorize.

---

## 🛠️ For Backend Developers

### Architecture
- **SPIP.API** — Presentation layer (Controllers, Middleware, Swagger, JWT)
- **SPIP.Application** — Business layer (Services, DTOs, Interfaces, Validators)
- **SPIP.Domain** — Pure entities/enums, no external references
- **SPIP.Infrastructure** — EF Core, Identity, JWT, Repositories, Unit of Work
- **SPIP.Shared** — Result<T>, PagedResult<T>, ApiResponse<T>, utilities

### Database Setup
1. Update the `DefaultConnection` string in `SPIP.API/appsettings.json` (or `appsettings.Development.json`) to point to your local SQL Server instance.
2. Open the Package Manager Console (Tools > NuGet Package Manager > Package Manager Console).
3. Set "Default project" to `SPIP.Infrastructure`.
4. Run `Update-Database -StartupProject SPIP.API` to apply the migrations to your local SQL Server.

### Key Features
- **Smart Purchase Order Import:** High-speed Excel parsing (`ClosedXML`) with strict `SkuSupplier` mapping and dynamic VAT deduction algorithms.
- **Secure Permission Catalog:** Database-driven integer-based permission verification ensuring robust Role-Based Access Control (RBAC).
- **Vendor Mapping System:** Flexible system mapping vendor-specific Excel columns to internal domain fields.

### Sprint 3 AI Pipeline Test

Use the [manual test guide](docs/sprint3-ai-manual-test.md) and its
[idempotent SQL fixture](scripts/sql/seed-sprint3-ai-demo.sql) to recreate the
verified nine-item invoice reconciliation scenario.

The Angular PO workflow, column-mapping contract, automatic product creation,
and import response are documented in the
[PO import frontend guide](docs/po-import-frontend-flow.md).

The Angular invoice upload, background-processing, reconciliation,
discrepancy, and download contract is documented in the
[invoice frontend guide](docs/invoice-frontend-flow.md).
