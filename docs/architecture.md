# SupplyChainX Architecture Documentation

**Version**: `v0.2.0`  
**Milestone**: `v0.2 – Backend Core & PostgreSQL Integration`

## High-Level Architectural Vision

SupplyChainX is designed as an enterprise-grade, event-driven modular platform built for high reliability, scalability, and strict separation of concerns.

```
+-------------------------------------------------------------------+
|                     Angular Client (Frontend)                     |
+-------------------------------------------------------------------+
                                  | HTTP / REST API (/api/v1)
                                  v
+-------------------------------------------------------------------+
|               ASP.NET Core Web API (SupplyChainX.Api)             |
|    [Serilog Logging] [GlobalExceptionMiddleware] [/api/v1 Prefix] |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|     Application Core (SupplyChainX.Application / AddApplication)  |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|                Domain Model (SupplyChainX.Domain)                 |
+-------------------------------------------------------------------+
                                  ^
                                  | Implementations
+-------------------------------------------------------------------+
|   Infrastructure (SupplyChainX.Infrastructure / AddInfrastructure)|
|           [EF Core DbContext] [DbContext Health Checks]           |
+-------------------------------------------------------------------+
              |                                       |
              v                                       v
      PostgreSQL 16 DB                       Apache Kafka Broker
```

---

## v0.2 Infrastructure Capabilities

### 1. PostgreSQL & EF Core Integration
- `SupplyChainXDbContext` is registered in `SupplyChainX.Infrastructure` using `Npgsql.EntityFrameworkCore.PostgreSQL`.
- Connection string handling prioritizes `ConnectionStrings:DefaultConnection` and `POSTGRES_CONNECTION_STRING` environment variables.
- Auto-retry strategy configured (`EnableRetryOnFailure`) for transient database connection errors.
- No business entities, DbSets, or database migrations are included in v0.2.

### 2. Database Connectivity & Health Checks
- Implemented `AddDbContextCheck<SupplyChainXDbContext>("database")` to dynamically verify PostgreSQL reachability.
- Exposed via `GET /health` endpoint with non-sensitive JSON output.

### 3. Global Exception Handling & RFC 7807 ProblemDetails
- Centralized `GlobalExceptionHandlingMiddleware` catches all unhandled request exceptions.
- Formats responses using standard RFC 7807 `ProblemDetails` (`application/problem+json`).
- Stack traces and detailed exception messages are hidden in production environments.

### 4. API Routing Conventions
- `ApiVersionRouteConvention` automatically prefixes business controller routes with `/api/v1`.
- System endpoints like `/health` remain accessible at root level.
