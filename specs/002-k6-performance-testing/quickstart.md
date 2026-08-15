# Quickstart: k6 Performance Testing Suite

**Feature**: `002-k6-performance-testing` | **Date**: 2026-08-15

## Prerequisites

1. **k6 installed** — [Installation guide](https://grafana.com/docs/k6/latest/set-up/install-k6/)
   - Windows: `winget install k6 --source winget` or `choco install k6`
   - macOS: `brew install k6`
   - Linux: See [k6 downloads](https://grafana.com/docs/k6/latest/set-up/install-k6/)
   - Verify: `k6 version`

2. **SPIP backend running** at a known URL (local, staging, or test environment)

3. **Test user account** pre-created in the target environment with admin-level permissions (access to all non-AI endpoints)

4. **Test data** pre-seeded in the target environment:
   - At least 3 vendors
   - At least 3 products (linked to existing vendors)
   - At least 3 purchase orders
   - At least 1 audit log entry
   - At least 2 users and 2 roles
   - Record the IDs in `tests/Performance/k6/data/test-config.json`

## Environment Setup

Create a `.env` file (or set environment variables) based on `tests/Performance/k6/.env.example`:

```bash
# Required
BASE_URL=https://localhost:7001
TEST_USER_EMAIL=perftest@example.com
TEST_USER_PASSWORD=YourTestPassword123!

# Optional — override defaults
THINK_TIME_MIN=1          # Minimum think-time seconds (default: 1)
THINK_TIME_MAX=5          # Maximum think-time seconds (default: 5)
```

> **⚠️ Never use production credentials or production URLs.**

## Validation Scenarios

### Scenario 1: Quick Smoke Test (2 VUs, 1 minute)

**Purpose**: Verify all k6 scripts load, authenticate, and execute successfully.

```bash
cd tests/Performance/k6
k6 run -e BASE_URL=https://localhost:7001 \
       -e TEST_USER_EMAIL=perftest@example.com \
       -e TEST_USER_PASSWORD=YourTestPassword123! \
       --vus 2 --duration 1m \
       scenarios/sustained-load.js
```

**Expected outcome**:
- k6 starts without script errors
- Virtual users authenticate successfully (login checks pass)
- All endpoint checks show >95% pass rate
- Summary shows p95 response times, throughput, and error rate
- No `TypeError` or `ReferenceError` in console output

### Scenario 2: Sustained Load Test (100 VUs, 30 minutes)

**Purpose**: Validate backend stability under moderate sustained load.

```bash
k6 run -e BASE_URL=https://localhost:7001 \
       -e TEST_USER_EMAIL=perftest@example.com \
       -e TEST_USER_PASSWORD=YourTestPassword123! \
       -e TARGET_VUS=100 \
       -e SUSTAINED_DURATION=30m \
       scenarios/sustained-load.js
```

**Expected outcome**:
- VUs ramp up gradually to 100
- Load sustained for 30 minutes
- VUs ramp down gradually
- Threshold checks: error rate <1%, p95 <1s, p99 <2s
- Test exits with code 0 (all thresholds passed) or code 99 (threshold breached)

### Scenario 3: Capacity/Stress Discovery

**Purpose**: Identify the backend's practical capacity limit.

```bash
k6 run -e BASE_URL=https://localhost:7001 \
       -e TEST_USER_EMAIL=perftest@example.com \
       -e TEST_USER_PASSWORD=YourTestPassword123! \
       scenarios/capacity-stress.js
```

**Expected outcome**:
- VUs increase in steps: 100 → 250 → 500 → 750 → 1,000 → 1,250 → 1,500
- Each step sustained for ~3 minutes
- Results show at which step p95/p99 or error rate crosses thresholds
- Degradation point is identifiable from the output

### Scenario 4: Environment Portability

**Purpose**: Confirm same scripts work against a different backend URL.

```bash
# Run against a second environment (e.g., staging)
k6 run -e BASE_URL=https://staging.spip.example.com \
       -e TEST_USER_EMAIL=staging-test@example.com \
       -e TEST_USER_PASSWORD=StagingPassword123! \
       --vus 2 --duration 1m \
       scenarios/sustained-load.js
```

**Expected outcome**: Identical test execution with metrics from the staging environment.

## Reading Results

After a test run, k6 prints a summary table. Key metrics to check:

| Metric | What to look for |
|--------|-----------------|
| `http_req_duration` (p95) | Should be < 1,000ms for normal operations |
| `http_req_duration` (p99) | Should be < 2,000ms |
| `http_req_failed` | Should be < 1% |
| `http_reqs` | Total request count — divide by test duration for RPS |
| `iteration_duration` | Full workflow cycle time including think-time |
| `vus` | Peak concurrent VU count |
| `checks` | Percentage of response validations that passed |

For per-endpoint breakdown, look for tagged metrics (grouped by endpoint name).

### JSON Output (Optional)

For machine-readable results:

```bash
k6 run --out json=results.json scenarios/sustained-load.js
```

## Infrastructure Monitoring

During test execution, the team should monitor the SPIP backend and database:

| Resource | What to watch | Warning signs |
|----------|--------------|---------------|
| Backend CPU | Sustained utilization | > 80% sustained |
| Backend Memory | Heap/working set growth | Continuous growth (leak) |
| Database CPU | Query processing load | > 70% sustained |
| Database Connections | Active/idle pool count | Pool exhaustion, connection wait times |
| Slow Queries | Queries > 1 second | Increasing count during load |
| Error Logs | Application exceptions | New error types during load |

These metrics are collected from the backend infrastructure's monitoring tools (not from k6).

## Troubleshooting

| Problem | Cause | Fix |
|---------|-------|-----|
| `ERRO[0000] GoError: dial tcp` | Backend unreachable | Check `BASE_URL`, verify backend is running |
| All login checks fail | Bad credentials or rate limit | Verify `TEST_USER_EMAIL`/`TEST_USER_PASSWORD`; reduce VU ramp-up speed |
| HTTP 403 on all endpoints | Test user missing permissions | Assign admin role to test user account |
| HTTP 429 responses | Rate limiter triggered | Login endpoint is rate-limited — pacing is built into the test |
| `check` failures but HTTP 200 | `ApiResponse.success` is false | Check backend logs for application errors |
| Entity IDs not found (404s) | `test-config.json` IDs don't exist | Update IDs to match target environment |
