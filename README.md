# SupplyChainX

**Version**: `v0.4.0`
**Milestone**: `v0.4 – Event-Driven Kafka Integration`

SupplyChainX is an enterprise inventory and order management platform built on a modern, scalable event-driven Clean Architecture.

---

## v0.4 Scope & Event-Driven Architecture

Milestone **v0.4** introduces event-driven domain integration using Apache Kafka:

1. **Clean Architecture Event Publisher (`IEventPublisher`)**:
   - `SupplyChainX.Application` defines the `IEventPublisher` interface and strongly typed event contract records (`ProductCreatedEvent`, `ProductUpdatedEvent`, `WarehouseCreatedEvent`, `WarehouseUpdatedEvent`, `InventoryAdjustedEvent`).
   - The Application layer has **zero** dependencies on `Confluent.Kafka`.

2. **Infrastructure Kafka Producer (`KafkaEventPublisher`)**:
   - Implemented in `SupplyChainX.Infrastructure/Messaging/Kafka/KafkaEventPublisher.cs` using `Confluent.Kafka.IProducer<string, string>`.
   - Serializes event payloads to JSON (camelCase policy) and publishes messages asynchronously with domain entity IDs as Kafka keys for message partitioning.

3. **PostgreSQL-First Transactional Order**:
   - Database mutations are saved to PostgreSQL (`SaveChangesAsync`) **before** publishing corresponding domain events.
   - If validation or database transactions fail, no events are published.

4. **Kafka Topics & Configuration**:
   - `supplychainx.product.events`: Product creation and update events.
   - `supplychainx.warehouse.events`: Warehouse creation and update events.
   - `supplychainx.inventory.events`: Stock increase, decrease, reserve, and release events.

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
│       ├── SupplyChainX.Application/  # Product/Warehouse/Inventory Services, IEventPublisher & Event Contracts
│       ├── SupplyChainX.Domain/       # Product, Warehouse, Inventory Entities & Domain Exceptions
│       └── SupplyChainX.Infrastructure/# DbContext, PostgreSQL Migrations & KafkaEventPublisher
├── messaging/               # Event contract documentation & schemas
├── tests/                   # Automated test suites
│   └── SupplyChainX.UnitTests/ # Unit tests with mocked IEventPublisher
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

---

## Local Event Verification via Kafka CLI

To consume and verify published events in real-time:

```bash
# Product Events
docker exec supplychainx-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic supplychainx.product.events \
  --from-beginning

# Warehouse Events
docker exec supplychainx-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic supplychainx.warehouse.events \
  --from-beginning

# Inventory Events
docker exec supplychainx-kafka kafka-console-consumer \
  --bootstrap-server localhost:9092 \
  --topic supplychainx.inventory.events \
  --from-beginning
```
