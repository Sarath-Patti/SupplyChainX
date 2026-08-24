# SupplyChainX Architecture Documentation

**Version**: `v0.5.0`
**Milestone**: `v0.5 – Kafka Event Consumers & Processing`

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
|   [IEventPublisher Interface] [Event Handlers: Product, WH, Inv]  |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|               Domain Model (SupplyChainX.Domain)                  |
|   [Product Entity] [Warehouse Entity] [Inventory Entity]          |
|   [ProcessedEvent Entity (Idempotency)] [Domain Exceptions]       |
+-------------------------------------------------------------------+
              ^                                       ^
              | Implementations                       | Implementations
+-------------------------------------------------------------------+
|             Infrastructure (SupplyChainX.Infrastructure)          |
|   [SupplyChainXDbContext] [PostgreSQL Npgsql] [IdempotencyService]|
|   [KafkaEventPublisher] [KafkaConsumerBackgroundService]          |
+-------------------------------------------------------------------+
              |                                       |
              v                                       v
      PostgreSQL 16 DB                       Apache Kafka Broker
```

---

## Event Producer & Consumer Flow

```
1. PRODUCER FLOW:
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

2. CONSUMER FLOW:
[Apache Kafka Broker] ---> [KafkaConsumerBackgroundService]
                                             |
                                             v
                                 1. Parse Event & EventId
                                             |
                                             v
                                 2. IIdempotencyService.HasBeenProcessedAsync()
                                             |
                                    [PostgreSQL 16 DB]
                                             |
                    +------------------------+------------------------+
                    | (If Duplicate)                                  | (If New Event)
                    v                                                 v
         Log Warning & Commit Offset                       3. Resolve IEventHandler<TEvent>
                                                                      |
                                                                      v
                                                           4. HandleAsync(event)
                                                                      |
                                                                      v
                                                           5. MarkAsProcessedAsync()
                                                                      |
                                                                      v
                                                           6. Manual consumer.Commit()
```

---

## Consumer Configuration & Offset Strategy

- **Consumer Group ID**: `supplychainx-event-consumers`
- **Offset Reset**: `Earliest` (ensures new consumer instances process unread messages)
- **Commit Mode**: Manual commit (`EnableAutoCommit = false`). Offsets are committed only after successful event processing and idempotency recording in PostgreSQL.
- **Idempotency Persistence**: PostgreSQL `ProcessedEvents` table (`EventId`, `EventType`, `ProcessedAtUtc`).
