# SupplyChainX

**Version**: `v0.5.0`
**Milestone**: `v0.5 – Kafka Event Consumers & Processing`

SupplyChainX is an enterprise inventory and order management platform built on a modern, scalable event-driven Clean Architecture.

---

## v0.5 Scope & Event Consumer Architecture

Milestone **v0.5** introduces reliable, asynchronous Kafka event consumers, event processing handlers, manual offset commit strategies, and PostgreSQL-backed idempotency:

1. **Application Event Handlers (`IEventHandler<TEvent>`)**:
   - `SupplyChainX.Application` defines strongly typed handlers for domain events (`ProductCreatedEventHandler`, `ProductUpdatedEventHandler`, `ProductDeletedEventHandler`, `WarehouseCreatedEventHandler`, `WarehouseUpdatedEventHandler`, `WarehouseDeletedEventHandler`, `InventoryAdjustedEventHandler`).
   - Handlers validate payloads and execute domain processing with structured logging.

2. **Hosted Kafka Consumer Service (`KafkaConsumerBackgroundService`)**:
   - Implemented in `SupplyChainX.Infrastructure/Messaging/Kafka/KafkaConsumerBackgroundService.cs` as a hosted `BackgroundService`.
   - Subscribes to `supplychainx.product.events`, `supplychainx.warehouse.events`, and `supplychainx.inventory.events` using consumer group `supplychainx-event-consumers`.
   - Executes background message consumption without blocking ASP.NET Core startup.

3. **Manual Offset Commit Strategy**:
   - `EnableAutoCommit` is set to `false`.
   - Offsets are committed manually via `consumer.Commit(result)` **only after** an event is successfully processed by its handler and recorded in PostgreSQL.

4. **PostgreSQL Idempotency & Duplicate Prevention**:
   - Persists processed event IDs in the PostgreSQL `ProcessedEvents` table (`EventId`, `EventType`, `ProcessedAtUtc`).
   - If a duplicate event is re-delivered, the consumer logs the duplicate warning, commits the offset to advance Kafka, and skips re-execution.

5. **Retry Policy & Resilience**:
   - Transient failures are retried up to `MaxRetryAttempts` (configurable).
   - Malformed payloads log structured errors and commit offsets to avoid queue deadlocks.

---

## Technology Stack

- **Backend**: C# 12 / .NET 8 ASP.NET Core Web API
- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture)
- **Database**: PostgreSQL 16 (EF Core 8 `Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Messaging**: Apache Kafka 3.8.0 (KRaft mode via `Confluent.Kafka` 2.6.0)
- **Authentication**: JWT (Deferred)
- **Testing**: xUnit (`FluentAssertions`, `NSubstitute`, `EF Core InMemory`)
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
│       ├── SupplyChainX.Api/          # REST Controllers, AppSettings & Middleware
│       ├── SupplyChainX.Application/  # Product/Warehouse/Inventory Services, Event Handlers & Contracts
│       ├── SupplyChainX.Domain/       # Domain Entities (Product, Warehouse, Inventory, ProcessedEvent)
│       └── SupplyChainX.Infrastructure/# DbContext, Migrations, KafkaEventPublisher & KafkaConsumerBackgroundService
├── messaging/               # Event contract documentation & schemas
├── tests/                   # Automated test suites
│   └── SupplyChainX.UnitTests/ # Unit tests for Services, Handlers & Idempotency
├── docs/                    # Architecture and developer documentation
├── infrastructure/          # Container orchestration (Docker Compose)
│   ├── docker-compose.yml
│   └── .env.example
├── LICENSE                  # MIT License
└── README.md                # Project documentation
```

---

## Infrastructure Setup

Start development infrastructure (PostgreSQL on port `5433`, Kafka on port `9092`):

```bash
cd infrastructure
cp .env.example .env
docker compose up -d
```
