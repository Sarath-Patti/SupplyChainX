# SupplyChainX Architecture Documentation

**Version**: `v0.3.0`  
**Milestone**: `v0.3 – Product, Warehouse & Inventory Domain`

## High-Level Architectural Vision

SupplyChainX is implemented as a production-quality modular monolith with strict domain boundaries and layered separation of concerns.

```
+-------------------------------------------------------------------+
|                     Angular Client (Frontend)                     |
+-------------------------------------------------------------------+
                                  | HTTP / REST API (/api/v1)
                                  v
+-------------------------------------------------------------------+
|               ASP.NET Core Web API (SupplyChainX.Api)             |
|   [ProductsController] [WarehousesController] [InventoryController]|
|   [Serilog Logging] [GlobalExceptionMiddleware] [/api/v1 Prefix]  |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|               Application Core (SupplyChainX.Application)         |
|   [ProductService] [WarehouseService] [InventoryService] [DTOs]   |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|               Domain Model (SupplyChainX.Domain)                  |
|   [Product Entity] [Warehouse Entity] [Inventory Entity]          |
|   [Domain Exceptions: DomainException, NotFoundException, etc.]   |
+-------------------------------------------------------------------+
                                  ^
                                  | Implementations
+-------------------------------------------------------------------+
|             Infrastructure (SupplyChainX.Infrastructure)          |
|   [EF Core Configurations] [DbContext] [PostgreSQL Migrations]    |
+-------------------------------------------------------------------+
              |                                       |
              v                                       v
      PostgreSQL 16 DB                       Apache Kafka Broker
```

---

## Domain & ERD Specification

```
+-------------------+             +-----------------------+             +-------------------+
|      Product      |             |       Inventory       |             |     Warehouse     |
+-------------------+             +-----------------------+             +-------------------+
| PK Id (Guid)      |<--- 1:N --->| PK Id (Guid)          |<--- N:1 --->| PK Id (Guid)      |
| Sku (Unique Index)|             | FK ProductId (Guid)   |             | Name (Index)      |
| Name              |             | FK WarehouseId (Guid) |             | Location          |
| Description       |             | AvailableQuantity     |             | IsActive          |
| UnitPrice (18,2)  |             | ReservedQuantity      |             | CreatedAtUtc      |
| IsActive          |             | MinimumStockThreshold |             | UpdatedAtUtc      |
| CreatedAtUtc      |             | Version (Concurrency) |             +-------------------+
| UpdatedAtUtc      |             | CreatedAtUtc          |
+-------------------+             | UpdatedAtUtc          |
                                  +-----------------------+
                                  | Unique Index:         |
                                  | (ProductId,WarehouseId)|
                                  +-----------------------+
```

---

## Inventory Stock Adjustment Rules

Inventory stock adjustments are executed inside the `Inventory` domain aggregate root:

1. **Stock Increase**: `AvailableQuantity += quantity`
2. **Stock Decrease**: Validates `AvailableQuantity >= quantity` and `AvailableQuantity - quantity >= ReservedQuantity`.
3. **Stock Reservation**: Validates `ReservedQuantity + quantity <= AvailableQuantity`.
4. **Reservation Release**: Validates `ReservedQuantity >= quantity`.

## Optimistic Concurrency Control

- High-frequency stock adjustments on `Inventory` records enforce EF Core optimistic concurrency control via an explicit `uint Version` property configured as a concurrency token (`.IsConcurrencyToken()`).
- Concurrent update collisions generate a `DbUpdateConcurrencyException`, caught globally to return `HTTP 409 Conflict`.
