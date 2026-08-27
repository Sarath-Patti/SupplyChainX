# SupplyChainX

**Version**: `v1.2.0`<br/>
**Milestone**: `v1.2 – Agentic AI & Model Context Protocol (MCP)`<br/>
**Status**: `v1.2 – Verified`

SupplyChainX is an enterprise inventory and order management platform built on a modern, event-driven Clean Architecture using C# / .NET 8, PostgreSQL, Apache Kafka, structured operational observability, JWT authentication, role-based authorization, Microsoft Semantic Kernel RAG & Agentic engine, Model Context Protocol (MCP) server, and an Angular 19 management dashboard with an integrated AI Copilot.

---

## Architectural Flow & Overview

```
  ┌──────────────────────────────────────────────────────────┐
  │         Angular 19 Frontend (Port 4200)                  │
  │   Dashboard / Copilot / Products / Warehouses / Auth    │
  └────────────────────────────┬─────────────────────────────┘
                               │ (REST API, JWT Bearer & X-Correlation-ID)
                               ▼
  ┌──────────────────────────────────────────────────────────┐
  │              SupplyChainX.Api (.NET 8)                   │
  │ Controllers (Domain, AI & MCP), Auth Middleware, Metrics │
  └────────────┬────────────────────────────────┬────────────┘
               │                                │
               ▼                                ▼
  ┌──────────────────────────┐    ┌──────────────────────────┐
  │ SupplyChainX.Application │    │ PostgreSQL Database (EF) │
  │ Services, DTOs & IMcp    │    │  Entities, Roles & Idem  │
  └────────────┬─────────────┘    └──────────────────────────┘
               │ (Semantic Kernel & MCP Tools)
               ▼
  ┌──────────────────────────┐
  │  Semantic Kernel Agent   │
  │   & MCP Server Layer     │
  └────────────┬─────────────┘
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

### **v0.9 — Authentication & Role-Based Authorization**
- **JWT Bearer Authentication**: User registration (`POST /api/v1/auth/register`), user login (`POST /api/v1/auth/login`), and current user profile (`GET /api/v1/auth/me`) using JWT tokens containing signed identity and role claims (`ClaimTypes.Role`).
- **Secure Password Hashing**: PBKDF2 password hashing implementation using `Microsoft.AspNetCore.Identity.PasswordHasher<User>`.
- **Role-Based Authorization Policies**:
  - **Viewer**: Read-only access to Products, Warehouses, and Inventory (`GET /api/v1/*`). Protected write operations (`POST`, `PUT`, `DELETE`) return HTTP 403 Forbidden.
  - **Operator**: Full read access plus authorized write operations for Product, Warehouse, and Inventory management (`POST`, `PUT`, `DELETE`).
  - **Admin**: Full system access, including protected operational metrics (`GET /api/v1/metrics`).
- **Protected API Endpoints**: Public access restricted to health probes (`/health`, `/health/ready`, `/health/live`). Unauthenticated requests to protected endpoints return HTTP 401 Unauthorized.
- **PostgreSQL Role Persistence**: EF Core migration (`AddAuthAndRolesTable`) creating `Users`, `Roles`, and `UserRoles` join tables with seeded roles (`Admin`, `Operator`, `Viewer`). Persisted roles verified in PostgreSQL:
  - `viewer_manual_v09` → `Viewer`
  - `operator_manual_v09` → `Operator`
  - `admin_manual_v09` → `Admin`
- **Angular Client Authentication**: Integrated `AuthService` state management, functional `authInterceptor` injecting `Authorization: Bearer` headers, `authGuard` / `roleGuard` route protection, `/login` & `/register` views, header user badge/logout button, and permission-based action control (`authService.canWrite()`).
- **Automated & Manual Verification**: Backend build passed (0 errors, 0 warnings), 82/82 backend unit tests passed, and live terminal HTTP verification confirmed 401 unauthenticated enforcement, Viewer read (200) / write (403), Operator write (201), and Admin metrics (200).

### **v1.0 — Production-Grade Frontend Authentication & User Experience**
- **Complete Angular Authentication Flow**: Implemented production-grade login (`/login`), user registration (`/register`), and unauthorized access views (`/unauthorized`) using existing backend REST APIs (`POST /api/v1/auth/login`, `POST /api/v1/auth/register`, `GET /api/v1/auth/me`).
- **JWT Session Persistence Across Refresh**: Persistent authentication state in `localStorage` with deterministic asynchronous session restoration (`initializeSession`). Upon browser refresh (F5), the Angular application automatically validates the stored JWT via `GET /api/v1/auth/me` and restores the user's profile and roles without redirecting to `/login`.
- **Angular HTTP Interceptor (`authInterceptor`)**: Centralized HTTP interceptor automatically injecting `Authorization: Bearer <token>` into all outbound protected API requests targeting the backend URL.
- **Protected Routes & Route Guards (`authGuard` & `roleGuard`)**: Protected routes (`/dashboard`, `/products`, `/warehouses`, `/inventory`) integrated with async route guards that await session initialization before evaluating access. Unauthenticated users are redirected to `/login?returnUrl=...`.
- **Role-Aware UI Action Control**:
  - **Viewer**: Full read access across all management pages. Protected write action controls (`Add`, `Edit`, `Delete`, `Adjust Stock`) are hidden via `*ngIf="authService.canWrite()"`.
  - **Operator**: Authorized write controls enabled across Product, Warehouse, and Inventory management workflows.
  - **Admin**: Full system access including protected operational metrics.
- **Centralized Error Handling (401 & 403)**:
  - **HTTP 401 Unauthorized**: Clears stale session tokens and redirects to `/login`.
  - **HTTP 403 Forbidden**: Preserves session state and redirects to `/unauthorized` without logging the user out.
- **Application Shell & User Experience**: Top header displaying active username, role badges (`Admin`, `Operator`, `Viewer`), UTC clock, system health widget, and logout action button.
- **Frontend & Backend Verification**: Angular build passed (`ng build`), backend build passed (0 errors, 0 warnings), 82/82 backend unit tests passed, and manual browser verification completed including F5 refresh persistence and logout protection.

### **v1.1 — AI Copilot, RAG & Semantic Kernel**
- **Microsoft Semantic Kernel Integration**: Added `Microsoft.SemanticKernel` (v1.30.0) as the AI orchestration engine for natural-language supply chain interactions.
- **Dedicated Authenticated AI Endpoint (`POST /api/v1/ai/chat`)**: Built a secure backend endpoint guarded by `[Authorize]` JWT authentication, processing user prompts (`ChatRequest`) and returning structured responses (`ChatResponse`) with tool execution metadata.
- **Retrieval-Augmented Generation (RAG)**: Implemented an enterprise RAG pipeline grounded in live PostgreSQL database telemetry. The engine retrieves authoritative domain facts via application services before synthesizing factual responses, preventing AI hallucination.
- **Semantic Kernel Plugin & Tools (`SupplyChainPlugin`)**: Encapsulated read-only supply chain data access into Semantic Kernel plugin functions wrapping existing application services:
  - `GetProductsAsync`: Product catalog retrieval.
  - `GetProductByIdOrSkuAsync`: Product lookups by Guid or SKU identifier.
  - `GetWarehousesAsync`: Warehouse facility capacity and status lookups.
  - `GetInventoryAsync`: Stock level and allocation distribution lookups.
  - `GetLowStockItemsAsync`: Low-stock alert detection (`AvailableQuantity <= MinimumStockThreshold`).
- **Role-Aware AI Authorization**: Preserved backend identity and RBAC boundaries (`ClaimsPrincipal`). AI tool calls enforce authenticated user permissions, ensuring zero authorization bypass.
- **Flexible AI Provider Architecture**: Configurable via `AiOptions` (`AiCopilot` section in `appsettings.json` and environment variables), supporting OpenAI completions (`gpt-4o-mini`), Azure OpenAI, or a deterministic Grounded Semantic Kernel RAG Orchestrator when external LLM credentials are absent.
- **Angular 19 AI Copilot Interface (`/copilot`)**: Standalone Angular component featuring interactive chat history, quick prompt pill triggers ("⚠️ Low Stock Alerts", "📦 Product Catalog", "🏭 Warehouse Status", "📋 Inventory Summary"), tool execution badges (`🔧 Tools Executed`), typing indicator animations, and session reset.
- **Multi-Turn Context & Error Handling**: Supports multi-turn chat history context with robust validation (HTTP 400 for empty prompts, HTTP 401 for unauthenticated requests).
- **Automated & Manual Verification**: Frontend build passed (`ng build`), backend build passed (0 errors, 0 warnings), 87/87 backend unit tests passed, live endpoint 401 unauthenticated enforcement verified, real data retrieval verified, session persistence after F5 refresh verified, and logout protection verified.

### **v1.2 — Agentic AI & Model Context Protocol (MCP)**
- **Agentic AI & Multi-Step Tool Orchestration**: Evolved the Semantic Kernel AI engine into a controlled Agentic AI Planner (`AiCopilotService`) capable of evaluating complex multi-step user prompts, dynamically planning execution sequences, and chaining multiple domain tools to synthesize grounded answers.
- **Model Context Protocol (MCP) Server**: Implemented a standard C# MCP server (`McpServerService`) using the official `ModelContextProtocol` package (`v0.1.0-preview.1.25171.12`). Exposed clean REST API endpoints (`McpController`) at `GET /api/v1/mcp/tools` (tool discovery) and `POST /api/v1/mcp/tools/call` (tool execution).
- **Exposed MCP Tools**:
  - `supplychainx_get_products`: Catalog products retrieval with pagination.
  - `supplychainx_get_warehouses`: Active warehouse facility lookups.
  - `supplychainx_get_inventory`: Inventory stock distribution lookups.
  - `supplychainx_get_low_stock`: Low-stock threshold detection (`AvailableQuantity <= MinimumStockThreshold`).
- **RBAC & MCP Security Model**: All MCP endpoints and AI tool invocations enforce JWT authentication (`[Authorize]`). Identity and roles (`ClaimsPrincipal`) propagate directly into tool handlers. Anonymous access attempts return HTTP 401 Unauthorized, while write operations over MCP remain strictly prohibited to enforce Viewer/Operator/Admin boundaries.
- **Angular Copilot Execution Trace**: Updated `CopilotComponent` to render live `🧠 Agent Execution Trace` (`AgentActivityStep`) details alongside `🔧 Tools Executed` badges, providing visual transparency for multi-step agent actions.
- **CORS / Preflight Reliability Fix**: Resolved an initial browser login CORS preflight issue (`Http failure response ... 0 Unknown Error`) by positioning `app.UseCors()` after `app.UseRouting()` in ASP.NET Core middleware pipeline and setting `policy.SetIsOriginAllowed(_ => true)`, enabling browser authentication while preserving JWT authorization.
- **Automated & Manual Verification**: Frontend build passed (`ng build`), backend build passed (`dotnet build`, 0 warnings, 0 errors), 93/93 automated backend unit tests passed, live endpoint 401 unauthenticated enforcement verified, live MCP tool discovery (`GET /api/v1/mcp/tools`) verified, live MCP tool execution (`POST /api/v1/mcp/tools/call`) verified with real PostgreSQL data, multi-tool agent execution trace (`GetLowStockItemsAsync` -> `GetWarehousesAsync`) verified, session persistence across browser refresh (F5) verified, and logout protection verified.

---

## Technology Stack

- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture, Angular Router, RxJS)
- **Backend Framework**: C# 12 / .NET 8 (ASP.NET Core Web API)
- **AI & Agentic Orchestration**: Microsoft Semantic Kernel (v1.30.0), Generative AI / LLM, RAG, Agentic AI Tool Orchestration
- **Model Context Protocol**: Model Context Protocol (MCP) Server (`ModelContextProtocol` v0.1.0-preview.1.25171.12)
- **Architecture**: Clean Architecture / Event-Driven Architecture (EDA) / Domain-Driven Design (DDD)
- **Authentication & Security**: JWT Bearer Authentication (`Microsoft.AspNetCore.Authentication.JwtBearer`), PBKDF2 Password Hashing (`PasswordHasher<User>`), Role-Based Authorization
- **Database**: PostgreSQL 16 (`Npgsql.EntityFrameworkCore.PostgreSQL` 8.0.10)
- **Messaging**: Apache Kafka 3.8.0 (`Confluent.Kafka` 2.6.0)
- **Observability & Health**: ASP.NET Core Health Checks, `System.Diagnostics.Metrics`, Serilog
- **Testing**: xUnit, FluentAssertions, NSubstitute, EF Core InMemory, Jasmine/Karma
- **Containerization**: Docker & Docker Compose

---

## Repository Structure

```
SupplyChainX/
├── frontend/                                 # Angular Client Application (v1.2)
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/                         # Models, Services, Auth Interceptor & Guards
│   │   │   │   ├── guards/                   # authGuard, roleGuard (with async initialization await)
│   │   │   │   ├── interceptors/             # authInterceptor (JWT Bearer Injection)
│   │   │   │   ├── models/                   # Auth, Product, Warehouse, Inventory & AI Models (AgentActivityStep)
│   │   │   │   └── services/                 # AuthService, ProductService, WarehouseService, InventoryService, AiService
│   │   │   ├── features/                     # Auth (Login/Register), Copilot, Dashboard, Products, Warehouses, Inventory
│   │   │   └── layout/                       # Layout & Navigation Header
│   │   └── environments/                     # Environment configuration (apiBaseUrl)
│   └── package.json
├── backend/                                  # ASP.NET Core Web API Solution
│   ├── SupplyChainX.sln
│   └── src/
│       ├── SupplyChainX.Api/                 # Controllers, Middleware & Host Configuration
│       │   ├── Controllers/                  # AuthController, AiController, McpController, HealthController, MetricsController
│       │   ├── Middleware/                   # CorrelationIdMiddleware, GlobalExceptionHandlingMiddleware
│       │   ├── Conventions/                  # ApiVersionRouteConvention
│       │   └── Program.cs
│       ├── SupplyChainX.Application/         # Interfaces, DTOs, Handlers & Configuration Options
│       │   ├── Common/
│       │   │   ├── Configuration/            # JwtOptions, AiOptions, KafkaTopicOptions, KafkaConsumerOptions, KafkaRetryOptions
│       │   │   ├── Events/                   # Domain Event Contracts
│       │   │   ├── Interfaces/               # ISupplyChainXDbContext, IAiCopilotService, IMcpServerService, IAuthService, IJwtTokenGenerator
│       │   │   └── Models/                   # KafkaConsumerStatusDto
│       │   ├── DTOs/                         # AuthDtos, AiDtos (ChatRequest, ChatResponse, AgentActivityStep), McpDtos
│       │   └── EventHandlers/                # Product, Warehouse & Inventory Event Handlers
│       ├── SupplyChainX.Domain/              # Core Domain Entities & Custom Exceptions
│       │   ├── Entities/                     # User, Role, UserRole, Product, Warehouse, Inventory, ProcessedEvent
│       │   └── Exceptions/                   # DomainException, NotFoundException, ConflictException
│       └── SupplyChainX.Infrastructure/      # DB Context, Kafka Messaging, Semantic Kernel, MCP & Health Checks
│           ├── Health/                       # KafkaHealthCheck
│           ├── Messaging/Kafka/              # KafkaEventPublisher, KafkaConsumerBackgroundService, KafkaConsumerStatusService
│           ├── Services/                     # AuthService, JwtTokenGenerator, PasswordService
│           │   ├── Ai/                       # AiCopilotService & SupplyChainPlugin (Semantic Kernel Tools)
│           │   └── Mcp/                      # McpServerService (MCP Server Tools & Call Handler)
│           └── Persistence/                  # SupplyChainXDbContext & Migrations (AddAuthAndRolesTable)
├── infrastructure/                           # Container Orchestration
│   └── docker-compose.yml                    # PostgreSQL & Kafka Services
├── tests/                                    # Automated Test Suites
│   └── SupplyChainX.UnitTests/               # Unit Tests for Services, Handlers, Auth, AI Copilot, MCP Server, Health & Metrics
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

- **Build Quality**: Frontend build passed (`ng build`); Backend build 0 Errors, 0 Warnings (`dotnet build`).
- **Test Suite**: 93 / 93 Automated Unit Tests Passing.
- **Model Context Protocol (MCP) Server**: Exposes standard tools (`supplychainx_get_products`, `supplychainx_get_warehouses`, `supplychainx_get_inventory`, `supplychainx_get_low_stock`) via `GET /api/v1/mcp/tools` and `POST /api/v1/mcp/tools/call`.
- **MCP & Agent Security**: Unauthenticated requests to `/api/v1/ai/chat` and `/api/v1/mcp/*` return HTTP 401 Unauthorized; authenticated user claims (`ClaimsPrincipal`) propagate into tool execution context.
- **Multi-Step Agent Orchestration**: Multi-tool agent execution verified (e.g. `GetLowStockItemsAsync` -> `GetWarehousesAsync`), rendering step-by-step traces (`AgentActivityStep`) in Angular Copilot UI.
- **CORS & Preflight Reliability**: Preflight `OPTIONS` routing configured using `app.UseRouting()` before `app.UseCors()`, resolving browser CORS login failures while enforcing JWT authentication.
- **Authentication & Authorization**: JWT token issuance, identity/role claims, and PBKDF2 password hashing verified.
- **Session Persistence**: Persistent session restoration verified across browser refreshes (F5) via `GET /api/v1/auth/me`.
- **Role Permissions & Enforcement**: Unauthenticated endpoints return HTTP 401; Viewer read access returns HTTP 200, write attempts return HTTP 403 Forbidden; Operator write operations return HTTP 201/200; Admin metrics returns HTTP 200 OK.
- **PostgreSQL Role Persistence**: User-to-role relationships persisted and verified in `Users`, `Roles`, and `UserRoles` database tables.
- **Kafka Resilience**: Primary and DLQ topics auto-provisioned; 3 retry attempts verified prior to DLQ publish.
- **Idempotency**: PostgreSQL `ProcessedEvents` persistence verified with zero duplicate processing.
