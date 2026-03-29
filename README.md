# BlockchainTracker

A .NET 10 Web API that polls the BlockCypher API for blockchain data, stores snapshots in PostgreSQL, and exposes them via REST endpoints.

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (10.0.201+)
- [Docker](https://docs.docker.com/get-docker/) (required for integration and functional tests — Testcontainers spins up PostgreSQL automatically)

## Running Tests

### Unit Tests

Unit tests have no external dependencies and can run without Docker:

```bash
dotnet test tests/BlockchainTracker.UnitTests
```

### Integration Tests

Integration tests use [Testcontainers](https://dotnet.testcontainers.org/) to spin up a PostgreSQL 16 container automatically. Docker must be running.

```bash
dotnet test tests/BlockchainTracker.IntegrationTests
```

**What they cover:** repository read/write operations, UnitOfWork persistence, database schema creation, unique index enforcement.

### Functional Tests

Functional tests use `WebApplicationFactory` with a Testcontainers PostgreSQL instance to run the full API in-process. Docker must be running.

```bash
dotnet test tests/BlockchainTracker.FunctionalTests
```

**What they cover:** all HTTP endpoints (`/api/chains`, `/api/chains/tracked`, `/api/chains/{chain}/latest`, `/api/chains/{chain}/history`), status codes, response serialization.

### All Tests

```bash
dotnet test BlockchainTracker.sln
```

> **Note:** Integration and functional tests require Docker Desktop (or a compatible Docker engine) to be running. The Testcontainers library automatically pulls `postgres:16-alpine` and manages container lifecycle — no manual setup needed.

## Running the Application

### With Docker Compose

```bash
docker compose up --build
```

This starts PostgreSQL and the API. The API is available at `http://localhost:8080`.

### Locally (development)

1. Start PostgreSQL (e.g., via Docker Compose or a local install):
   ```bash
   docker compose up -d postgres
   ```

2. Run the API:
   ```bash
   dotnet run --project src/BlockchainTracker.Api
   ```

The API reads `ConnectionStrings:PostgreSql` from `appsettings.json` / environment variables. Set `BLOCKCYPHER_TOKEN` for higher API rate limits (optional).
