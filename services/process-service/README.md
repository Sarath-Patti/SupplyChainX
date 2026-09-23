# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v2.4 — Enterprise Security & Observability`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Security 6 / Spring Data JPA / Spring Kafka / PostgreSQL / Micrometer / SLF4J

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, step-level tracking, deterministic bottleneck detection, process variant analysis, rework detection, process conformance analysis, enterprise security (Spring Security + JWT + RBAC), and production observability for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server, JWT Token Generation).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics engine, stateless JWT authentication, RBAC authorization (`ADMIN`, `ANALYST`, `USER`), request correlation tracing (`X-Correlation-ID` & MDC context), Spring Boot Actuator health/metrics probes, and event processing.
- **PostgreSQL**: Shared relational storage with indexed tables (`process_instances`, `process_events`, `process_steps`).
- **Apache Kafka**: Primary domain event bus consumed asynchronously by `process-service`.

```text
HTTP Client / API Request (Header: Authorization Bearer JWT, X-Correlation-ID)
  ↓
CorrelationIdFilter (MDC Population & Response Header Attachment)
  ↓
JwtAuthenticationFilter (HMAC-SHA256 Token Validation & Role Mapping)
  ↓
Spring Security 6 (Authorization Checks: ADMIN, ANALYST, USER)
  ↓
Process Analytics Controllers & Micrometer Metrics Registry
  ↓
PostgreSQL (Indexed Relational Storage)
```

---

## Directory Structure

```text
services/process-service/
├── pom.xml
├── README.md
└── src/
    ├── main/
    │   ├── java/com/supplychainx/processservice/
    │   │   ├── ProcessServiceApplication.java       # Spring Boot Application Entrypoint
    │   │   ├── security/                            # Enterprise Security & Correlation Layer
    │   │   │   ├── SecurityConfig.java              # Spring Security 6 FilterChain & Authorization Rules
    │   │   │   ├── JwtTokenProvider.java            # JWT Signature Validation & Role Authority Extractor
    │   │   │   ├── JwtAuthenticationFilter.java     # Stateless Bearer Token Interceptor
    │   │   │   ├── CorrelationIdFilter.java         # Request Correlation Header & MDC Context Filter
    │   │   │   ├── CustomAuthenticationEntryPoint.java # Standardized 401 Unauthorized Handler
    │   │   │   └── CustomAccessDeniedHandler.java   # Standardized 403 Forbidden Handler
    │   │   ├── metrics/                             # Application Metrics & Micrometer Layer
    │   │   │   └── ProcessAnalyticsMetrics.java     # Low-Cardinality Counter & Timer Instruments
    │   │   ├── analytics/                           # Process Analytics & Conformance Engine Layer
    │   │   │   ├── ProcessAnalyticsController.java  # REST Controller (/api/v1/analytics/*)
    │   │   │   ├── ProcessAnalyticsService.java     # Metrics, Variant, Rework & Conformance Logic
    │   │   │   ├── ProcessAnalyticsRepository.java  # Custom Specification & Aggregation Queries
    │   │   │   └── dto/
    │   │   │       ├── ProcessMetricsResponse.java
    │   │   │       ├── StageMetricsResponse.java
    │   │   │       ├── ProcessAnalyticsSummaryResponse.java
    │   │   │       ├── ThroughputResponse.java
    │   │   │       ├── BottleneckResponse.java
    │   │   │       ├── ProcessVariantResponse.java
    │   │   │       ├── ActivityReworkResponse.java
    │   │   │       ├── ReworkAnalyticsSummaryResponse.java
    │   │   │       ├── ProcessReworkDetailResponse.java
    │   │   │       ├── ProcessConformanceResponse.java
    │   │   │       ├── ConformanceAnalyticsSummaryResponse.java
    │   │   │       ├── ProcessDeviationDto.java
    │   │   │       └── StageDurationStats.java
    │   │   ├── controller/
    │   │   │   └── ProcessController.java            # Process Instance CRUD APIs (/api/v1/processes)
    │   │   ├── service/
    │   │   │   └── ProcessService.java               # State Transitions, Ingestion & Idempotency
    │   │   ├── kafka/
    │   │   │   ├── ProcessEventConsumer.java         # @KafkaListener Consumer with MDC & Observability
    │   │   │   ├── KafkaEventMapper.java             # Maps JSON Events to Entities & Steps
    │   │   │   ├── KafkaConsumerConfig.java          # Spring Kafka Ack Configuration
    │   │   │   └── model/
    │   │   │       └── SupplyChainXDomainEventDto.java
    │   │   ├── repository/
    │   │   │   ├── ProcessInstanceRepository.java
    │   │   │   ├── ProcessEventRepository.java
    │   │   │   └── ProcessStepRepository.java
    │   │   ├── entity/
    │   │   │   ├── ProcessInstance.java
    │   │   │   ├── ProcessEvent.java
    │   │   │   └── ProcessStep.java
    │   │   ├── dto/
    │   │   │   ├── ProcessInstanceResponseDto.java
    │   │   │   ├── ProcessInstanceDetailResponseDto.java
    │   │   │   ├── ProcessEventResponseDto.java
    │   │   │   ├── ProcessStepResponseDto.java
    │   │   │   └── ErrorResponseDto.java            # Standardized RFC-Compliant Error Response
    │   │   └── exception/
    │   │       ├── ResourceNotFoundException.java
    │   │       └── GlobalExceptionHandler.java      # Centralized HTTP Exception Handler
    │   └── resources/
    │       ├── application.yml                      # Production / PostgreSQL, Kafka, JWT & Logging Config
    │       └── application-test.yml                 # Fast In-Memory H2 Testing Config
    └── test/
        └── java/com/supplychainx/processservice/
            ├── security/
            │   └── SecurityAndObservabilityTests.java # 20 Comprehensive Security & Observability Tests
            ├── analytics/
            │   ├── ProcessAnalyticsServiceTests.java
            │   └── ProcessAnalyticsControllerTests.java
            ├── repository/
            │   └── ProcessRepositoryTests.java
            ├── controller/
            │   └── ProcessControllerTests.java
            ├── service/
            │   └── ProcessServiceTests.java
            └── kafka/
                ├── KafkaEventMapperTests.java
                └── ProcessEventConsumerTests.java
```

---

## v2.4 Enterprise Security & Observability Specifications

### 1. Authentication & Role-Based Access Control (RBAC)
- **Token Validation**: Stateless HMAC-SHA256 JWT validation matching C# backend secret key (`jwt.secret`), issuer (`SupplyChainX`), and audience (`SupplyChainXClients`).
- **Role Mapping**:
  - `Admin` / `ADMIN` / `ROLE_ADMIN` $\rightarrow$ `ROLE_ADMIN`
  - `Operator` / `ANALYST` / `ROLE_ANALYST` $\rightarrow$ `ROLE_ANALYST`
  - `Viewer` / `USER` / `ROLE_USER` $\rightarrow$ `ROLE_USER`
- **Access Control Matrix**:
  - Actuator Probes (`/actuator/health`, `/actuator/health/readiness`, `/actuator/health/liveness`, `/actuator/metrics`, `/actuator/prometheus`): `permitAll()`
  - Analytics & Process APIs (`/api/v1/analytics/**`, `/api/v1/processes/**`): Authorized roles `ADMIN`, `ANALYST`, `USER`
  - Admin Operations (`/api/v1/analytics/admin/**`, `/api/v1/admin/**`): `hasRole('ADMIN')`
  - Unauthenticated requests: `401 Unauthorized`
  - Insufficient privileges: `403 Forbidden`

### 2. Request Correlation & Structured Logging
- **`CorrelationIdFilter`**: Extracts `X-Correlation-ID` header if present; generates UUID if missing; attaches `X-Correlation-ID` header to HTTP response.
- **SLF4J MDC Context**: Sets `correlationId`, `processId`, and `eventId` in ThreadLocal MDC. Context is cleared in a `finally` block to prevent leaks.
- **Log Pattern**:
  `%d{yyyy-MM-dd HH:mm:ss.SSS} [%thread] %-5level %logger{36} [service=process-service, correlationId=%X{correlationId:-none}, processId=%X{processId:-none}, eventId=%X{eventId:-none}] - %msg%n`
- **Security**: Raw tokens, passwords, JWT secrets, and `Authorization` headers are never logged.

### 3. Global Error Handling
Standardized error DTO schema returned across all exception types:
```json
{
  "timestamp": "2026-09-23T04:23:19.736Z",
  "status": 401,
  "error": "Unauthorized",
  "message": "Full authentication is required to access this resource",
  "path": "/api/v1/analytics/summary",
  "correlationId": "4f322224-d0cf-488a-9b50-afd63b177970"
}
```

### 4. Actuator Health & Micrometer Metrics
- **Health Probes**: Exposed at `/actuator/health/liveness` and `/actuator/health/readiness` without exposing credentials.
- **Metrics Instruments**:
  - `analytics.requests.total` (tags: `endpoint`, `method`, `status`)
  - `analytics.conformance.requests.total` (tags: `status`)
  - `analytics.variants.requests.total` (tags: `status`)
  - `analytics.rework.requests.total` (tags: `status`)
  - `process.event.processed.total` (tags: `topic`, `eventType`, `status`)
  - `process.event.failures.total` (tags: `eventType`, `errorType`)
  - `kafka.consumer.activity.total` (tags: `topic`, `status`)

---

## API Endpoints & Authorization Rules

| Method | Endpoint | Description | Authorization Requirement |
| :--- | :--- | :--- | :--- |
| `GET` | `/actuator/health` | Application health probe | Unprotected (`permitAll()`) |
| `GET` | `/actuator/health/readiness` | Kubernetes readiness probe | Unprotected (`permitAll()`) |
| `GET` | `/actuator/health/liveness` | Kubernetes liveness probe | Unprotected (`permitAll()`) |
| `GET` | `/actuator/metrics` | Micrometer metrics index | Unprotected (`permitAll()`) |
| `GET` | `/actuator/prometheus` | Prometheus metrics export | Unprotected (`permitAll()`) |
| `GET` | `/api/v1/analytics/summary` | Aggregate analytics summary | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/throughput` | Process throughput metrics | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/bottlenecks` | Bottleneck analysis | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/variants` | Process variant analysis | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/variants/{variantKey}` | Variant detail by key | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/rework` | Aggregate rework summary | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/rework/{processId}` | Instance rework detail | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/conformance` | Aggregate conformance summary | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/conformance/{processId}` | Instance conformance detail | `ADMIN`, `ANALYST`, `USER` |
| `GET` | `/api/v1/analytics/admin/summary` | System administrative summary | `ADMIN` only |

---

## Build, Test & Run

### 1. Build and Run Full Test Suite
```bash
export PATH="/opt/homebrew/opt/openjdk/bin:/opt/homebrew/bin:$PATH"
cd services/process-service
mvn clean test
```

### 2. Package Application JAR
```bash
mvn clean package
```

### 3. Run Service Locally
```bash
export DB_HOST=localhost
export DB_PORT=5433
export DB_NAME=supplychainx_db
export DB_USERNAME=postgres
export DB_PASSWORD=postgres_dev_password
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092

java -jar target/process-service-1.0.0-SNAPSHOT.jar --server.port=8081
```

---

## Verification & Benchmark Results

- **Java Unit & Integration Test Suite**: `70/70 passed` (20 new security & observability tests covering authentication 401, authorization 403, admin access 200, JWT validation, correlation ID header propagation, MDC context, health probes, validation, error mapping, and Actuator metrics).
- **C# ASP.NET Core Regression Suite**: `102/102 passed`.
- **E2E Environment Verification**:
  - Unauthenticated Request (`/api/v1/analytics/summary`): Returned `401 Unauthorized` with `X-Correlation-ID` header and standardized JSON body.
  - Authenticated Request with C# JWT Token: Returned `200 OK` with full process analytics data.
  - Admin Endpoint Request (`/api/v1/analytics/admin/summary`): Returned `200 OK` for `Admin` role; returned `403 Forbidden` for non-admin tokens.
  - Health & Readiness Probes (`/actuator/health/readiness`, `/liveness`): Returned `200 OK` with `status: UP` without credentials.
  - Actuator Metrics (`/actuator/metrics/analytics.requests.total`): Verified counter increment and low-cardinality tags.
- **Latency / Performance**: Observed local latency for authenticated endpoints measured $\approx 13.7\text{ ms} - 15.3\text{ ms}$ per request.
- **Security Audit**: Zero hardcoded secrets, zero raw JWT/authorization header logging, zero stack trace leakage in API responses.
tion 4).
- **Aggregate Summary Verification**:
  - `totalProcessesAnalyzed`: 12
  - `conformantProcessCount`: 5
  - `deviatedProcessCount`: 7
  - `conformanceRate`: 41.67%
  - `averageConformanceScore`: 0.95
  - `totalDeviationCount`: 9 (`MISSING_ACTIVITY`: 2, `UNEXPECTED_ACTIVITY`: 4, `ORDER_VIOLATION`: 1, `TERMINAL_ACTIVITY_VIOLATION`: 2).
- **Latency / Performance**: API response latency measured $\approx 15\text{ ms} - 47\text{ ms}$ on local test dataset. *Local dataset size is limited; production-scale p95/p99 latency should be evaluated under full load.*
- **Limitations**: Deterministic, rule-based structural conformance algorithm inspired by process mining concepts. Does not use ML, fuzzy matching, or statistical token-based fitness alignment.





