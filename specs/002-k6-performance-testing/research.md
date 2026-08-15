# Research: k6 Performance Testing Suite

**Feature**: `002-k6-performance-testing` | **Date**: 2026-08-15

## Decision Log

### 1. k6 Test Script Module System

**Decision**: Use k6's native ES6 module imports (local file imports via relative paths)

**Rationale**: k6 bundles scripts internally using its Go-based runtime. ES6 `import`/`export` is the standard k6 module pattern. No need for Webpack, Babel, or npm — k6 handles module resolution natively for local files. This keeps the test suite zero-dependency beyond k6 itself.

**Alternatives considered**:
- **npm + bundler (Webpack/esbuild)**: Adds build complexity, requires Node.js installed alongside k6. Only needed when importing npm packages, which this suite does not require.
- **k6 extensions (xk6)**: Custom Go-based extensions. Unnecessary — built-in `k6/http`, `k6/check`, and `k6/metrics` modules cover all requirements.

### 2. Think-Time Implementation

**Decision**: Use `sleep(Math.random() * 4 + 1)` for 1–5 second random think-time between actions

**Rationale**: k6's `sleep()` function accepts seconds as a float. A uniform random distribution between 1 and 5 seconds is the simplest realistic pacing model. The `Math.random()` approach is standard in k6 documentation and introduces natural variation without requiring external libraries.

**Alternatives considered**:
- **Fixed sleep (e.g., `sleep(3)`)**: Unrealistic — all VUs pause identically, creating synchronized request bursts.
- **Normal/Gaussian distribution**: More realistic but adds complexity. Uniform random is the standard k6 best practice and sufficient for this use case.
- **k6 `randomIntBetween()` from jslib**: Adds an external dependency on `https://jslib.k6.io`. Not needed for a simple random range.

### 3. Authentication Strategy

**Decision**: Authenticate once per VU iteration using `POST api/auth/login`, extract JWT token from `data.token` in the `ApiResponse<AuthResponseDto>` wrapper, and pass it as `Authorization: Bearer <token>` header for all subsequent requests.

**Rationale**: The SPIP backend uses JWT with refresh tokens. Authenticating once per iteration (not once per VU lifetime) is more realistic — real users' sessions have finite duration. The `setup()` function could pre-authenticate, but per-iteration auth is more resilient to token expiry during long test runs. Login is rate-limited (`AuthPolicy`), so the test must pace login requests during ramp-up.

**Alternatives considered**:
- **`setup()` pre-authentication**: Authenticates once for all VUs at test start. Problematic because: (a) tokens may expire during 30–60 minute tests, (b) all VUs share the same token which isn't realistic, (c) `setup()` runs in a single context, not per-VU.
- **Per-request authentication**: Wasteful and unrealistic. No real user logs in before every API call.

### 4. Per-Endpoint Metric Tagging

**Decision**: Use k6's `tags` parameter on each `http.get()`/`http.post()` call with a custom `name` tag matching the endpoint pattern (e.g., `{ tags: { name: 'GET /api/vendors' } }`)

**Rationale**: k6 natively supports custom tags on HTTP requests. Using the `name` tag overrides k6's default URL-based metric grouping, which would otherwise create separate metric entries for each unique URL (e.g., `/api/vendors/1`, `/api/vendors/2`). The `name` tag consolidates metrics by endpoint pattern, enabling per-endpoint analysis in the summary output.

**Alternatives considered**:
- **URL grouping via `http.url` template literal**: k6's `http.url` tagged template function. Works but is less explicit than manual `name` tags and requires a different coding pattern.
- **Custom `Trend` per endpoint**: Creating separate `Trend` objects per endpoint. More verbose and harder to maintain.

### 5. Scenario Configuration Strategy

**Decision**: Use k6 `options.scenarios` with the `ramping-vus` executor for sustained-load and stepped capacity scenarios. All numeric parameters (VU counts, durations, stages) are read from environment variables with sensible defaults.

**Rationale**: The `ramping-vus` executor directly supports the ramp-up → sustain → ramp-down pattern required by the spec. Environment variables allow the same script to be used for 100-user and 1,000-user tests without modification. k6 natively supports `__ENV` for reading environment variables at script initialization.

**Alternatives considered**:
- **`constant-vus` executor**: No ramp-up/ramp-down support. Doesn't meet FR-001.
- **`externally-controlled` executor**: Requires external orchestration. Overengineered for this use case.
- **Separate script per VU count**: Violates the "configurable without script changes" requirement.

### 6. Write Operation Isolation Strategy

**Decision**: Use VU-unique test data by appending the VU ID (`__VU`) and iteration counter (`__ITER`) to generated entity names (e.g., `k6-vendor-${__VU}-${__ITER}`). Write operations (create, update, delete) will create their own test entities, operate on them, and clean up within the same iteration when feasible.

**Rationale**: This prevents concurrent VUs from interfering with each other's data. Each VU creates its own vendor/product, uses it within the workflow, and optionally deletes it at the end. The VU ID ensures uniqueness across concurrent users. Cleanup-within-iteration avoids test data accumulation.

**Alternatives considered**:
- **Shared pre-seeded data for writes**: Concurrent VUs would conflict (e.g., two VUs trying to delete the same vendor).
- **No write operations in load tests**: Would miss testing write endpoint performance, which is part of the spec scope.

### 7. Response Validation Approach

**Decision**: Use k6 `check()` to validate: (a) HTTP status code is in the 2xx range, (b) response body contains `"success":true` (matching the `ApiResponse<T>` wrapper), (c) response body is valid JSON. Checks are tagged per endpoint.

**Rationale**: The SPIP backend wraps all responses in `ApiResponse<T>` with a `success` boolean. Checking this field catches application-level errors that still return HTTP 200. k6's `check()` function records pass/fail metrics without aborting the test, which is the correct behavior for load testing.

**Alternatives considered**:
- **Schema validation**: Full JSON schema validation per response. Too expensive at load-test scale — adds CPU overhead per request.
- **Status code only**: Would miss application-level errors wrapped in HTTP 200 with `"success":false`.

### 8. Results Output Strategy

**Decision**: Use k6's built-in summary output (printed to console at test end) as the primary results mechanism. Optionally support `--out json=results.json` for machine-readable output. The README will document both approaches.

**Rationale**: k6's default console summary already includes all required metrics (p50/p90/p95/p99, throughput, error rate, iteration duration). JSON output enables post-processing or integration with dashboards. No additional output plugins needed for the initial implementation.

**Alternatives considered**:
- **InfluxDB + Grafana**: Full dashboard stack. Excellent for ongoing monitoring but heavy for initial setup. Can be added later via `--out influxdb`.
- **k6 Cloud**: SaaS offering. Adds external dependency and cost.
- **CSV output**: Less structured than JSON. k6's JSON output is better for programmatic analysis.
