# SupplyChainX Architecture Documentation

**Version**: `v0.4.0`
**Milestone**: `v0.4 – Event-Driven Kafka Integration`

## High-Level Architectural Vision

SupplyChainX is implemented as a production-quality modular monolith adhering to Clean Architecture principles.

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
|   [IEventPublisher Interface] [Strongly Typed Event Contracts]   |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|               Domain Model (SupplyChainX.Domain)                  |
|   [Product Entity] [Warehouse Entity] [Inventory Entity]          |
|   [Domain Exceptions: DomainException, NotFoundException, etc.]   |
+-------------------------------------------------------------------+
              ^                                       ^
              | Implementations                       | Implementations
+-------------------------------------------------------------------+
|             Infrastructure (SupplyChainX.Infrastructure)          |
|   [SupplyChainXDbContext] [PostgreSQL Npgsql] [KafkaEventPublisher]|
+-------------------------------------------------------------------+
              |                                       |
              v                                       v
      PostgreSQL 16 DB                       Apache Kafka Broker
```

---

## Event-Driven Architecture & Producer Flow

```
[HTTP Request] ---> [Controller] ---> [Application Service]
                                             |
                                             v
                                 1. Mutate Domain Entity
                                             |
                                             v
                                 2. DbContext.SaveChangesAsync()
                                             |
                                    [PostgreSQL 16 DB]
                                             |
                                             v
                                 3. IEventPublisher.PublishAsync()
                                             |
                                             v
                                  [KafkaEventPublisher]
                                             |
                                             v
                                   [Apache Kafka Broker]
                               (supplychainx.*.events)
```

---

## Kafka Event Contracts & Topics

| Event Contract | Topic Name | Trigger |
| :--- | :--- | :--- |
| `ProductCreatedEvent` | `supplychainx.product.events` | Successful `CreateProductAsync` |
| `ProductUpdatedEvent` | `supplychainx.product.events` | Successful `UpdateProductAsync` |
| `WarehouseCreatedEvent` | `supplychainx.warehouse.events` | Successful `CreateWarehouseAsync` |
| `WarehouseUpdatedEvent` | `supplychainx.warehouse.events` | Successful `UpdateWarehouseAsync` |
| `InventoryAdjustedEvent` | `supplychainx.inventory.events` | Successful `AdjustInventoryAsync` |
