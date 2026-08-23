# SupplyChainX

**Version**: `v0.3.0`  
**Milestone**: `v0.3 – Product, Warehouse & Inventory Domain`

SupplyChainX is an enterprise inventory and order management platform built on a modern, modular, scalable event-driven architecture.

---

## v0.3 Scope & Business Domain Capabilities

Milestone **v0.3** introduces the core business domain model for SupplyChainX:

1. **Product Domain**:
   - Management of product catalog (`Sku`, `Name`, `Description`, `UnitPrice`, `IsActive`).
   - Domain invariants: Unique SKU (case-insensitive), non-empty name, non-negative unit price (`decimal(18,2)` precision).

2. **Warehouse Domain**:
   - Management of physical/logical distribution hubs (`Name`, `Location`, `IsActive`).
   - Domain invariants: Required name and location.

3. **Inventory Domain**:
   - Multi-warehouse inventory tracking (`ProductId`, `WarehouseId`, `AvailableQuantity`, `ReservedQuantity`, `MinimumStockThreshold`).
   - Business Rules:
     - Stock increases update `AvailableQuantity`.
     - Stock decreases enforce `AvailableQuantity >= requestedQuantity` and `AvailableQuantity - requestedQuantity >= ReservedQuantity`.
     - Reservations enforce `ReservedQuantity <= AvailableQuantity`.
     - Quantities cannot become negative.
   - Foreign Keys: Product and Warehouse references with `DeleteBehavior.Restrict`.
   - Unique Composite Index: `(ProductId, WarehouseId)`.

4. **Optimistic Concurrency Strategy**:
   - `Inventory` aggregate root maintains an explicit `uint Version` concurrency token configured via EF Core `.IsConcurrencyToken()`.
   - Concurrent updates colliding on the same inventory record raise `DbUpdateConcurrencyException`, handled globally to return `HTTP 409 Conflict`.

5. **REST API Endpoints (`/api/v1`)**:
   - **Products**: `GET /api/v1/products`, `GET /api/v1/products/{id}`, `POST /api/v1/products`, `PUT /api/v1/products/{id}`, `DELETE /api/v1/products/{id}`.
   - **Warehouses**: `GET /api/v1/warehouses`, `GET /api/v1/warehouses/{id}`, `POST /api/v1/warehouses`, `PUT /api/v1/warehouses/{id}`, `DELETE /api/v1/warehouses/{id}`.
   - **Inventory**: `GET /api/v1/inventory`, `GET /api/v1/inventory/{productId}/{warehouseId}`, `POST /api/v1/inventory/adjust`.

---

## Technology Stack

- **Backend**: C# 12 / .NET 8 ASP.NET Core Web API
- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8 (`Npgsql.EntityFrameworkCore.PostgreSQL`)
- **Messaging**: Apache Kafka (KRaft mode - infrastructure container ready)
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
│       ├── SupplyChainX.Api/          # Products, Warehouses, Inventory REST Controllers & Middleware
│       ├── SupplyChainX.Application/  # Product, Warehouse, Inventory Services, DTOs & Validation
│       ├── SupplyChainX.Domain/       # Product, Warehouse, Inventory Entities & Domain Exceptions
│       └── SupplyChainX.Infrastructure/# DbContext, EF Core Configurations & PostgreSQL Migrations
├── messaging/               # Kafka schemas and event contract specifications
├── tests/                   # Automated test suites
│   └── SupplyChainX.UnitTests/ # Product, Warehouse, Inventory Domain & Service Unit Tests
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
  "version": "v0.3.0",
  "timestamp": "2026-08-23T21:03:43Z",
  "checks": [
    {
      "name": "database",
      "status": "Healthy",
      "description": "SupplyChainXDbContext reachability check"
    }
  ]
}
```
