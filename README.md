# SupplyChainX

**Version**: `v0.2.0`  
**Milestone**: `v0.2 – Backend Core & PostgreSQL Integration`

SupplyChainX is an enterprise inventory and order management platform built on a modern, modular, scalable event-driven architecture.

---

## v0.2 Scope & Backend Capabilities

Milestone **v0.2** establishes the backend infrastructure foundation:

1. **Entity Framework Core & PostgreSQL Integration**:
   - Registered `SupplyChainXDbContext` in `SupplyChainX.Infrastructure` via Npgsql driver.
   - Connection strings are configuration and environment-variable driven (`ConnectionStrings:DefaultConnection` / `POSTGRES_CONNECTION_STRING`).
   - Zero business entities, tables, or database migrations created.

2. **Database Connectivity & Health Checks**:
   - Integrated ASP.NET Core Health Checks framework with `AddDbContextCheck<SupplyChainXDbContext>("database")`.
   - `/health` endpoint dynamically tests PostgreSQL reachability and reports structured status without exposing credentials.

3. **Global Exception Handling**:
   - Centralized `GlobalExceptionHandlingMiddleware` catching unhandled exceptions and returning standard RFC 7807 `ProblemDetails` (`application/problem+json`).
   - Inner exception details and stack traces are suppressed in production environments.

4. **Structured Request Logging**:
   - Configured Serilog HTTP request logging capturing method, path, HTTP status code, and execution time in milliseconds.

5. **API Routing Convention**:
   - Enforced `/api/v1` route prefix convention for future business endpoints via `ApiVersionRouteConvention`, leaving system endpoints (`/health`) at root.

6. **Modular Dependency Injection**:
   - Encapsulated service registrations in `SupplyChainX.Infrastructure` (`AddInfrastructure()`) and `SupplyChainX.Application` (`AddApplication()`).

---

## Technology Stack

- **Backend**: C# 12 / .NET 8 ASP.NET Core Web API
- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Messaging**: Apache Kafka (KRaft mode)
- **Authentication**: JWT (Deferred)
- **Testing**: xUnit (`FluentAssertions`, `NSubstitute`)
- **Containerization**: Docker & Docker Compose
- **Version Control**: Git

---

## Repository Structure

```
SupplyChainX/
├── frontend/                # Angular client baseline (v0.1)
├── backend/                 # ASP.NET Core Web API solution
│   ├── SupplyChainX.sln
│   └── src/
│       ├── SupplyChainX.Api/          # Controllers, /api/v1 routing, Exception Middleware
│       ├── SupplyChainX.Application/  # Application contracts & AddApplication() DI
│       ├── SupplyChainX.Domain/       # Domain primitives (Entity, AggregateRoot)
│       └── SupplyChainX.Infrastructure/# EF Core DbContext, Npgsql PostgreSQL, AddInfrastructure() DI
├── messaging/               # Kafka schemas and event contract specifications
├── tests/                   # Automated test suites
│   └── SupplyChainX.UnitTests/ # xUnit infrastructure unit tests (Middleware, DI)
├── docs/                    # Architecture and developer documentation
├── scripts/                 # Environment and development scripts
├── infrastructure/          # Container orchestration (Docker Compose)
│   ├── docker-compose.yml
│   └── .env.example
├── LICENSE                  # MIT License
└── README.md                # Project documentation
```

---

## Infrastructure Setup

Docker Compose configuration is available in `infrastructure/docker-compose.yml` to provision local development dependencies:

- **PostgreSQL 16**: Port `5432`
- **Apache Kafka (KRaft)**: Port `9092`

Copy `infrastructure/.env.example` to `infrastructure/.env` before starting services.

```bash
cd infrastructure
cp .env.example .env
docker compose up -d
```

---

## Verification & Health Check

The backend ASP.NET Core API provides a health check status endpoint at:

```
GET /health
```

Response schema:

```json
{
  "status": "Healthy",
  "service": "SupplyChainX API",
  "version": "v0.2.0",
  "timestamp": "2026-08-23T20:32:00Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "SupplyChainXDbContext reachability check"
    }
  ]
}
```
