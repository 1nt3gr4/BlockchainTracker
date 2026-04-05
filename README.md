# BlockchainTracker

A .NET 10 Web API that polls the [BlockCypher API](https://www.blockcypher.com/dev/) for blockchain data, stores snapshots in PostgreSQL, and exposes them via REST endpoints with caching, resilience policies, and observability.

## Quick Start

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) 10.0.201+
- [Docker](https://docs.docker.com/get-docker/)

### Run Everything with Docker Compose

```bash
docker compose up --build
```

The API is available at **http://localhost:8080**. Swagger UI at **http://localhost:8080/swagger**.

Database migrations run automatically on startup.

### Local Development

Start only PostgreSQL in Docker, then run the API with the .NET CLI:

```bash
# 1. Start PostgreSQL
docker compose up -d postgres

# 2. Run the API
dotnet run --project src/BlockchainTracker.Api
```

The API starts at **http://localhost:5023** (Development profile). Swagger UI is enabled in non-production environments.

### Optional: BlockCypher API Token

The free tier allows ~3 requests/second. For higher limits, set a token:

```bash
# Docker Compose
BLOCKCYPHER_TOKEN=your_token docker compose up --build

# Local
export BlockCypher__Token=your_token
dotnet run --project src/BlockchainTracker.Api
```

---

## API Endpoints

| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/api/chains` | Latest snapshot for each tracked chain |
| `GET` | `/api/chains/tracked` | List of supported chain names |
| `GET` | `/api/chains/{chainName}/latest` | Latest snapshot for a specific chain |
| `GET` | `/api/chains/{chainName}/history?page=1&pageSize=20` | Paginated snapshot history |
| `GET` | `/health` | Health check (reports stale data) |

**Tracked chains:** `btc-main`, `eth-main`, `ltc-main`, `dash-main`, `btc-test3`

---

## Project Structure

```
BlockchainTracker.sln
├── src/
│   ├── BlockchainTracker.Domain          # Entities, interfaces, value objects, configuration
│   ├── BlockchainTracker.Application     # CQRS commands/queries, DTOs, mapping, interfaces
│   ├── BlockchainTracker.Infrastructure  # EF Core, Refit API client, caching, Polly, telemetry
│   └── BlockchainTracker.Api             # API controllers, background worker, health checks
└── tests/
    ├── BlockchainTracker.UnitTests           # Fast, no external dependencies
    ├── BlockchainTracker.IntegrationTests    # PostgreSQL via Testcontainers
    └── BlockchainTracker.FunctionalTests     # Full API via WebApplicationFactory + Testcontainers
```

### Architecture

Clean Architecture with four layers. Dependencies flow inward — Domain has zero dependencies, Application depends only on Domain, Infrastructure implements interfaces from both, and Api is the composition root.

**Key patterns:**
- **CQRS** via [Mediator](https://github.com/martinothamar/Mediator) (source-generated, zero-reflection)
- **Repository + Unit of Work** for data access
- **Object mapping** via [Mapperly](https://github.com/riok/mapperly) (source-generated)
- **Pipeline behaviors** for cross-cutting concerns (FluentValidation)

---

## Technology Stack

| Category | Technology |
|----------|-----------|
| Runtime | .NET 10 |
| Web framework | ASP.NET Core MVC (Controllers) |
| Database | PostgreSQL 18, EF Core 10, Npgsql |
| HTTP client | Refit (source-generated) |
| Resilience | Polly (retry with exponential backoff + jitter, rate-limit handling, circuit breaker) |
| Caching | In-memory (`IMemoryCache`) with TTL-based invalidation |
| CQRS | Mediator (source-generated) |
| Mapping | Mapperly (source-generated) |
| Logging | Serilog (structured, console sink) |
| Metrics | OpenTelemetry (`System.Diagnostics.Metrics`) |
| Testing | xUnit v3, NSubstitute, Testcontainers |
| Containerization | Docker, Docker Compose |

---

## Resilience

HTTP calls to BlockCypher are protected by three Polly policies (applied in order):

1. **Rate-limit retry** — On `429 Too Many Requests`, retries up to 5 times with exponential backoff + 1–3s random jitter to avoid thundering herd
2. **Transient error retry** — On 5xx / network errors, retries 3 times with exponential backoff
3. **Circuit breaker** — Opens after 5 consecutive failures, stays open for 30 seconds

---

## Background Polling

A `BackgroundService` polls all tracked chains every 5 minutes (configurable via `Polling:Interval`). Chains are fetched in parallel with `MaxDegreeOfParallelism: 3` by sending individual `FetchChainDataCommand` messages via Mediator. Duplicate snapshots (same chain + height + hash) are detected and skipped.

---

## Configuration

All settings are configurable via `appsettings.json` or environment variables:

| Setting | Default | Description |
|---------|---------|-------------|
| `ConnectionStrings:PostgreSql` | *(see appsettings)* | PostgreSQL connection string |
| `BlockCypher:BaseUrl` | `https://api.blockcypher.com` | BlockCypher API base URL (**required**) |
| `BlockCypher:Token` | *(empty)* | Optional API token for higher rate limits |
| `Polling:Interval` | `00:05:00` | How often to poll all chains |
| `Polling:MaxDegreeOfParallelism` | `3` | Max concurrent chain fetches |
| `Cache:LatestSnapshotTtl` | `00:01:00` | Cache duration for latest snapshots |
| `Cache:HistoryTtl` | `00:05:00` | Cache duration for history queries |
| `HealthCheck:MaxStaleAge` | `00:10:00` | Data older than this marks health as degraded |

---

## Database

- **PostgreSQL 18** with EF Core 10
- Migrations run automatically on application startup (uses PostgreSQL advisory locks — safe for multi-instance deployments)
- Single table: `blockchain_snapshots` with JSONB column for raw API responses
- Unique index on `(chain_name, height, hash)` prevents duplicate snapshots
- Composite index on `(chain_name, fetched_at)` for efficient history queries

---

## Testing

All integration and functional tests use [Testcontainers](https://dotnet.testcontainers.org/) — Docker must be running, but **no manual database setup is needed**.

```bash
# Unit tests only (no Docker needed)
dotnet test tests/BlockchainTracker.UnitTests

# Integration tests (Docker required)
dotnet test tests/BlockchainTracker.IntegrationTests

# Functional tests (Docker required)
dotnet test tests/BlockchainTracker.FunctionalTests

# All tests
dotnet test
```

| Suite | What it covers |
|-------|---------------|
| **Unit** | Command/query handlers, API client mapping, DTO mapping, chain helper, fetch metrics |
| **Integration** | Repository CRUD, Unit of Work persistence, schema creation, unique index enforcement, pagination, ordering |
| **Functional** | Full HTTP endpoint responses, status codes, validation pipeline, health check, Swagger spec |
