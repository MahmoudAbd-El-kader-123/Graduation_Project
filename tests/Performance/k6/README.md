# SPIP k6 Performance Tests

This read-heavy suite measures SPIP backend capacity with realistic, paced user journeys. Primary scenarios authenticate and issue only GET requests; they never modify application data.

## Prerequisites

- Install k6: Windows `winget install k6 --source winget` or `choco install k6`; macOS `brew install k6`; Linux users should follow the [k6 installation guide](https://grafana.com/docs/k6/latest/set-up/install-k6/).
- Verify the installation with `k6 version`.
- Run SPIP at a known URL and create a test user with permission to access every tested endpoint.
- Seed vendors, products, purchase orders, users, roles, and audit logs. Update `data/test-config.json` with IDs from that environment. The suite only reads these records.
- Use a dedicated test or staging environment, never production.

## Configuration

`.env.example` documents all variables. k6 does not load dotenv files automatically, so pass values with `-e` or set operating-system environment variables.

| Variable | Required | Default | Purpose |
|---|---|---|---|
| `BASE_URL` | Yes | None | Backend URL without a trailing slash |
| `TEST_USER_EMAIL` | Yes | None | Existing test account email |
| `TEST_USER_PASSWORD` | Yes | None | Existing test account password |
| `THINK_TIME_MIN` / `THINK_TIME_MAX` | No | `1` / `5` | Random pause bounds in seconds |
| `TARGET_VUS` | No | `100` | Sustained-load VUs |
| `RAMP_UP_DURATION` | No | `2m` | Sustained ramp-up |
| `SUSTAINED_DURATION` | No | `30m` | Sustained plateau |
| `RAMP_DOWN_DURATION` | No | `1m` | Sustained ramp-down |
| `CAPACITY_STEP_DURATION` | No | `3m` | Duration of each capacity step |
| `CAPACITY_STEPS` | No | `100,250,500,750,1000,1250,1500` | Capacity targets |

## Quick Start

From `tests/Performance/k6`:

```powershell
k6 run -e BASE_URL=https://localhost:7001 -e TEST_USER_EMAIL=perftest@example.com -e TEST_USER_PASSWORD=YourTestPassword123! --vus 2 --duration 1m scenarios/sustained-load.js
```

Run the default 100-VU sustained test or capacity test by omitting `--vus` and `--duration`:

```powershell
k6 run -e BASE_URL=https://localhost:7001 -e TEST_USER_EMAIL=perftest@example.com -e TEST_USER_PASSWORD=YourTestPassword123! scenarios/sustained-load.js
k6 run -e BASE_URL=https://localhost:7001 -e TEST_USER_EMAIL=perftest@example.com -e TEST_USER_PASSWORD=YourTestPassword123! scenarios/capacity-stress.js
```

Override any optional value with another `-e`, for example `-e TARGET_VUS=500 -e SUSTAINED_DURATION=60m` or `-e THINK_TIME_MIN=2 -e THINK_TIME_MAX=8`.

## Results

| Metric | Interpretation |
|---|---|
| `http_reqs` / rate | Total requests and throughput |
| `http_req_failed` | HTTP transport/status failure rate; threshold below 1% |
| `http_req_duration` | Average, max, p50, p90, p95, and p99 latency; p95 below 1s and p99 below 2s |
| `checks` | API contract checks; threshold above 95% |
| `vus` / `vus_max` | Active and maximum virtual users |
| `iteration_duration` | Complete journey duration including think time |

k6 exits with a nonzero status when a threshold fails. The `name` tag groups dynamic URLs under stable endpoint patterns, so endpoint-level latency can be compared. Persist raw output with:

```powershell
k6 run --out json=results.json -e BASE_URL=https://localhost:7001 -e TEST_USER_EMAIL=perftest@example.com -e TEST_USER_PASSWORD=YourTestPassword123! scenarios/sustained-load.js
```

## Infrastructure Monitoring

| Layer | Watch |
|---|---|
| API | CPU, memory, thread pool, request queue, exceptions |
| Database | CPU, connections, query latency, locks, slow queries |
| Host/network | CPU saturation, memory pressure, bandwidth, packet loss |

## Endpoints Under Test

The journey calls `GET /api/vendors`, `/api/vendors/{id}`, `/api/products`, `/api/products/{id}`, `/api/purchase-orders`, `/api/purchase-orders/{id}`, `/api/users`, `/api/users/{id}`, `/api/roles`, `/api/roles/{id}`, `/api/permissions`, `/api/audit-logs`, `/api/audit-logs/{id}`, and `/api/vendor-mappings/vendor/{vendorId}`.

Only `POST /api/auth/login` is used outside those GET requests. The primary tests exclude all data POST/PUT/DELETE operations, token refresh, registration, purchase-order imports, invoices, AI chat, dashboard, reconciliation reports, and user/role writes. Write wrappers and isolated-data generators remain available for future dedicated write-performance scenarios.

## Troubleshooting

| Problem | Cause | Fix |
|---|---|---|
| Missing environment variable | Required `-e` value absent | Supply all three required variables |
| Login checks fail or 401 responses | Invalid account or insufficient permissions | Verify credentials, activation, and role permissions |
| Entity checks fail with 404 | IDs do not exist in the target | Update `data/test-config.json` |
| TLS certificate error | Untrusted local certificate | Trust the certificate; use `--insecure-skip-tls-verify` only for local testing |
| 429 responses | Authentication rate limiter | Slow ramp-up or use appropriately provisioned test accounts |
| Threshold failure | Target is overloaded or environment is unhealthy | Correlate the endpoint tags with infrastructure metrics |

## Structure

```text
k6/
|-- config/       # Thresholds and scenario stage builders
|-- data/         # Environment-specific read-only entity IDs
|-- helpers/      # Authentication, HTTP, checks, and data utilities
|-- scenarios/    # Sustained and capacity entry points
|-- workflows/    # Shared read-only user journey
|-- .env.example
`-- README.md
```
