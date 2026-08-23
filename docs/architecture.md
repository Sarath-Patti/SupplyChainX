# SupplyChainX Architecture Documentation

**Version**: `v0.1.0`

## High-Level Architectural Vision

SupplyChainX is designed as an enterprise-grade, event-driven modular platform built for high reliability, scalability, and strict separation of concerns.

```
+-------------------------------------------------------------------+
|                     Angular Client (Frontend)                     |
+-------------------------------------------------------------------+
                                  | HTTP / REST API
                                  v
+-------------------------------------------------------------------+
|               ASP.NET Core Web API (SupplyChainX.Api)             |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|              Application Core (SupplyChainX.Application)          |
+-------------------------------------------------------------------+
                                  |
                                  v
+-------------------------------------------------------------------+
|                Domain Model (SupplyChainX.Domain)                 |
+-------------------------------------------------------------------+
                                  ^
                                  | Implementations
+-------------------------------------------------------------------+
|             Infrastructure (SupplyChainX.Infrastructure)          |
+-------------------------------------------------------------------+
              |                                       |
              v                                       v
      PostgreSQL 16 DB                       Apache Kafka Broker
```

## Layers & Module Boundaries

1. **Domain (`SupplyChainX.Domain`)**: Core enterprise primitives (`Entity<T>`, `IAggregateRoot`). Contains zero external dependencies.
2. **Application (`SupplyChainX.Application`)**: Core application services, interfaces, DTOs, and result types.
3. **Infrastructure (`SupplyChainX.Infrastructure`)**: Persistence implementations (EF Core DbContext), message broker abstraction, and external integration adapters.
4. **API (`SupplyChainX.Api`)**: ASP.NET Core host, HTTP middleware, controllers, logging, and OpenAPI specification.

---

## Infrastructure Services

- **PostgreSQL 16**: Primary relational store for transactional data.
- **Apache Kafka**: Distributed event bus for async messaging and domain event publishing.
