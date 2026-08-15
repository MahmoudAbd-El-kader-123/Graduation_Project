# Tasks: k6 Performance Testing Suite

**Input**: Design documents from `/specs/002-k6-performance-testing/`

**Prerequisites**: plan.md âœ…, spec.md âœ…, research.md âœ…, data-model.md âœ…, contracts/ âœ…, quickstart.md âœ…

**Tests**: Not requested in the feature specification â€” test tasks are omitted.

**Organization**: Tasks are grouped by user story to enable independent implementation and testing of each story.

**Design Decision â€” Read-Heavy Workload**: The primary sustained-load and capacity test workflows are **read-only**. All write operations (POST, PUT, DELETE) are excluded from the primary `userJourney()` workflow. The helper modules (`http-wrapper.js`) retain `post`, `put`, and `del` functions for future dedicated write-performance tests, but they are **not called** by any primary scenario. Virtual users authenticate **once per VU** (not per iteration) and reuse the JWT token across all iterations, re-authenticating only on 401/token expiry.

## Format: `[ID] [P?] [Story] Description`

- **[P]**: Can run in parallel (different files, no dependencies)
- **[Story]**: Which user story this task belongs to (e.g., US1, US2, US3, US4)
- Include exact file paths in descriptions

## Path Conventions

All files reside under `tests/Performance/k6/` at the repository root.

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: Create the project directory structure, environment configuration template, and test data file. These are prerequisites for all subsequent phases.

- [X] T001 Create the complete directory structure for the k6 test suite at `tests/Performance/k6/`

  **Details**: Create the following empty directories if they do not already exist:
  ```
  tests/Performance/k6/config/
  tests/Performance/k6/helpers/
  tests/Performance/k6/scenarios/
  tests/Performance/k6/workflows/
  tests/Performance/k6/data/
  ```

- [X] T002 [P] Create the environment variable template at `tests/Performance/k6/.env.example`

  **Details**: Create a `.env.example` file with the following content. This file documents all environment variables consumed by the k6 scripts. None of these variables should have actual secret values â€” use placeholder examples only:
  ```bash
  # ============================================================
  # k6 Performance Test Suite â€” Environment Variables
  # ============================================================
  # Copy this file to .env and fill in values for your target
  # environment before running tests.
  # ============================================================

  # Required â€” Target backend base URL (no trailing slash)
  BASE_URL=https://localhost:7001

  # Required â€” Test user credentials (must exist in target environment)
  TEST_USER_EMAIL=perftest@example.com
  TEST_USER_PASSWORD=YourTestPassword123!

  # Optional â€” Think-time range (seconds) between virtual user actions
  # Default: 1-5 seconds random uniform distribution
  THINK_TIME_MIN=1
  THINK_TIME_MAX=5

  # Optional â€” Sustained-load scenario overrides
  TARGET_VUS=100
  RAMP_UP_DURATION=2m
  SUSTAINED_DURATION=30m
  RAMP_DOWN_DURATION=1m

  # Optional â€” Capacity scenario overrides
  CAPACITY_STEP_DURATION=3m
  CAPACITY_STEPS=100,250,500,750,1000,1250,1500
  ```

- [X] T003 [P] Create the test data configuration file at `tests/Performance/k6/data/test-config.json`

  **Details**: Create a JSON file containing pre-seeded entity IDs that must exist in the target environment. k6 scripts will import this file using `open()` and `JSON.parse()` to select random entities for **read-only** operations. These IDs point to data that is browsed but never modified or deleted during tests:
  ```json
  {
    "_comment": "Entity IDs must exist in the target environment. Update these values to match your test/staging data. These are READ-ONLY â€” the test suite never creates, modifies, or deletes these entities.",
    "vendorIds": [1, 2, 3],
    "productIds": [1, 2, 3],
    "purchaseOrderIds": [1, 2, 3],
    "auditLogIds": [1],
    "userIds": [1, 2],
    "roleIds": ["guid-role-1", "guid-role-2"]
  }
  ```

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: Implement all reusable helper modules and configuration modules that scenarios and workflows depend on. These MUST be completed before any user story phase begins.

**âš ï¸ CRITICAL**: No user story work can begin until this phase is complete.

- [X] T004 [P] Create the threshold definitions module at `tests/Performance/k6/config/thresholds.js`

  **Details**: This module exports a `thresholds` object that k6's `options.thresholds` consumes. The thresholds define pass/fail criteria for the entire test run. k6 exits with code 99 when any threshold is breached.

  **Exported interface**:
  ```js
  // Named export: object compatible with k6 options.thresholds
  export const thresholds = { ... };
  ```

  **Required thresholds** (from FR-007):
  - `http_req_failed`: rate < 0.01 (HTTP error rate < 1%)
  - `http_req_duration`: p(95) < 1000 (p95 response time < 1 second)
  - `http_req_duration`: p(99) < 2000 (p99 response time < 2 seconds)
  - `checks`: rate > 0.95 (95% of all k6 checks must pass)

  **Example structure**:
  ```js
  export const thresholds = {
    http_req_failed: ['rate<0.01'],
    http_req_duration: ['p(95)<1000', 'p(99)<2000'],
    checks: ['rate>0.95'],
  };
  ```

- [X] T005 [P] Create the scenario options builder module at `tests/Performance/k6/config/options.js`

  **Details**: This module exports builder functions that generate k6 `options.scenarios` configuration objects. Each function reads environment variables via `__ENV` and provides sensible defaults. The `ramping-vus` executor is used for all scenarios (research decision #5).

  **Exported functions**:
  ```js
  /**
   * Returns a k6 scenario config object for the sustained-load test.
   * Reads from __ENV: TARGET_VUS (default 100), RAMP_UP_DURATION (default '2m'),
   *   SUSTAINED_DURATION (default '30m'), RAMP_DOWN_DURATION (default '1m')
   * 
   * @returns {{ executor: 'ramping-vus', stages: Array<{duration: string, target: number}> }}
   */
  export function getSustainedLoadOptions() { ... }

  /**
   * Returns a k6 scenario config object for the capacity/stress discovery test.
   * Reads from __ENV: CAPACITY_STEP_DURATION (default '3m'),
   *   CAPACITY_STEPS (default '100,250,500,750,1000,1250,1500')
   * 
   * @returns {{ executor: 'ramping-vus', stages: Array<{duration: string, target: number}> }}
   */
  export function getCapacityStressOptions() { ... }
  ```

  **Implementation notes**:
  - **Both functions MUST include `executor: 'ramping-vus'`** in the returned object. Without this property k6 will error.
  - Use `parseInt(__ENV.TARGET_VUS) || 100` pattern for defaults.
  - For `CAPACITY_STEPS`, split the comma-separated string into an array of integers and build stages dynamically: for each step value, add a stage of `{ duration: stepDuration, target: stepValue }`. Add a final ramp-down stage `{ duration: '1m', target: 0 }`.
  - For `getSustainedLoadOptions()`, the three stages are:
    1. `{ duration: rampUpDuration, target: targetVUs }` â€” ramp up
    2. `{ duration: sustainedDuration, target: targetVUs }` â€” sustain
    3. `{ duration: rampDownDuration, target: 0 }` â€” ramp down
  - The sustained-load scenario must support 100, 500, and 1,000 VUs for 30â€“60 minutes via env var overrides.
  - The capacity scenario must support steps: 100 â†’ 250 â†’ 500 â†’ 750 â†’ 1,000 â†’ 1,250 â†’ 1,500 VUs.

- [X] T006 [P] Create the authentication helper at `tests/Performance/k6/helpers/auth.js`

  **Details**: This module handles login against the SPIP backend and returns the JWT token. It uses `POST /api/auth/login` with credentials from environment variables. The module validates the response using k6's `check()` function.

  **Design decision â€” authenticate once per VU**: Unlike the previous per-iteration design, authentication now happens **once per VU lifetime** in k6's `setup()` phase or via a VU-level init pattern. The token is stored and reused across all iterations. If a 401 response is received during the workflow, the VU re-authenticates automatically.

  **Exported functions**:
  ```js
  import http from 'k6/http';
  import { check } from 'k6';

  /**
   * Authenticates against the SPIP backend and returns the JWT token string.
   * Reads from __ENV: BASE_URL, TEST_USER_EMAIL, TEST_USER_PASSWORD
   * 
   * @returns {string} JWT token (without 'Bearer ' prefix), or empty string on failure.
   * 
   * Request body: { email: string, password: string }
   * Expected response (200): { success: true, data: { token: "jwt-string", ... } }
   * 
   * k6 checks performed:
   *   - 'login status is 200': res.status === 200
   *   - 'login success is true': body.success === true
   *   - 'login returned token': body.data && body.data.token
   */
  export function authenticate() {
    const url = `${__ENV.BASE_URL}/api/auth/login`;
    const payload = JSON.stringify({
      email: __ENV.TEST_USER_EMAIL,
      password: __ENV.TEST_USER_PASSWORD,
    });
    const params = {
      headers: { 'Content-Type': 'application/json' },
      tags: { name: 'POST /api/auth/login' },
    };
    const res = http.post(url, payload, params);
    
    const body = JSON.parse(res.body || '{}');
    check(res, {
      'login status is 200': (r) => r.status === 200,
      'login success is true': () => body.success === true,
      'login returned token': () => body.data && body.data.token,
    });
    
    return (body.data && body.data.token) ? body.data.token : '';
  }
  ```

  **Important**: The login endpoint is rate-limited (`AuthPolicy`), but since authentication happens only once per VU (not per iteration), rate limiting is not a concern during sustained-load tests. During VU ramp-up, k6 naturally spaces out VU initialization.

- [X] T007 [P] Create the HTTP wrapper module at `tests/Performance/k6/helpers/http-wrapper.js`

  **Details**: This module provides convenience wrappers around `k6/http` methods that automatically prepend `BASE_URL`, set the `Authorization: Bearer` header, set the `Content-Type` header, and apply per-endpoint metric tagging via the `name` tag (research decision #4, FR-014).

  **Exported functions**:
  ```js
  import http from 'k6/http';

  /**
   * Sends a GET request to the specified API path.
   * @param {string} path - API path without base URL (e.g., '/api/vendors')
   * @param {string} token - JWT token for Authorization header
   * @param {string} endpointName - Metric tag name (e.g., 'GET /api/vendors')
   * @returns {import('k6/http').RefinedResponse} k6 HTTP response
   */
  export function get(path, token, endpointName) { ... }

  /**
   * Sends a POST request with JSON body.
   * NOTE: Not used by the primary read-heavy workflows.
   * Retained for future dedicated write-performance tests.
   * 
   * @param {string} path - API path without base URL
   * @param {object} body - Request body (will be JSON.stringified)
   * @param {string} token - JWT token
   * @param {string} endpointName - Metric tag name (e.g., 'POST /api/vendors')
   * @returns {import('k6/http').RefinedResponse}
   */
  export function post(path, body, token, endpointName) { ... }

  /**
   * Sends a PUT request with JSON body.
   * NOTE: Not used by the primary read-heavy workflows.
   * Retained for future dedicated write-performance tests.
   * 
   * @param {string} path - API path without base URL
   * @param {object} body - Request body
   * @param {string} token - JWT token
   * @param {string} endpointName - Metric tag name (e.g., 'PUT /api/vendors/{id}')
   * @returns {import('k6/http').RefinedResponse}
   */
  export function put(path, body, token, endpointName) { ... }

  /**
   * Sends a DELETE request.
   * NOTE: Not used by the primary read-heavy workflows.
   * Retained for future dedicated write-performance tests.
   * 
   * @param {string} path - API path without base URL
   * @param {string} token - JWT token
   * @param {string} endpointName - Metric tag name (e.g., 'DELETE /api/vendors/{id}')
   * @returns {import('k6/http').RefinedResponse}
   */
  export function del(path, token, endpointName) { ... }
  ```

  **Implementation notes**:
  - Always use `${__ENV.BASE_URL}${path}` for the full URL. No hardcoded hostnames (FR-005).
  - Headers: `{ 'Authorization': \`Bearer ${token}\`, 'Content-Type': 'application/json' }`
  - Tags: `{ tags: { name: endpointName } }` â€” this overrides k6's default URL-based grouping so metrics are aggregated by endpoint pattern rather than unique URLs (e.g., all `/api/vendors/1`, `/api/vendors/2` calls group under `GET /api/vendors/{id}`).
  - For `post` and `put`, `JSON.stringify(body)` before sending.
  - For `del`, no body is needed.
  - The `post`, `put`, and `del` functions are **fully implemented** but are not imported or called by the primary workflow (`user-journey.js`). They exist for future write-performance test scenarios.

- [X] T008 [P] Create the reusable checks module at `tests/Performance/k6/helpers/checks.js`

  **Details**: This module provides reusable k6 `check()` assertion functions for validating API responses. All SPIP responses use the `ApiResponse<T>` wrapper (research decision #7).

  **Exported functions**:
  ```js
  import { check } from 'k6';

  /**
   * Validates that an API response is successful:
   *   1. HTTP status is 200 or 201
   *   2. Response body parses as JSON
   *   3. body.success === true (ApiResponse wrapper check)
   * 
   * @param {import('k6/http').RefinedResponse} res - k6 HTTP response
   * @param {string} label - Descriptive label for the check (e.g., 'GET vendors list')
   * @returns {boolean} true if all checks pass
   */
  export function checkApiResponse(res, label) { ... }

  /**
   * Validates a paginated list response:
   *   1. All checks from checkApiResponse
   *   2. body.data.items is an array
   *   3. body.data.totalCount is a number >= 0
   * 
   * @param {import('k6/http').RefinedResponse} res - k6 HTTP response
   * @param {string} label - Descriptive label
   * @returns {boolean}
   */
  export function checkPagedResponse(res, label) { ... }

  /**
   * Validates a single-entity response:
   *   1. All checks from checkApiResponse
   *   2. body.data is truthy (entity exists)
   * 
   * @param {import('k6/http').RefinedResponse} res - k6 HTTP response
   * @param {string} label - Descriptive label
   * @returns {boolean}
   */
  export function checkEntityResponse(res, label) { ... }
  ```

  **Implementation notes**:
  - Always parse `res.body` with `JSON.parse()` inside a try-catch. If parsing fails, all checks for that response should fail.
  - Use k6's `check()` function with descriptive check names like: `check(res, { [\`${label}: status is 2xx\`]: (r) => r.status >= 200 && r.status < 300 })`.
  - The `success` field check is critical â€” the SPIP backend can return HTTP 200 with `"success": false` for application-level errors (data-model.md, API response wrapper section).

- [X] T009 [P] Create the data utilities module at `tests/Performance/k6/helpers/data-utils.js`

  **Details**: This module provides helper functions for randomizing selections from pre-seeded test data arrays and applying think-time pauses. Since the primary workflow is read-only, no write-data generators are needed in the primary flow. Write-data generators are included but documented as reserved for future write tests.

  **Exported functions**:
  ```js
  import { sleep } from 'k6';

  /**
   * Returns a random element from the given array.
   * Used to select random entity IDs from test-config.json arrays
   * for read-only endpoint calls.
   * 
   * @param {Array} arr - Array to select from
   * @returns {*} A random element
   */
  export function randomItem(arr) { ... }

  /**
   * Sleeps for a random duration between the configured think-time bounds.
   * Reads __ENV.THINK_TIME_MIN (default 1) and __ENV.THINK_TIME_MAX (default 5).
   * Uses: sleep(Math.random() * (max - min) + min)
   * 
   * Per research decision #2: uniform random distribution between 1â€“5 seconds.
   */
  export function thinkTime() { ... }

  // --- Future write-test utilities (not used by primary read-heavy workflow) ---

  /**
   * Generates a unique vendor name for write isolation.
   * Pattern: 'k6-vendor-{VU_ID}-{ITER}-{TIMESTAMP}'
   * NOTE: Not used by primary read-heavy workflow. Reserved for future write tests.
   * 
   * @returns {string} Unique vendor name
   */
  export function generateVendorName() { ... }

  /**
   * Generates a unique product name for write isolation.
   * Pattern: 'k6-product-{VU_ID}-{ITER}-{TIMESTAMP}'
   * NOTE: Not used by primary read-heavy workflow. Reserved for future write tests.
   * 
   * @returns {string} Unique product name
   */
  export function generateProductName() { ... }

  /**
   * Generates a unique vendor column mapping for write isolation.
   * NOTE: Not used by primary read-heavy workflow. Reserved for future write tests.
   * 
   * @param {number} vendorId - The vendor ID to map to
   * @returns {{ vendorId: number, systemField: string, excelColumn: string }}
   */
  export function generateVendorMapping(vendorId) { ... }
  ```

  **Implementation notes**:
  - `thinkTime()` must import `sleep` from `k6` module.
  - `randomItem()` is the primary function used by the read-heavy workflow.
  - Write-data generators (`generateVendorName`, `generateProductName`, `generateVendorMapping`) are implemented but **not imported** by `user-journey.js`. They are available for future write-performance test scenarios.

**Checkpoint**: Foundation ready â€” all helper and config modules are implemented. User story implementation can now begin.

---

## Phase 3: User Story 3 â€” Realistic User Workflow Simulation (Priority: P1)

**Goal**: Virtual users simulate realistic **read-heavy** application usage: authenticate once per VU, browse multiple endpoints across vendors/products/purchase orders/users/roles/permissions/audit logs using pre-seeded test data, and pause with configurable think-time between actions. No write operations in the primary workflow.

**Independent Test**: Run any scenario and verify that k6 output shows requests to multiple different endpoint tags with think-time pauses between iterations. All requests should be GET operations plus a single POST for authentication.

> **Note**: User Story 3 is implemented first (Phase 3) because both US1 and US2 depend on the user journey workflow.

### Implementation for User Story 3

- [X] T010 [US3] Create the user journey workflow at `tests/Performance/k6/workflows/user-journey.js`

  **Details**: This is the core workflow module that every scenario calls (FR-003). It simulates a realistic **read-heavy** multi-step user session. Each call to `userJourney()` represents one complete VU iteration of **read-only** endpoint browsing.

  **Authentication model â€” once per VU**:
  The workflow authenticates once per VU lifetime using a module-level variable. On the first iteration (or after a 401 response), it calls `authenticate()` and caches the token. All subsequent iterations reuse the cached token. This is more realistic than per-iteration auth and avoids distorting the workload with unnecessary login requests.

  **File structure**:
  ```js
  import { authenticate } from '../helpers/auth.js';
  import { get } from '../helpers/http-wrapper.js';
  import { checkPagedResponse, checkEntityResponse } from '../helpers/checks.js';
  import { randomItem, thinkTime } from '../helpers/data-utils.js';

  // Load test data IDs at init time (runs once per VU, not per iteration)
  const testConfig = JSON.parse(open('../data/test-config.json'));

  // Validate required environment variables (runs once during init phase)
  const requiredEnvVars = ['BASE_URL', 'TEST_USER_EMAIL', 'TEST_USER_PASSWORD'];
  for (const envVar of requiredEnvVars) {
    if (!__ENV[envVar]) {
      throw new Error(
        `Missing required environment variable: ${envVar}. ` +
        `Set it with: k6 run -e ${envVar}=<value> ...`
      );
    }
  }

  // VU-level token cache (persists across iterations for this VU)
  let cachedToken = null;

  /**
   * Returns a valid JWT token, authenticating only if needed.
   * Called at the start of each iteration.
   * Re-authenticates if: (a) no cached token, or (b) previous response was 401.
   */
  function getToken() {
    if (!cachedToken) {
      cachedToken = authenticate();
    }
    return cachedToken;
  }

  /**
   * Invalidates the cached token, forcing re-authentication on next call.
   * Called when a 401 response is received.
   */
  function invalidateToken() {
    cachedToken = null;
  }

  /**
   * Executes a complete, realistic READ-ONLY user journey:
   * 
   * 1. Get token (authenticate if first iteration or token expired)
   * 2. Browse vendors list         (GET /api/vendors)
   * 3. Think-time pause
   * 4. View a specific vendor      (GET /api/vendors/{id})
   * 5. Think-time pause
   * 6. Browse products list        (GET /api/products)
   * 7. Think-time pause
   * 8. View a specific product     (GET /api/products/{id})
   * 9. Think-time pause
   * 10. Browse purchase orders list (GET /api/purchase-orders)
   * 11. Think-time pause
   * 12. View a specific PO         (GET /api/purchase-orders/{id})
   * 13. Think-time pause
   * 14. Browse users list           (GET /api/users)
   * 15. Think-time pause
   * 16. View a specific user        (GET /api/users/{id})
   * 17. Think-time pause
   * 18. Browse roles list           (GET /api/roles)
   * 19. Think-time pause
   * 20. View a specific role        (GET /api/roles/{id})
   * 21. Think-time pause
   * 22. View permissions            (GET /api/permissions)
   * 23. Think-time pause
   * 24. Browse audit logs list      (GET /api/audit-logs)
   * 25. Think-time pause
   * 26. View a specific audit log   (GET /api/audit-logs/{id})
   * 27. Think-time pause
   * 28. View vendor mappings        (GET /api/vendor-mappings/vendor/{vendorId})
   * 29. Think-time pause (end of iteration)
   * 
   * Total: 14 GET requests per iteration + think-time between each.
   * No POST, PUT, or DELETE operations.
   */
  export function userJourney() { ... }
  ```

  **Implementation notes â€” CRITICAL for Codex**:

  1. **Authentication â€” once per VU, not per iteration**: Call `getToken()` at the top of each iteration. On the first iteration, `cachedToken` is null so `authenticate()` is called. On subsequent iterations, the cached token is returned immediately. If any endpoint returns HTTP 401, call `invalidateToken()` and `getToken()` to re-authenticate, then retry the failed request or continue to the next step.

  2. **Using `open()` for test-config.json**: The `open()` call MUST be at the module level (outside any function), not inside `userJourney()`. k6's `open()` only works during the init phase. Use `const testConfig = JSON.parse(open('../data/test-config.json'));` at the top of the file.

  3. **Read-only endpoints â€” complete list (14 GET calls per iteration)**:
     | # | Endpoint | Tag Name | ID Source |
     |---|----------|----------|-----------|
     | 1 | `GET /api/vendors` | `'GET /api/vendors'` | N/A (list) |
     | 2 | `GET /api/vendors/{id}` | `'GET /api/vendors/{id}'` | `randomItem(testConfig.vendorIds)` |
     | 3 | `GET /api/products` | `'GET /api/products'` | N/A (list) |
     | 4 | `GET /api/products/{id}` | `'GET /api/products/{id}'` | `randomItem(testConfig.productIds)` |
     | 5 | `GET /api/purchase-orders` | `'GET /api/purchase-orders'` | N/A (list) |
     | 6 | `GET /api/purchase-orders/{id}` | `'GET /api/purchase-orders/{id}'` | `randomItem(testConfig.purchaseOrderIds)` |
     | 7 | `GET /api/users` | `'GET /api/users'` | N/A (list) |
     | 8 | `GET /api/users/{id}` | `'GET /api/users/{id}'` | `randomItem(testConfig.userIds)` |
     | 9 | `GET /api/roles` | `'GET /api/roles'` | N/A (list) |
     | 10 | `GET /api/roles/{id}` | `'GET /api/roles/{id}'` | `randomItem(testConfig.roleIds)` |
     | 11 | `GET /api/permissions` | `'GET /api/permissions'` | N/A (no ID) |
     | 12 | `GET /api/audit-logs` | `'GET /api/audit-logs'` | N/A (list) |
     | 13 | `GET /api/audit-logs/{id}` | `'GET /api/audit-logs/{id}'` | `randomItem(testConfig.auditLogIds)` |
     | 14 | `GET /api/vendor-mappings/vendor/{vendorId}` | `'GET /api/vendor-mappings/vendor/{vendorId}'` | `randomItem(testConfig.vendorIds)` |

  4. **Only import `get` from http-wrapper.js**: The primary workflow does NOT import `post`, `put`, or `del`. Only the `get` function is needed.

  5. **Check functions**: Use the appropriate check function after each HTTP call:
     - `checkPagedResponse(res, label)` for list endpoints (GET /api/vendors, GET /api/products, GET /api/purchase-orders, GET /api/users, GET /api/roles, GET /api/audit-logs)
     - `checkEntityResponse(res, label)` for single-entity endpoints (GET /api/vendors/{id}, GET /api/products/{id}, GET /api/purchase-orders/{id}, GET /api/users/{id}, GET /api/roles/{id}, GET /api/audit-logs/{id}, GET /api/permissions, GET /api/vendor-mappings/vendor/{vendorId})

  6. **Think-time**: Call `thinkTime()` between each endpoint call. This adds a 1â€“5 second random pause (FR-003).

  7. **401 handling**: After each `get()` call, check if `res.status === 401`. If so, call `invalidateToken()`, then `getToken()` to re-authenticate. This handles token expiry during long test runs (30â€“60 minutes) without adding `POST /api/auth/refresh-token` calls that would distort the read-heavy workload.

  8. **No excluded endpoints**: Do NOT call any endpoint listed in the excluded endpoints table in `contracts/api-contracts.md`:
     - No invoices, no dashboard, no reconciliation reports, no AI chat
     - No auth/register, no user write operations, no role write operations, no PO import
     - No POST, PUT, or DELETE operations of any kind

  9. **Error resilience**: The workflow must not crash if individual endpoints return errors. Use the check functions (which use k6's `check()`) to record pass/fail, but always continue to the next step. Only abort the iteration early if authentication fails (no token from `getToken()`).

  10. **Environment variable validation**: The `requiredEnvVars` validation block runs during the init phase (module level). If any required variable is missing, k6 will throw an error immediately with a clear message before any VU starts.

**Checkpoint**: At this point, the read-only user journey workflow is complete and independently testable.

---

## Phase 4: User Story 1 â€” Sustained Load Testing (Priority: P1) ðŸŽ¯ MVP

**Goal**: Provide a sustained-load test scenario that runs a configurable number of concurrent virtual users (100, 500, or 1,000) for a configurable duration (30â€“60 min) with gradual ramp-up and ramp-down, producing p50/p95/p99 response times, throughput, and error rate with configurable thresholds. The workload is **read-only**.

**Independent Test**: Run `k6 run -e BASE_URL=... -e TEST_USER_EMAIL=... -e TEST_USER_PASSWORD=... --vus 2 --duration 1m scenarios/sustained-load.js` and verify the test starts, VUs authenticate once, execute read-only endpoint calls with think-time, and produce a summary report with per-endpoint metrics.

### Implementation for User Story 1

- [X] T011 [US1] Create the sustained-load scenario at `tests/Performance/k6/scenarios/sustained-load.js`

  **Details**: This is the primary test script for sustained-load testing (FR-001). It is a complete k6 test script with `options` export and a default export function. The workload is entirely read-only.

  **File structure**:
  ```js
  import { thresholds } from '../config/thresholds.js';
  import { getSustainedLoadOptions } from '../config/options.js';
  import { userJourney } from '../workflows/user-journey.js';

  export const options = {
    scenarios: {
      sustained_load: {
        ...getSustainedLoadOptions(),
        exec: 'default',
      },
    },
    thresholds: thresholds,
  };

  export default function () {
    userJourney();
  }
  ```

  **Implementation notes**:
  - The `options` object merges the scenario configuration from `getSustainedLoadOptions()` with the shared thresholds from `thresholds.js`.
  - The `default` export function is the VU entry point. It calls `userJourney()` which handles cached authentication and read-only endpoint calls (see T010).
  - `getSustainedLoadOptions()` returns an object with `executor: 'ramping-vus'` and three stages (ramp-up â†’ sustain â†’ ramp-down).
  - When the engineer overrides via CLI (e.g., `--vus 2 --duration 1m`), k6 CLI flags take precedence over script-defined options, so the script works for both quick smoke tests and full load runs.
  - Supports 100, 500, and 1,000 VUs for 30â€“60 minutes via `TARGET_VUS` and `SUSTAINED_DURATION` env vars.

**Checkpoint**: User Story 1 (sustained-load scenario with read-only workload) is fully functional.

---

## Phase 5: User Story 2 â€” Capacity / Stress Discovery (Priority: P2)

**Goal**: Provide a capacity/stress test scenario that incrementally increases virtual users in configurable steps (100 â†’ 250 â†’ 500 â†’ 750 â†’ 1,000 â†’ 1,250 â†’ 1,500) to identify the backend's degradation point under **read-heavy** load.

**Independent Test**: Run `k6 run -e BASE_URL=... -e TEST_USER_EMAIL=... -e TEST_USER_PASSWORD=... scenarios/capacity-stress.js` and verify VUs increase in steps, with each step sustained for ~3 minutes, and all requests are read-only.

### Implementation for User Story 2

- [X] T012 [US2] Create the capacity/stress discovery scenario at `tests/Performance/k6/scenarios/capacity-stress.js`

  **Details**: This is the capacity/stress test script (FR-002). It uses the stepped ramp configuration from `getCapacityStressOptions()`. The workload is entirely read-only.

  **File structure**:
  ```js
  import { thresholds } from '../config/thresholds.js';
  import { getCapacityStressOptions } from '../config/options.js';
  import { userJourney } from '../workflows/user-journey.js';

  export const options = {
    scenarios: {
      capacity_stress: {
        ...getCapacityStressOptions(),
        exec: 'default',
      },
    },
    thresholds: thresholds,
  };

  export default function () {
    userJourney();
  }
  ```

  **Implementation notes**:
  - The structure mirrors `sustained-load.js` but uses `getCapacityStressOptions()` for the stepped ramp-up pattern.
  - Capacity steps: 100 â†’ 250 â†’ 500 â†’ 750 â†’ 1,000 â†’ 1,250 â†’ 1,500 VUs with configurable step duration (default 3 minutes per step).
  - Stages example:
    ```
    stages: [
      { duration: '3m', target: 100 },
      { duration: '3m', target: 250 },
      { duration: '3m', target: 500 },
      { duration: '3m', target: 750 },
      { duration: '3m', target: 1000 },
      { duration: '3m', target: 1250 },
      { duration: '3m', target: 1500 },
      { duration: '1m', target: 0 },  // ramp down
    ]
    ```
  - When thresholds are breached (e.g., error rate > 1%), k6 continues running but reports the breach clearly. The test engineer can abort manually (`Ctrl+C`). Per-step metrics show exactly where degradation started.

**Checkpoint**: User Story 2 (capacity/stress scenario with read-only workload) is functional.

---

## Phase 6: User Story 4 â€” Environment-Portable Test Execution (Priority: P2)

**Goal**: Ensure the same k6 test scripts work against any SPIP backend environment by changing only the `BASE_URL` environment variable. No hardcoded hostnames anywhere.

**Independent Test**: Run any scenario with `-e BASE_URL=https://localhost:7001` and then with `-e BASE_URL=https://staging.example.com` â€” both should execute identically (results differ based on backend performance, but no script errors).

### Implementation for User Story 4

- [X] T013 [US4] Audit all helper and scenario files to verify no hardcoded hostnames exist and all HTTP requests use `__ENV.BASE_URL`

  **Details**: This is a verification task. Review every `.js` file under `tests/Performance/k6/` and confirm:
  1. No file contains a hardcoded URL like `https://localhost`, `http://localhost`, `https://staging`, or any IP address.
  2. All HTTP requests go through the `http-wrapper.js` module which prepends `__ENV.BASE_URL`.
  3. The `auth.js` helper uses `${__ENV.BASE_URL}/api/auth/login` â€” not a hardcoded URL.
  4. If any hardcoded URL is found, replace it with `__ENV.BASE_URL` reference.
  
  **No file changes expected** if T004â€“T012 were implemented correctly. This task exists as an explicit verification step.

**Checkpoint**: All 4 user stories are now complete. The test suite is environment-portable and validates its configuration.

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: Documentation, final validation, and cross-cutting improvements.

- [X] T014 [P] Create the README documentation at `tests/Performance/k6/README.md`

  **Details**: Write a comprehensive README (FR-013) that enables a team member to set up and execute the test suite within 15 minutes (SC-009). The README must cover:

  **Sections to include**:

  1. **Overview**: Brief description â€” emphasize that the suite is a **read-heavy** performance test that measures backend capacity under realistic concurrent-user read workloads. No write operations are performed during primary tests.

  2. **Prerequisites**:
     - k6 installation instructions for Windows (`winget install k6 --source winget` or `choco install k6`), macOS (`brew install k6`), Linux (link to k6 docs).
     - Verify installation: `k6 version`
     - SPIP backend running at a known URL
     - Test user account pre-created with admin-level permissions
     - **Pre-seeded test data** (vendors, products, purchase orders, users, roles, audit logs) â€” emphasize that the test only reads this data, never modifies it

  3. **Environment Setup**: How to configure environment variables. Reference `.env.example`. List all variables with descriptions and defaults.

  4. **Quick Start** â€” step-by-step:
     ```bash
     # 1. Navigate to the test directory
     cd tests/Performance/k6

     # 2. Run a quick smoke test (2 VUs, 1 minute)
     k6 run -e BASE_URL=https://localhost:7001 \
            -e TEST_USER_EMAIL=perftest@example.com \
            -e TEST_USER_PASSWORD=YourTestPassword123! \
            --vus 2 --duration 1m \
            scenarios/sustained-load.js

     # 3. Run a full sustained-load test (100 VUs, 30 minutes)
     k6 run -e BASE_URL=https://localhost:7001 \
            -e TEST_USER_EMAIL=perftest@example.com \
            -e TEST_USER_PASSWORD=YourTestPassword123! \
            scenarios/sustained-load.js

     # 4. Run a capacity/stress discovery test
     k6 run -e BASE_URL=https://localhost:7001 \
            -e TEST_USER_EMAIL=perftest@example.com \
            -e TEST_USER_PASSWORD=YourTestPassword123! \
            scenarios/capacity-stress.js
     ```

  5. **Customizing Test Parameters**: How to override VU counts, durations, think-time, and capacity steps via environment variables.

  6. **Reading Results**: Table of key metrics. Explain the threshold pass/fail mechanism.

  7. **JSON Output**: `k6 run --out json=results.json ...`

  8. **Per-Endpoint Metrics**: Explain `name` tag-based grouping.

  9. **Infrastructure Monitoring**: Table of backend/database metrics to watch during test runs.

  10. **Troubleshooting**: Table of common problems, causes, and fixes.

  11. **Endpoints Under Test**: Complete list of all **14 read-only GET endpoints** tested by the primary workflow.

  12. **Intentionally Excluded Operations**: Document that all POST/PUT/DELETE operations (except auth login) are excluded from primary tests because the objective is read-heavy capacity measurement. List excluded destructive endpoints:
      - `DELETE /api/purchase-orders/{id}` â€” destructive; outside read-heavy objective
      - `POST /api/vendors`, `PUT /api/vendors/{id}`, `DELETE /api/vendors/{id}` â€” write operations
      - `PUT /api/vendors/{id}/toggle-approval` â€” write operation
      - `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}` â€” write operations
      - `POST /api/vendor-mappings`, `DELETE /api/vendor-mappings/{id}` â€” write operations
      - `POST /api/auth/refresh-token` â€” not needed with VU-level auth caching; would distort read-heavy workload
      - All AI endpoints (AIChatController, InvoicesController, etc.)
      - `POST /api/auth/register` â€” creates accounts
      - All user/role write operations â€” risk of data corruption

  13. **Future Write-Performance Tests**: Note that the helper modules (`http-wrapper.js`, `data-utils.js`) include `post`, `put`, `del`, and write-data generators for future dedicated write-performance scenarios.

  14. **Directory Structure**: Visual tree of the `tests/Performance/k6/` directory.

- [X] T015 [P] Validate that all FR requirements are met by reviewing the complete test suite

  **Details**: This is a final verification task. Go through each functional requirement and confirm it is addressed:

  | FR | Requirement | Where Implemented | Notes |
  |----|-------------|-------------------|-------|
  | FR-001 | Sustained-load scenario with configurable VUs/duration/ramp | `scenarios/sustained-load.js` + `config/options.js` | 100/500/1000 VUs, 30â€“60 min |
  | FR-002 | Capacity/stress scenario with configurable steps | `scenarios/capacity-stress.js` + `config/options.js` | 100â†’250â†’500â†’750â†’1000â†’1250â†’1500 |
  | FR-003 | Realistic workflows with think-time | `workflows/user-journey.js` + `helpers/data-utils.js` | Read-only with 1â€“5s think-time |
  | FR-004 | JWT authentication via env vars, token reuse | `helpers/auth.js` + `workflows/user-journey.js` | Once per VU, cached across iterations |
  | FR-005 | Single BASE_URL env var, no hardcoded hostnames | `helpers/http-wrapper.js` + all files | Verified by T013 |
  | FR-006 | Metric collection | k6 built-in + `config/thresholds.js` | All metrics included |
  | FR-007 | Configurable thresholds | `config/thresholds.js` | error <1%, p95 <1s, p99 <2s |
  | FR-008 | No AI endpoints | `workflows/user-journey.js` | No AI calls |
  | FR-009 | Only existing SPIP endpoints | `workflows/user-journey.js` | 14 GET endpoints (read-only subset) |
  | FR-010 | Separate directory structure | `tests/Performance/k6/` | Organized subdirectories |
  | FR-011 | No hardcoded credentials | `helpers/auth.js` reads from `__ENV` | Validated by T010 env check |
  | FR-012 | No production modification | Read-only workflow, no writes | No data modification at all |
  | FR-013 | README documentation | `README.md` | Comprehensive guide |
  | FR-014 | Per-endpoint metric tagging | `helpers/http-wrapper.js` `name` tags | All 14 endpoints tagged |

  **FR-009 coverage note**: The primary read-heavy workflow covers 14 of the ~28 in-scope endpoints (all GET endpoints). The remaining 14 endpoints are POST/PUT/DELETE write operations that are intentionally excluded from the primary workflow. Write helpers exist in the codebase for future dedicated write-performance tests. `POST /api/auth/refresh-token` is excluded because VU-level auth caching with 401 re-authentication makes it unnecessary and it would distort the read-heavy workload.

  **If any FR is not met**, create or modify the relevant file to address it.

- [X] T016 Run the quickstart.md Scenario 1 validation (smoke test) to confirm the suite works end-to-end

  **Details**: Execute the following command from the repository root:
  ```bash
  cd tests/Performance/k6
  k6 run -e BASE_URL=https://localhost:7001 \
         -e TEST_USER_EMAIL=perftest@example.com \
         -e TEST_USER_PASSWORD=YourTestPassword123! \
         --vus 2 --duration 1m \
         scenarios/sustained-load.js
  ```
  
  **Expected outcomes**:
  - k6 starts without script errors
  - Virtual users authenticate once (first iteration only)
  - All endpoint checks show >95% pass rate
  - Summary shows only GET requests (plus initial POST /api/auth/login for auth)
  - No POST/PUT/DELETE requests to any data endpoint
  - Summary shows p95 response times, throughput, and error rate
  - No `TypeError` or `ReferenceError` in console output
  
  **Note**: This requires a running SPIP backend. If the backend is not available, skip this task and document it as "pending manual validation."

  **Validation status (2026-08-15)**: Pending manual validation. No backend was listening at `https://localhost:7001`; both scenario scripts passed `k6 inspect` validation.

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: No dependencies â€” can start immediately
- **Foundational (Phase 2)**: Depends on Phase 1 (directory structure must exist) â€” BLOCKS all user stories
- **User Story 3 â€” Realistic Workflows (Phase 3)**: Depends on Phase 2 (all helpers). **Implemented first** because US1 and US2 depend on the user journey.
- **User Story 1 â€” Sustained Load (Phase 4)**: Depends on Phase 3 (T010 user journey)
- **User Story 2 â€” Capacity/Stress (Phase 5)**: Depends on Phase 3 (T010 user journey)
- **User Story 4 â€” Environment Portability (Phase 6)**: Depends on Phases 3â€“5 (all files must exist to audit)
- **Polish (Phase 7)**: Depends on all user stories being complete

### User Story Dependencies

- **User Story 3 (P1)**: Implemented FIRST â€” US1 and US2 both depend on the user journey workflow (T010)
- **User Story 1 (P1)**: Can start after US3 (T010) is complete â€” `sustained-load.js` imports `userJourney()`
- **User Story 2 (P2)**: Can start after US3 (T010) is complete â€” `capacity-stress.js` imports `userJourney()`
- **User Story 1 and User Story 2**: Can be implemented in parallel since they are in different files
- **User Story 4 (P2)**: Can start after US1, US2, US3 are done â€” it audits all files

### Within Each User Story

- Config modules before scenario scripts
- Helper modules before workflow modules
- Workflow module before scenario scripts

### Recommended Execution Order for a Single Agent (Codex)

Since a single agent will implement all tasks sequentially, the recommended order is:

1. **T001** â†’ Create directory structure
2. **T002, T003** â†’ Env template + test config (parallel, different files)
3. **T004, T005** â†’ Config modules (parallel, different files)
4. **T006, T007, T008, T009** â†’ All helper modules (parallel, different files)
5. **T010** â†’ User journey workflow (depends on all helpers)
6. **T011** â†’ Sustained-load scenario (depends on T010)
7. **T012** â†’ Capacity-stress scenario (depends on T010)
8. **T013** â†’ Hardcoded hostname audit (depends on all .js files)
9. **T014** â†’ README documentation
10. **T015** â†’ FR validation sweep
11. **T016** â†’ Smoke test (if backend available)

### Parallel Opportunities

```
# Phase 2 â€” all helper modules can be created in parallel:
T004 (thresholds.js) â•‘ T005 (options.js) â•‘ T006 (auth.js) â•‘ T007 (http-wrapper.js) â•‘ T008 (checks.js) â•‘ T009 (data-utils.js)

# Phase 4+5 â€” scenario scripts can be created in parallel (after T010):
T011 (sustained-load.js) â•‘ T012 (capacity-stress.js)

# Phase 7 â€” polish tasks can be done in parallel:
T014 (README.md) â•‘ T015 (FR validation)
```

---

## Implementation Strategy

### MVP First (User Story 3 + User Story 1)

1. Complete Phase 1: Setup (T001â€“T003)
2. Complete Phase 2: Foundational helpers and config (T004â€“T009) â€” CRITICAL, blocks all stories
3. Complete Phase 3: User Story 3 â€” read-only user journey workflow (T010)
4. Complete Phase 4: User Story 1 â€” sustained-load scenario (T011)
5. **STOP and VALIDATE**: Run smoke test with 2 VUs, 1 minute. Verify all requests are GET-only.
6. MVP is ready â€” read-heavy sustained load testing works

### Incremental Delivery

1. Setup + Foundational + US3 + US1 â†’ Read-heavy sustained load testing works â†’ **MVP!**
2. Add US2 (T012) â†’ Capacity/stress testing works
3. Add US4 (T013) â†’ Environment portability verified
4. Add Polish (T014â€“T016) â†’ Documentation complete, full validation

---

## Intentionally Excluded from Primary Workflow

The following endpoints and operations are confirmed as **intentionally excluded** from the primary read-heavy sustained-load and capacity test workflows:

| Endpoint | Reason |
|----------|--------|
| `POST /api/auth/refresh-token` | VU-level auth caching with 401 re-authentication makes explicit refresh unnecessary. Adding it would distort the read-heavy workload. |
| `DELETE /api/purchase-orders/{id}` | Destructive write operation; outside the read-heavy performance objective. |
| All `POST /api/vendors`, `PUT /api/vendors/{id}`, `DELETE /api/vendors/{id}`, `PUT /api/vendors/{id}/toggle-approval` | Write operations excluded from read-heavy workflow. |
| All `POST /api/products`, `PUT /api/products/{id}`, `DELETE /api/products/{id}` | Write operations excluded from read-heavy workflow. |
| All `POST /api/vendor-mappings`, `DELETE /api/vendor-mappings/{id}` | Write operations excluded from read-heavy workflow. |
| All AI endpoints (AIChatController, InvoicesController) | Per FR-008. |
| `GET /api/dashboard/stats` | Excluded per user request. |
| `GET /api/reconciliation-reports/*` | Excluded per user request. |
| `POST /api/auth/register` | Creates accounts; risk of data pollution. |
| All user/role write operations | Risk of permission system corruption. |

The `post`, `put`, `del` helper functions and write-data generators (`generateVendorName`, `generateProductName`, `generateVendorMapping`) are retained in the codebase for future dedicated write-performance test scenarios.

---

## Notes

- [P] tasks = different files, no dependencies between them
- [Story] label maps task to specific user story for traceability
- Each user story should be independently completable and testable
- Commit after each task or logical group
- Stop at any checkpoint to validate independently
- Avoid: vague tasks, same file conflicts, cross-story dependencies that break independence
- All files use ES6 module syntax (`import`/`export`) â€” k6 handles module resolution natively
- No npm, no Node.js, no bundler â€” k6 is a standalone Go-based binary
- The `open()` function for loading `test-config.json` must be called at module level (init phase), not inside exported functions
- **Primary workflow is READ-ONLY** â€” no POST/PUT/DELETE to data endpoints
- **Authentication is once per VU** â€” cached token reused across iterations, re-auth on 401

