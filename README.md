# SupplyChainX

**Version**: `v2.0.0`<br/>
**Milestone**: `v2.0 — Process Analytics Engine`<br/>
**Status**: `v2.0 Process Analytics Engine – Verified`

SupplyChainX is a production-grade, event-driven enterprise inventory and order management platform built on C# / .NET 8, Java 21 / Spring Boot 3, PostgreSQL, Apache Kafka, Microsoft Semantic Kernel, Model Context Protocol (MCP), Angular 19, and Kubernetes (`kind`). It demonstrates modern polyglot distributed microservices architecture, process analytics engine capabilities (cycle time, stage durations, throughput, and deterministic bottleneck analysis), reliable event processing with application-level idempotency, grounded Retrieval-Augmented Generation (RAG), multi-step agentic AI tool orchestration, role-based operational security, cloud-native container orchestration, consumer auto-scaling capacity, event-driven backpressure recovery, and empirically verified fault tolerance across distributed failure scenarios.

---

## Why SupplyChainX?

Modern supply chain systems demand high availability, data consistency across asynchronous microservices, real-time telemetry, and intelligent decision support. SupplyChainX was engineered from the ground up to solve complex distributed backend challenges:
- **Asynchronous Event Processing**: Decoupling transactional write operations from downstream processing using Apache Kafka.
- **Process Analytics Engine**: Transform historical domain events into measurable cycle times, stage breakdowns, process throughput, and stage bottleneck contributions.
- **Resilient Message Semantics**: Guaranteeing application-level idempotency and dead-letter queue (DLQ) isolation without data loss.
- **Polyglot Microservices Architecture**: Combining C# .NET 8 core domain services with Java Spring Boot 3 process analytics microservice.
- **Grounded Enterprise AI**: Combining LLMs and Semantic Kernel with real-time PostgreSQL database state to deliver zero-hallucination AI Copilot and MCP tools.
- **Production-Grade Observability & Security**: End-to-end correlation ID propagation, structured logging, real-time health/metrics probes, and strict role-based access control (RBAC).

---

## What This Project Demonstrates

- **Distributed Event-Driven Architecture**: Asynchronous domain event publishing and consumption via Apache Kafka (`Confluent.Kafka`).
- **Polyglot Microservice Architecture**: Java Spring Boot 3 process analytics microservice (`process-service`) working alongside ASP.NET Core API (`backend`) over shared PostgreSQL infrastructure.
- **Process Analytics Engine**: Cycle-time analysis, stage duration breakdown, completed-process throughput calculations, and deterministic bottleneck detection.
- **Application-Level Idempotency**: Deduplication using a durable PostgreSQL `ProcessedEvents` store to safely handle duplicate message delivery.
- **Fault-Tolerant Message Handling**: Retry loops with exponential backoff, malformed message isolation, and Dead Letter Queue (DLQ) routing.
- **Enterprise AI & RAG Orchestration**: Microsoft Semantic Kernel RAG engine grounded in live domain facts to prevent AI hallucinations.
- **Agentic AI & Model Context Protocol (MCP)**: Dynamic multi-step tool planning, visual execution traces, and standardized C# MCP server REST endpoints.
- **Production AI Provider Integration**: Strongly typed configuration support for Azure OpenAI and OpenAI completions with local fallback.
- **Role-Based Access Control (RBAC)**: Fine-grained JWT authentication enforcing `Admin`, `Operator`, and `Viewer` policies across API and AI boundaries.
- **Kubernetes & Cloud-Native Deployment**: Declarative K8s manifests (`Deployments`, `Services`, `ConfigMaps`, `Secrets`, `PVC`), Nginx same-origin reverse proxy, readiness/liveness probes, bounded Kafka JVM memory, and horizontal pod scaling.
- **Kafka Consumer Scaling & Backpressure**: Repeatable event workload harness (`IKafkaBenchmarkService`, `BenchmarkController`), real-time consumer lag metrics, partition-to-consumer scaling analysis (1, 2, 3 consumers), and 150-event backpressure burst recovery.
- **Distributed Failure & Recovery Validation**: Empirically verified system resilience across 6 real-world failure scenarios in Kubernetes (Kafka outage, consumer pod crash/rebalance, PostgreSQL database outage, duplicate event delivery, poison payload retry/DLQ routing, and backend service rolling restart).
- **End-to-End Tracing & Telemetry**: `X-Correlation-ID` header propagation across HTTP requests, domain events, Serilog context, and background workers.
- **Modern Angular Frontend**: Standalone component architecture with async session restoration, role-aware UI controls, and live telemetry dashboards.

---

## Engineering Highlights

- **Clean Architecture & DDD**: Strict layer separation (`Domain`, `Application`, `Infrastructure`, `Api`) protecting business invariants.
- **Non-Blocking Background Workers**: Hosted `.NET BackgroundService` consuming Kafka messages independently of API HTTP request threads.
- **Manual Offset Commit Control**: Offsets are committed only after successful PostgreSQL processing or verified DLQ publication.
- **Safe RFC 7807 Error Responses**: Centralized exception handling returning sanitized `ProblemDetails` with correlation IDs while shielding database schemas and secrets.

---

## Technology Stack

### Backend & Microservices
- **Core Web API**: .NET 8 (C# 12) / ASP.NET Core Web API (`backend/`)
- **Process Analytics Service**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / Hibernate (`services/process-service/`)
- **Persistence**: Entity Framework Core 8, PostgreSQL 16 (Npgsql & PostgreSQL JDBC providers)
- **Event Bus & Messaging**: Apache Kafka (`Confluent.Kafka` v2.6+), hosted `.NET BackgroundService`
- **AI & RAG**: Microsoft Semantic Kernel v1.30+, Azure OpenAI SDK, Model Context Protocol (MCP) C# SDK
- **Security**: JWT Bearer Authentication (`System.IdentityModel.Tokens.Jwt`), ASP.NET Core Authorization Policies
- **Logging & Monitoring**: Serilog, Spring Boot Actuator, ASP.NET Core Health Checks (`AspNetCore.HealthChecks.NpgSql`)

### Frontend
- **Framework**: Angular 19 (TypeScript 5.6+)
- **Architecture**: Standalone Components, Reactive Forms (`RxJS`), Modular Feature Routing
- **HTTP Client**: Angular `HttpClient` with Functional `authInterceptor`
- **UI Components & Icons**: Custom Scoped Glassmorphism Theme System, Lucide Angular Icons

### Infrastructure & Cloud-Native
- **Containerization**: Docker Multi-Stage Dockerfiles for API and SPA
- **Kubernetes**: `kind` (Kubernetes in Docker), Declarative YAML (`Deployments`, `Services`, `ConfigMaps`, `Secrets`, `PVC`)
- **Reverse Proxy**: Nginx (same-origin SPA asset server and API reverse proxy)

---

## Milestone History

- **v0.7 — Observability & Operational Monitoring**: Health check probes, operational metrics endpoint, Serilog correlation ID middleware, thread-safe counters.
- **v0.8 — Angular Frontend & Operations Dashboard**: Standalone Angular 19 SPA, CRUD management views for Products, Warehouses, Inventory, and Telemetry Dashboard.
- **v0.9 — Authentication & Role-Based Authorization**: JWT authentication, PBKDF2 password hashing, PostgreSQL identity tables, and RBAC policies (`Admin`, `Operator`, `Viewer`).
- **v1.0 — Production-Grade Frontend Authentication & User Experience**: Angular session persistence across F5 refresh, HTTP `authInterceptor`, route guards (`authGuard`, `roleGuard`), and 401/403 error handling.
- **v1.1 — AI Copilot, RAG & Semantic Kernel**: Microsoft Semantic Kernel engine, RAG pipeline grounded in live domain facts, authenticated `POST /api/v1/ai/chat` endpoint, and Angular `/copilot` chat interface.
- **v1.2 — Agentic AI & Model Context Protocol (MCP)**: Multi-step agentic tool planner, C# MCP server (`GET /api/v1/mcp/tools`, `POST /api/v1/mcp/tools/call`), and execution trace rendering.
- **v1.3 — Azure OpenAI Integration & Production AI Configuration**: Configurable Azure OpenAI LLM provider integration, strongly typed `AiOptions` validation, and public CORS optimization.
- **v1.4 — Production Hardening & Version Consistency**: Version display alignment across API/UI (`v1.3.0`), correlation ID attachment to RFC 7807 `ProblemDetails`, and secret shielding.
- **v1.5 — Event-Driven Supply Chain Workflows & Reliability**: Domain event models, reliable Kafka producer/consumer background service, PostgreSQL application-level idempotency (`ProcessedEvents`), retry backoff, DLQ routing, and correlation ID tracing.
- **v1.6 — Kubernetes & Cloud-Native Deployment**: Dockerized ASP.NET Core API and Angular SPA, declarative Kubernetes manifests (`namespace`, `Deployments`, `Services`, `ConfigMaps`, `Secrets`, `PVC`), PostgreSQL persistence, Apache Kafka KRaft deployment with JVM heap limits, Nginx same-origin reverse proxying, readiness/liveness probes, rolling updates, service discovery, and horizontal pod scaling.
- **v1.7 — Kafka Consumer Scaling & Event-Driven Backpressure**: Repeatable domain event workload harness (`IKafkaBenchmarkService`, `BenchmarkController`), real-time consumer lag tracking (`GET /api/v1/benchmark/lag`), partition assignment analysis across Kubernetes replicas, backpressure burst validation (150 events, 132 peak lag, 100% backlog recovery), and 100/100 passing unit tests.
- **v1.8 — Distributed Failure & Recovery Validation**: Empirically verified failure and recovery matrix across 6 real-world scenarios in Kubernetes (Kafka broker outage, consumer pod crash/rebalance, PostgreSQL database outage, duplicate event idempotency deduplication, poison event retry/DLQ routing, backend service rolling restart), 102/102 passing unit tests.
- **v1.9 — Spring Boot Process Analytics Service**: Polyglot Java 21 / Spring Boot 3 microservice (`services/process-service/`). Phase 1 established Spring Data JPA / Hibernate persistence (`ProcessInstance`, `ProcessEvent`, `ProcessStep`), REST API foundation (`/api/v1/processes`), and Actuator health checks (10/10 tests). Phase 2 implemented Spring Kafka domain event integration (`supplychainx.product.events`, `supplychainx.warehouse.events`, `supplychainx.inventory.events`), automatic business key extraction, process lifecycle state transitions, step generation, application & database idempotency (`existsByEventId` and unique database constraint), manual offset acknowledgment (`MANUAL_IMMEDIATE`), and comprehensive unit/integration test coverage (24/24 Java tests passed, 102/102 C# tests passed).
- **v2.0 — Process Analytics Engine**: End-to-end process analytics layer transforming persisted process event history into actionable metrics. Implemented process cycle time analysis, stage duration breakdown, completed-process throughput tracking, and deterministic stage bottleneck contribution analysis (`processTimeContribution`). Optimized PostgreSQL query execution with JPA entity indexing (`started_at`, `completed_at`, `step_name`, `process_type`) and dynamic Spring Data `JpaSpecificationExecutor` specifications. Added REST analytics endpoints (`GET /api/v1/analytics/processes/{id}`, `/processes/{id}/stages`, `/summary`, `/throughput`, `/bottlenecks`), 37/37 passing Java tests, 102/102 passing C# tests, and real E2E verification across ASP.NET Core API → Kafka → Spring Boot `process-service` → PostgreSQL → Analytics API.

---

## Repository Structure

```
SupplyChainX/
├── frontend/                                 # Angular 19 SPA Client Application
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                         # Auth Services, Interceptor, Guards & Models
│   │   │   ├── features/                     # Auth, Copilot, Dashboard, Products, Warehouses, Inventory
│   │   │   └── layout/                       # App Shell Header & Navigation
│   └── package.json
├── backend/                                  # ASP.NET Core Web API Solution (.NET 8)
│   ├── SupplyChainX.sln
│   └── src/
│       ├── SupplyChainX.Api/                 # Controllers (Auth, Products, Warehouses, Inventory, AI, MCP, Benchmark, Health, Metrics) & Middleware
│       ├── SupplyChainX.Application/         # DTOs, Event Contracts, Interfaces & Service Boundaries
│       ├── SupplyChainX.Domain/              # Domain Entities (User, Role, Product, Warehouse, Inventory, ProcessedEvent) & Exceptions
│       └── SupplyChainX.Infrastructure/      # EF Core DbContext, Kafka Producer/Consumer, Benchmark Service, Semantic Kernel, MCP & Health Checks
├── services/                                 # Polyglot Microservices
│   └── process-service/                      # Java 21 / Spring Boot 3 Process Analytics Microservice
│       ├── pom.xml                           # Maven Dependencies & Plugins
│       ├── README.md                         # Process Service Architecture & API Guide
│       └── src/                              # Controllers, Services, Repositories, Entities & Tests
├── infrastructure/                           # Container Orchestration (PostgreSQL & Kafka Docker Compose & K8s)
├── tests/                                    # Automated Unit & Integration Test Suites
│   └── SupplyChainX.UnitTests/
├── LICENSE
└── README.md
```
