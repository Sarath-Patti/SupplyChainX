# SupplyChainX

**Version**: `v0.8.0`  
**Milestone**: `v0.8 – Angular Frontend & Operations Dashboard`  
**Status**: `v0.8 – Verified`

SupplyChainX is an enterprise inventory and order management platform built on a modern, event-driven Clean Architecture using C# / .NET 8, PostgreSQL, Apache Kafka, structured operational observability, and an Angular 19 management dashboard.

---

## Architectural Flow & Overview

```
  ┌──────────────────────────────────────────────────────────┐
  │         Angular 19 Frontend (Port 4200)                  │
  │   Dashboard / Products / Warehouses / Inventory Pages   │
  └────────────────────────────┬─────────────────────────────┘
                               │ (REST API & X-Correlation-ID)
                               ▼
  ┌──────────────────────────────────────────────────────────┐
  │              SupplyChainX.Api (.NET 8)                   │
  │     Controllers, Middleware, Health & Metrics Endpoints  │
  └────────────┬────────────────────────────────┬────────────┘
               │                                │
               ▼                                ▼
  ┌──────────────────────────┐    ┌──────────────────────────┐
  │ SupplyChainX.Application │    │ PostgreSQL Database (EF) │
  │  Services & Event Logic  │    │  Entities & Idempotency  │
  └────────────┬─────────────┘    └──────────────────────────┘
               │ (Publish Events)
               ▼
  ┌──────────────────────────┐
  │   Apache Kafka Broker    │
  │   (Primary & DLQ Topics) │
  └────────────┬─────────────┘
               │ (Consume Events)
               ▼
  ┌──────────────────────────┐
  │ KafkaConsumerBackground  │
  │   Idempotent Consumer    │
  └────────────┬─────────────┘
               │ (Retry / DLQ Routing)
               ▼
  ┌──────────────────────────┐
  │ Operational Monitoring   │
  │ Metrics & Health Checks  │
  └──────────────────────────┘
```

---

## Milestone Evolution & Completed Capabilities

### **v0.1 — Project Foundation**
- **Clean Architecture Solution Setup**: Structured into `Domain`, `Application`, `Infrastructure`, and `Api` layers following DDD and Clean Architecture principles.
- **Infrastructure Baseline**: Provisioned PostgreSQL database and Apache Kafka containers using Docker Compose.
- **CI & Build Pipeline**: Initialized automated project build and test configuration.

### **v0.2 — Backend Core + PostgreSQL**
- **ASP.NET Core Web API**: Established Web API host, middleware pipeline, and global exception handling.
- **PostgreSQL Integration**: Configured Entity Framework Core (`SupplyChainXDbContext`) with Npgsql driver and connection pooling.
- **Health Foundation**: Implemented initial API health check endpoint.

### **v0.3 — Product, Warehouse & Inventory Domain**
- **Domain Modeling**: Rich domain entities (`Product`, `Warehouse`, `Inventory`) featuring business rule encapsulation, concurrency tokens, and stock allocation logic (available vs. reserved quantities).
- **Service Layer & Repositories**: Application service boundaries (`ProductService`, `WarehouseService`, `InventoryService`) managing transaction workflows.
- **EF Core Migrations**: Database schema generation and migration (`InitialDomainSchema`).
- **Comprehensive Unit Testing**: Automated unit tests for domain invariants and service operations.

### **v0.4 — Kafka Event Publishing**
- **Asynchronous Event Publishing**: Event producer abstraction (`IEventPublisher`, `KafkaEventPublisher`) using `Confluent.Kafka`.
- **Event Contracts**: Strongly typed event models (`ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductDeletedEvent`, `WarehouseCreatedEvent`, `WarehouseUpdatedEvent`, `WarehouseDeletedEvent`, `InventoryAdjustedEvent`).
- **Domain Event Integration**: Automatic event publishing upon successful state-changing domain operations to dedicated Kafka topics:
  - `supplychainx.product.events`
  - `supplychainx.warehouse.events`
  - `supplychainx.inventory.events`

### **v0.5 — Kafka Event Consumption + Idempotency**
- **Hosted Consumer Service**: Non-blocking background worker (`KafkaConsumerBackgroundService`) subscribing under consumer group `supplychainx-event-consumers`.
- **Application Event Handlers**: Specialized event handlers (`IEventHandler<TEvent>`) executing domain processing.
- **Manual Offset Commits**: Explicit offset commits (`EnableAutoCommit = false`) triggered only after successful database processing.
- **PostgreSQL Idempotency Store**: Duplicate message prevention using PostgreSQL-backed `ProcessedEvents` table (`EventId`, `EventType`, `ProcessedAtUtc`).

### **v0.6 — Retry Handling & Dead Letter Queue (DLQ)**
- **Resilient Retry Loop**: Automatic retry handling for transient processing failures with configurable attempt counts (`MaxRetryAttempts = 3`) and optional exponential backoff.
- **Automatic Topic Provisioning**: Startup topic initialization via Kafka `IAdminClient` ensuring primary and DLQ topics exist prior to consumer subscription:
  - `supplychainx.product.events.dlq`
  - `supplychainx.warehouse.events.dlq`
  - `supplychainx.inventory.events.dlq`
- **Dead Letter Routing**: Poison message routing to corresponding `.dlq` topics with diagnostic headers (`x-original-topic`, `x-original-partition`, `x-original-offset`, `x-exception-message`, `x-failed-at-utc`, `x-retry-attempts`, `x-event-id`, `x-event-type`).
- **Guaranteed Offset Semantics**: Offsets are committed **only** after successful processing or successful DLQ publication, preventing data loss.
- **Malformed Payload Isolation**: Invalid JSON payloads log error context and commit offset without halting consumer execution.

### **v0.7 — Observability & Health Monitoring**
- **Active Health Checks**:
  - `/health`: Detailed health report checking PostgreSQL `DbContext` and active Kafka broker connectivity via `AdminClient`.
  - `/health/ready`: Readiness probe verifying backend dependencies are fully operational.
  - `/health/live`: Liveness probe for process execution monitoring.
- **Operational Metrics Endpoint (`GET /api/v1/metrics`)**: Exposes consumer runtime state (`IsRunning`, `ConsumerGroupId`, `SubscribedTopics`), event throughput counters, retry/DLQ statistics, and process memory/thread metrics without exposing sensitive secrets or connection strings.
- **Thread-Safe Metrics Service**: `IKafkaConsumerStatusService` / `KafkaConsumerStatusService` backed by `Interlocked` atomic counters and `System.Diagnostics.Metrics` instruments.
- **Request Tracing**: `CorrelationIdMiddleware` injecting `X-Correlation-ID` headers into HTTP requests and Serilog `LogContext`.
- **Structured Logging**: Contextual log enrichment (`EventId`, `EventType`, `Topic`, `Partition`, `Offset`, `CorrelationId`).

### **v0.8 — Angular Frontend & Operations Dashboard**
- **Angular 19 + TypeScript Client**: Built standalone component architecture in `frontend/` with Angular Router and modular layout.
- **API Service Layer**: Reusable HTTP services (`HealthService`, `MetricsService`, `ProductService`, `WarehouseService`, `InventoryService`) communicating directly with ASP.NET Core Web API endpoints.
- **Operations & Telemetry Dashboard (`/dashboard`)**: Real-time health probes (`/health`, `/health/ready`, `/health/live`), Kafka consumer group status, throughput counters, retry/DLQ metrics, and system runtime telemetry.
- **Product Catalog Management (`/products`)**: Paginated product list with search/status filters, view details, create modal, update modal, and delete confirmation.
- **Warehouse Management (`/warehouses`)**: Paginated warehouse list with search/status filters, location configuration, and full CRUD modal dialogs.
- **Inventory Control & Stock Allocation (`/inventory`)**: Stock overview with low stock alerts (`availableQuantity < threshold`), product/warehouse filter dropdowns, and inventory adjustment modal supporting Increase, Decrease, Reserve, and Release allocation actions.
- **Backend & Frontend Verification**: Frontend build passed (`ng build`), backend build passed (0 errors, 0 warnings), 64/64 backend unit tests passed, and manual browser verification completed across all routes.

---

## Technology Stack

- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture, Angular Router, RxJS)
- **Backend Framework**: C# 12 / .NET 8 (ASP.NET Core Web API)
- **Architecture**: Clean Architecture / Event-Driven Architecture (EDA) / Domain-Driven Design (DDD)
- **Database**: PostgreSQL 16 (`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.10)
- **Messaging**: Apache Kafka 3.8.0 (`Confluent.Kafka` 2.6.0)
- **Observability & Health**: ASP.NET Core Health Checks, `System.Diagnostics.Metrics`, Serilog
- **Testing**: xUnit, FluentAssertions, NSubstitute, EF Core InMemory
- **Containerization**: Docker & Docker Compose

---

## Repository Structure

```
SupplyChainX/
├── frontend/                                 # Angular Client Application (v0.8)
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                         # Models & HTTP API Services
│   │   │   ├── features/                     # Dashboard, Products, Warehouses, Inventory
│   │   │   └── layout/                       # Layout & Navigation
│   │   └── environments/                     # Environment configuration (apiBaseUrl)
│   └── package.json
├── backend/                                  # ASP.NET Core Web API Solution
│   ├── SupplyChainX.sln
│   └── src/
│       ├── SupplyChainX.Api/                 # Controllers, Middleware & Host Configuration
│       │   ├── Controllers/                  # HealthController, MetricsController, Domain Controllers
│       │   ├── Middleware/                   # CorrelationIdMiddleware, GlobalExceptionHandlingMiddleware
│       │   ├── Conventions/                  # ApiVersionRouteConvention
│       │   └── Program.cs
│       ├── SupplyChainX.Application/         # Interfaces, DTOs, Handlers & Configuration Options
│       │   ├── Common/
│       │   │   ├── Configuration/            # KafkaTopicOptions, KafkaConsumerOptions, KafkaRetryOptions
│       │   │   ├── Events/                   # Domain Event Contracts
│       │   │   ├── Interfaces/               # ISupplyChainXDbContext, IEventPublisher, IKafkaConsumerStatusService
│       │   │   └── Models/                   # KafkaConsumerStatusDto
│       │   └── EventHandlers/                # Product, Warehouse & Inventory Event Handlers
│       ├── SupplyChainX.Domain/              # Core Domain Entities & Custom Exceptions
│       │   ├── Entities/                     # Product, Warehouse, Inventory, ProcessedEvent
│       │   └── Exceptions/                   # DomainException, NotFoundException, ConflictException
│       └── SupplyChainX.Infrastructure/      # DB Context, Kafka Messaging & Health Checks
│           ├── Health/                       # KafkaHealthCheck
│           ├── Messaging/Kafka/              # KafkaEventPublisher, KafkaConsumerBackgroundService, KafkaConsumerStatusService
│           └── Persistence/                  # SupplyChainXDbContext & Migrations
├── infrastructure/                           # Container Orchestration
│   └── docker-compose.yml                    # PostgreSQL & Kafka Services
├── tests/                                    # Automated Test Suites
│   └── SupplyChainX.UnitTests/               # Unit Tests for Services, Handlers, Health & Metrics
├── LICENSE                                   # MIT License
└── README.md                                 # Project Documentation
```

---

## Development Setup & Verification

### 1. Start Infrastructure Dependencies
Ensure Docker is running, then start PostgreSQL (port `5433`) and Kafka (port `9092`):

```bash
cd infrastructure
docker compose up -d
```

### 2. Build & Test Backend Solution
Execute clean backend build and unit tests:

```bash
dotnet build backend/SupplyChainX.sln
dotnet test backend/SupplyChainX.sln --logger "console;verbosity=normal"
```

### 3. Run Backend Web API
Start the ASP.NET Core API server:

```bash
dotnet run --project backend/src/SupplyChainX.Api/SupplyChainX.Api.csproj
```

### 4. Build & Run Angular Frontend
In a separate terminal, start the Angular development server:

```bash
cd frontend
npm start
```
*Navigate to `http://localhost:4200/` in your browser to access the management UI.*

---

## Verified Invariants & Quality Standards

- **Build Quality**: Frontend build passed (`ng build`); Backend build 0 Errors, 0 Warnings.
- **Test Suite**: 64 / 64 Automated Unit Tests Passing.
- **Frontend UI & Integration**: All 4 client routes (`/dashboard`, `/products`, `/warehouses`, `/inventory`) integrated with real ASP.NET Core backend endpoints.
- **Endpoints**: `/health`, `/health/ready`, `/health/live`, `/api/v1/metrics` returning HTTP 200 OK.
- **Kafka Resilience**: Primary and DLQ topics auto-provisioned; 3 retry attempts verified prior to DLQ publish.
- **Consumer Lag**: Verified consumer lag returns to `0` across active topic partitions.
- **Idempotency**: PostgreSQL `ProcessedEvents` persistence verified with zero duplicate processing.
