# Feature Specification: k6 Performance Testing Suite

**Feature Branch**: `002-k6-performance-testing`

**Created**: 2026-08-14

**Status**: Draft

**Input**: User description: "Create a performance testing feature for the SPIP backend using k6 to measure concurrent-user capacity with realistic workflows against non-AI HTTP APIs."

## Clarifications

### Session 2026-08-15

- Q: What default think-time range should virtual users use between actions? → A: 1–5 seconds (random) — standard web app user pacing

## User Scenarios & Testing *(mandatory)*

### User Story 1 - Sustained Load Testing (Priority: P1)

A performance engineer runs a sustained-load test to determine whether the SPIP backend can support a defined number of concurrent users (e.g., 100, 500, or 1,000) for an extended period (e.g., 30 minutes to 1 hour) while maintaining acceptable response times, throughput, and error rates.

**Why this priority**: This is the primary question the test suite must answer — whether the backend can serve a target number of users reliably over time. Without this capability, the team cannot validate production readiness or plan capacity.

**Independent Test**: Can be fully tested by executing a single k6 load-test run with configurable virtual users and duration, then reviewing the summary report for p50/p95/p99 response times, throughput, and error rate.

**Acceptance Scenarios**:

1. **Given** a running SPIP backend with test data, **When** the performance engineer starts a sustained-load test with 100 concurrent virtual users for 30 minutes, **Then** k6 produces a summary report showing total requests, throughput, p50/p95/p99 response times, and HTTP error rate, and the test enforces configurable performance thresholds.
2. **Given** a sustained-load test configuration, **When** the engineer changes the virtual user count to 500 or 1,000 and the duration to 60 minutes, **Then** the test runs with the updated parameters without modifying test scripts.
3. **Given** a sustained-load test in progress, **When** the test starts, **Then** virtual users are ramped up gradually (not all started simultaneously), sustain the target load for the configured duration, and ramp down gradually at the end.

---

### User Story 2 - Capacity / Stress Discovery (Priority: P2)

A performance engineer runs an optional capacity test to identify the maximum number of concurrent users the SPIP backend can support before response times, error rates, or throughput degrade beyond acceptable levels.

**Why this priority**: Knowing the breaking point helps the team plan infrastructure scaling and set alert thresholds. This builds on the sustained-load test but focuses on finding limits rather than validating known targets.

**Independent Test**: Can be tested by executing a capacity scenario that gradually increases virtual users in steps (e.g., 100 → 250 → 500 → 750 → 1,000 → 1,250 → 1,500) and reviewing the results to identify the degradation point.

**Acceptance Scenarios**:

1. **Given** a running SPIP backend, **When** the capacity test runs, **Then** virtual users increase in configurable steps with each step sustained long enough to collect meaningful metrics, and the results clearly show at which user level response times or error rates began to degrade.
2. **Given** a capacity test in progress, **When** the backend becomes severely unstable (e.g., error rates exceed a high threshold), **Then** the test avoids unnecessarily increasing the load further.

---

### User Story 3 - Realistic User Workflow Simulation (Priority: P1)

Virtual users in all test scenarios simulate realistic application usage patterns by performing sequences of actions that mirror how a real SPIP user would interact with the system — authenticate, browse data, perform operations, and pause between actions.

**Why this priority**: Unrealistic test behavior (e.g., sending requests as fast as possible without pacing) produces misleading results. Realistic simulation is essential for the sustained-load and capacity test results to be meaningful.

**Independent Test**: Can be tested by executing any test scenario and verifying that virtual user iterations include authentication, multiple API calls across different endpoints, and think-time pauses between actions.

**Acceptance Scenarios**:

1. **Given** a virtual user executing a test iteration, **When** the iteration runs, **Then** the user authenticates once, accesses multiple SPIP backend endpoints in a realistic sequence (e.g., browse vendors, view products, view purchase orders, browse roles), and pauses for a configurable think-time between actions.
2. **Given** test credentials configured via environment variables, **When** a virtual user authenticates, **Then** the resulting JWT token is reused for all subsequent requests within that user session.
3. **Given** multiple concurrent virtual users, **When** the test runs, **Then** virtual users operate independently without causing test failures due to shared or conflicting test data.

---

### User Story 4 - Environment-Portable Test Execution (Priority: P2)

The same k6 test scripts can be executed against any SPIP backend environment (local development, staging, dedicated test) simply by changing the `BASE_URL` environment variable. No script modifications are needed.

**Why this priority**: Reusability across environments eliminates the need to maintain separate test scripts and reduces the risk of environment-specific bugs in test code.

**Independent Test**: Can be tested by running the same test script against two different backend URLs and confirming both runs execute successfully.

**Acceptance Scenarios**:

1. **Given** k6 test scripts, **When** the engineer runs `k6 run -e BASE_URL=https://localhost:7001 test.js`, **Then** all requests target the specified URL, and no hardcoded hostnames exist in any test script.
2. **Given** k6 test scripts, **When** the engineer changes only the `BASE_URL` value, **Then** the tests run identically against the new target without any script changes.

---

### Edge Cases

- What happens when the backend is completely unreachable at the configured `BASE_URL`? The test should fail fast with a clear error rather than running for the full duration against a dead host.
- What happens when test credentials are invalid or missing? The test should report an authentication failure early in the run and halt, rather than counting auth errors as performance failures.
- What happens when the backend returns HTTP 429 (rate limited)? Rate-limited responses should be distinguishable from actual backend failures in the test results.
- What happens when individual endpoints return errors while others succeed? Per-endpoint metrics should make it possible to isolate which endpoints are degrading.
- What happens when the test data (e.g., vendor IDs, product IDs) does not exist in the target environment? Tests should fail with a clear data-configuration error, not produce misleading performance data.

## Requirements *(mandatory)*

### Functional Requirements

- **FR-001**: The testing suite MUST provide a sustained-load test scenario that runs a configurable number of concurrent virtual users for a configurable duration, with gradual ramp-up and ramp-down phases.
- **FR-002**: The testing suite MUST provide a capacity/stress test scenario that incrementally increases concurrent virtual users in configurable steps to identify the backend's degradation point.
- **FR-003**: Virtual users MUST simulate realistic application workflows by performing sequences of API calls with think-time pauses between actions. The default think-time MUST be a random duration between 1 and 5 seconds per action, configurable per test run.
- **FR-004**: Virtual users MUST authenticate using test credentials provided via environment variables and reuse the resulting JWT token for all subsequent requests in the session.
- **FR-005**: All test scripts MUST use a single `BASE_URL` environment variable as the sole source for the target backend host, with no hardcoded hostnames.
- **FR-006**: The testing suite MUST collect and report: total request count, requests per second, HTTP error rate, average response time, p50/p90/p95/p99 response times, maximum response time, active virtual user count, and iteration duration.
- **FR-007**: The testing suite MUST enforce configurable k6 thresholds, with initial baselines of HTTP error rate < 1%, p95 < 1 second, and p99 < 2 seconds for normal operations.
- **FR-008**: The testing suite MUST completely exclude all AI-related endpoints: `AIChatController` endpoints, invoice upload (which triggers AI extraction via background job), and any endpoint that directly or indirectly invokes an AI service.
- **FR-009**: The testing suite MUST target only existing SPIP backend endpoints discovered from the codebase. Specifically, the following endpoints are in scope:
  - **Auth**: `POST api/auth/login`, `POST api/auth/refresh-token`
  - **Vendors**: `GET api/vendors`, `GET api/vendors/{id}`, `POST api/vendors`, `PUT api/vendors/{id}`, `DELETE api/vendors/{id}`, `PUT api/vendors/{id}/toggle-approval`
  - **Products**: `GET api/products`, `GET api/products/{id}`, `POST api/products`, `PUT api/products/{id}`, `DELETE api/products/{id}`
  - **Purchase Orders**: `GET api/purchase-orders`, `GET api/purchase-orders/{id}`, `DELETE api/purchase-orders/{id}`
  - **Vendor Mappings**: `GET api/vendor-mappings/vendor/{vendorId}`, `POST api/vendor-mappings`, `DELETE api/vendor-mappings/{id}`
  - **Users**: `GET api/users`, `GET api/users/{id}`
  - **Roles**: `GET api/roles`, `GET api/roles/{id}`
  - **Permissions**: `GET api/permissions`
  - **Audit Logs**: `GET api/audit-logs`, `GET api/audit-logs/{id}`
- **FR-010**: The testing suite MUST be organized in a separate directory structure (`tests/Performance/k6/`) with reusable helpers, configuration, scenarios, and test data separated into distinct modules.
- **FR-011**: Test credentials MUST NOT be hardcoded in test scripts; they MUST be supplied through environment variables or secure test configuration files.
- **FR-012**: The testing suite MUST NOT modify production backend behavior, create fake API endpoints, or use production credentials or data.
- **FR-013**: The testing suite MUST include documentation (README) that explains how to install k6, configure test parameters, execute each scenario, and interpret results.
- **FR-014**: The testing suite MUST provide per-endpoint metric tagging so that individual endpoint performance can be analyzed separately in the results.

### Excluded Endpoints

The following endpoints are explicitly excluded from performance testing:

- **AIChatController**: All endpoints (AI chat functionality — directly invokes AI services)
- **InvoicesController**: All endpoints — `POST api/invoices/upload` triggers AI extraction via background job (`InvoiceProcessingJob` → `InvoiceProcessingService` → `IAIExtractionService`); read endpoints (`GET api/invoices`, `GET api/invoices/{id}`, `GET api/invoices/{id}/reconciliation`, `GET api/invoices/{id}/download`) are excluded per user request
- **DashboardController**: `GET api/dashboard/stats` — excluded per user request
- **ReconciliationReportsController**: `GET api/reconciliation-reports`, `GET api/reconciliation-reports/{invoiceId}` — excluded per user request (tightly coupled to invoice data)

## Success Criteria *(mandatory)*

### Measurable Outcomes

- **SC-001**: The performance testing suite can execute repeatable load tests against the SPIP backend with results consistent across multiple runs under identical conditions (variance in key metrics within 10% across runs).
- **SC-002**: The sustained-load test can run 1,000 concurrent virtual users for 30 minutes and produce a complete metrics report without test framework errors.
- **SC-003**: The capacity test can identify the point where backend performance degrades by producing a clear comparison of metrics at each user-count step.
- **SC-004**: p50, p95, p99 response times, throughput, and error rates are reported for every test run and broken down by individual endpoint.
- **SC-005**: The same test scripts produce valid results against at least two different backend environments by changing only the `BASE_URL` variable.
- **SC-006**: AI-related endpoints are verifiably absent from all test scripts and do not appear in any test run's request logs.
- **SC-007**: Virtual users execute realistic multi-step workflows with measurable think-time between actions (not zero-delay request flooding).
- **SC-008**: Performance thresholds are enforced during test execution, and the test run produces a clear pass/fail outcome based on the configured thresholds.
- **SC-009**: The suite can be set up and executed by a team member within 15 minutes following the provided documentation.

## Assumptions

- The SPIP backend is accessible at the configured `BASE_URL` with a valid TLS certificate or over HTTP for local development.
- A dedicated test or staging environment is available for running high-load tests. The team will not run sustained load tests against production.
- Test user accounts with appropriate permissions (admin-level access to all non-AI endpoints) are pre-created in the target environment before test execution.
- k6 is installed on the test runner machine (the documentation will include installation instructions).
- The SPIP backend has sufficient test data (vendors, products, purchase orders, users, roles) pre-seeded in the target environment for realistic read operations.
- The backend's rate limiter (applied only to `POST api/auth/login` via `AuthPolicy`) may affect login throughput during test warm-up; the test will authenticate virtual users with appropriate pacing to avoid triggering rate limits.
- The `POST api/auth/register` endpoint is excluded from normal test workflows to avoid creating test user accounts during load tests; authentication relies on pre-existing test credentials.
- Write operations (create, update, delete) will be used sparingly and with unique test data to avoid conflicts between virtual users.
- The team is responsible for monitoring backend infrastructure (CPU, memory, database metrics) during test execution using their existing monitoring tools; the test suite will document what to monitor but will not implement server-side metric collection.
