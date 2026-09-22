# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v2.3 — Process Conformance Analysis`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / Spring Kafka / PostgreSQL

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, step-level tracking, deterministic bottleneck detection, process variant analysis, rework detection, and process conformance analysis for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics engine, workflow state reconstruction, step execution tracking, process variant discovery, rework analysis, deterministic process conformance analysis, and historical event persistence.
- **PostgreSQL**: Shared relational storage with indexed tables (`process_instances`, `process_events`, `process_steps`).
- **Apache Kafka**: Primary domain event bus consumed asynchronously by `process-service`.

```text
Kafka (supplychainx.*.events)
  ↓
ProcessEventConsumer (@KafkaListener)
  ↓
ProcessService (State Reconstruction & Idempotent Persistence)
  ↓
PostgreSQL (Indexed Relational Storage)
  ↓
Process Analytics Engine (ProcessAnalyticsService)
  ↓
REST API (/api/v1/analytics/*)
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
    │   │   ├── analytics/                           # Process Analytics & Conformance Engine Layer
    │   │   │   ├── ProcessAnalyticsController.java  # REST Controller (/api/v1/analytics/*)
    │   │   │   ├── ProcessAnalyticsService.java     # Metrics, Variant, Rework & Conformance Logic
    │   │   │   ├── ProcessAnalyticsRepository.java  # Custom Specification & Aggregation Queries
    │   │   │   └── dto/
    │   │   │       ├── ProcessMetricsResponse.java  # Single Process Instance Analytics DTO
    │   │   │       ├── StageMetricsResponse.java    # Stage Breakdown DTO
    │   │   │       ├── ProcessAnalyticsSummaryResponse.java # Aggregate Summary DTO
    │   │   │       ├── ThroughputResponse.java       # Process Throughput Metrics DTO
    │   │   │       ├── BottleneckResponse.java       # Stage Bottleneck Metrics DTO
    │   │   │       ├── ProcessVariantResponse.java   # Process Variant Analysis DTO
    │   │   │       ├── ActivityReworkResponse.java   # Activity-level Rework Analysis DTO
    │   │   │       ├── ReworkAnalyticsSummaryResponse.java # Rework Summary DTO
    │   │   │       ├── ProcessReworkDetailResponse.java   # Single Process Instance Rework DTO
    │   │   │       ├── ProcessConformanceResponse.java    # Process Conformance Detail DTO
    │   │   │       ├── ConformanceAnalyticsSummaryResponse.java # Aggregate Conformance Summary DTO
    │   │   │       ├── ProcessDeviationDto.java      # Deviation Detail DTO
    │   │   │       └── StageDurationStats.java       # Repository Projection Record
    │   │   ├── controller/
    │   │   │   └── ProcessController.java            # Process Instance CRUD APIs (/api/v1/processes)
    │   │   ├── service/
    │   │   │   └── ProcessService.java               # State Transitions, Ingestion & Idempotency
    │   │   ├── kafka/
    │   │   │   ├── ProcessEventConsumer.java         # @KafkaListener Consumer for Domain Events
    │   │   │   ├── KafkaEventMapper.java             # Maps JSON Events to Entities & Steps
    │   │   │   ├── KafkaConsumerConfig.java          # Spring Kafka Ack Configuration
    │   │   │   └── model/
    │   │   │       └── SupplyChainXDomainEventDto.java # Jackson DTO for Domain Event Payloads
    │   │   ├── repository/
    │   │   │   ├── ProcessInstanceRepository.java   # Spring Data JPA Repository for ProcessInstance
    │   │   │   ├── ProcessEventRepository.java      # Spring Data JPA Repository for ProcessEvent
    │   │   │   └── ProcessStepRepository.java       # Spring Data JPA Repository for ProcessStep
    │   │   ├── entity/
    │   │   │   ├── ProcessInstance.java             # Indexed Process Instance Entity
    │   │   │   ├── ProcessEvent.java                # Indexed Process Event Entity
    │   │   │   └── ProcessStep.java                 # Indexed Process Step Entity
    │   │   ├── dto/
    │   │   │   ├── ProcessInstanceResponseDto.java
    │   │   │   ├── ProcessInstanceDetailResponseDto.java
    │   │   │   ├── ProcessEventResponseDto.java
    │   │   │   ├── ProcessStepResponseDto.java
    │   │   │   └── ErrorResponseDto.java
    │   │   └── exception/
    │   │       ├── ResourceNotFoundException.java
    │   │       └── GlobalExceptionHandler.java      # Centralized HTTP Exception Handler
    │   └── resources/
    │       ├── application.yml                      # Production / PostgreSQL & Kafka Config
    │       └── application-test.yml                 # Fast In-Memory H2 Testing Config
    └── test/
        └── java/com/supplychainx/processservice/
            ├── analytics/                           # Analytics Unit & Controller Tests
            │   ├── ProcessAnalyticsServiceTests.java # Analytics Unit Tests (Metrics, Variants, Rework & Conformance)
            │   └── ProcessAnalyticsControllerTests.java # WebMvcTest Controller Tests
            ├── repository/
            │   └── ProcessRepositoryTests.java      # DataJpaTest for Entity Persistence & Queries
            ├── controller/
            │   └── ProcessControllerTests.java      # WebMvcTest Controller Tests
            ├── service/
            │   └── ProcessServiceTests.java         # Integration Tests for Event Ingestion
            └── kafka/
                ├── KafkaEventMapperTests.java       # Unit Tests for Mapping Logic & Steps
                └── ProcessEventConsumerTests.java   # Listener Execution Tests
```

---

## v2.3 Process Conformance Analysis Engine

### 1. Definition & Expected Process Paths
Process conformance analysis compares actual chronological process executions against expected normative process reference models.
- **PRODUCT_LIFECYCLE Expected Path**:
  `PRODUCT_CREATION → PRODUCT_UPDATE → PRODUCT_DELETION`
- Designed to be extensible so additional process types/expected paths can be configured deterministically.

### 2. Status Classification & Conformance Score
- **Classification**:
  - `CONFORMANT`: Actual sequence matches expected path with zero deviations.
  - `DEVIATED`: One or more deviations detected.
- **Structural Conformance Score**:
  $$\text{conformanceScore} = \min\left(1.0, \frac{\text{matchedExpectedActivities}}{\text{expectedActivities}}\right)$$
  - Fully conformant process: `1.0`.
  - Process missing one of 3 expected activities: `0.67`.
  - Implementation-defined structural coverage score, not a formal process mining fitness/precision metric.

### 3. Deviation Taxonomy & Detection Logic

| Deviation Type | Definition | Example Scenario |
| :--- | :--- | :--- |
| `MISSING_ACTIVITY` | An expected activity in the reference path was not executed. | Expected: `CREATE → UPDATE → DELETE`<br>Actual: `CREATE → DELETE`<br>Deviation: Missing `PRODUCT_UPDATE` |
| `UNEXPECTED_ACTIVITY` | An activity occurs that is not part of the expected path, or an unexpected repetition occurs. | Expected: `CREATE → UPDATE → DELETE`<br>Actual: `CREATE → UPDATE → UPDATE → DELETE`<br>Deviation: Repeated `PRODUCT_UPDATE` |
| `ORDER_VIOLATION` | An expected activity occurs, but out of the expected relative order. | Expected: `CREATE → UPDATE → DELETE`<br>Actual: `CREATE → DELETE → UPDATE`<br>Deviation: `PRODUCT_UPDATE` after `PRODUCT_DELETION` |
| `TERMINAL_ACTIVITY_VIOLATION` | An activity occurs after the expected terminal activity. | Expected: `CREATE → UPDATE → DELETE`<br>Actual: `CREATE → UPDATE → DELETE → UPDATE`<br>Deviation: `PRODUCT_UPDATE` at position 4 after terminal `PRODUCT_DELETION` |

---

## API Endpoints & Filtering

| Method | Endpoint | Description | Query Parameters |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/analytics/processes/{id}` | Single process instance cycle-time & stage count | None |
| `GET` | `/api/v1/analytics/processes/{id}/stages` | Chronological stage duration breakdown | None |
| `GET` | `/api/v1/analytics/summary` | Aggregate summary (processes, cycle times, throughput) | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/throughput` | Process throughput over time range | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/bottlenecks` | Stage bottleneck analysis & duration contribution | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/variants` | List process variants ranked by occurrence count | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/variants/{variantKey}` | Get detailed metrics for a specific process variant | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/rework` | Aggregate rework summary & activity-level breakdown | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/rework/{processId}` | Instance-specific rework detail & repeated activities | None |
| `GET` | `/api/v1/analytics/conformance` | Aggregate conformance metrics & deviation breakdown | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/conformance/{processId}` | Single process instance conformance analysis | None |

---

## Example Process Scenarios & Verification Matrix

| Scenario | Sequence | Status | Score | Deviation Types Reported |
| :--- | :--- | :--- | :--- | :--- |
| **A** | `CREATE → UPDATE → DELETE` | `CONFORMANT` | `1.0` | None |
| **B** | `CREATE → UPDATE → UPDATE → DELETE` | `DEVIATED` | `1.0` | `UNEXPECTED_ACTIVITY` |
| **C** | `CREATE → DELETE` | `DEVIATED` | `0.67` | `MISSING_ACTIVITY` |
| **D** | `CREATE → DELETE → UPDATE` | `DEVIATED` | `1.0` | `TERMINAL_ACTIVITY_VIOLATION`, `ORDER_VIOLATION` |
| **E** | `CREATE → UPDATE → DELETE → UPDATE` | `DEVIATED` | `1.0` | `TERMINAL_ACTIVITY_VIOLATION` |

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

- **Java Unit & Integration Test Suite**: `50/50 passed` (Analytics service logic, variant grouping, rework math, conformance scoring & deviation detection, repository queries, WebMvcTest controllers, Kafka consumers).
- **C# ASP.NET Core Regression Suite**: `102/102 passed`.
- **E2E Kafka → Spring Boot → PostgreSQL → Conformance REST API Verification**: Verified real domain events across all 5 test scenarios (A, B, C, D, E) via ASP.NET Core API (`/api/v1/products`) through Kafka into PostgreSQL.
- **PostgreSQL vs REST API Verification**:
  - Process A (`CREATE > UPDATE > DELETE`): `CONFORMANT`, `score = 1.0`, `deviations = 0`.
  - Process B (`CREATE > UPDATE > UPDATE > DELETE`): `DEVIATED`, `score = 1.0`, `UNEXPECTED_ACTIVITY` (position 3).
  - Process C (`CREATE > DELETE`): `DEVIATED`, `score = 0.67`, `MISSING_ACTIVITY` (`PRODUCT_UPDATE` at position 2).
  - Process D (`CREATE > DELETE > UPDATE`): `DEVIATED`, `score = 1.0`, `TERMINAL_ACTIVITY_VIOLATION` & `ORDER_VIOLATION`.
  - Process E (`CREATE > UPDATE > DELETE > UPDATE`): `DEVIATED`, `score = 1.0`, `TERMINAL_ACTIVITY_VIOLATION` (position 4).
- **Aggregate Summary Verification**:
  - `totalProcessesAnalyzed`: 12
  - `conformantProcessCount`: 5
  - `deviatedProcessCount`: 7
  - `conformanceRate`: 41.67%
  - `averageConformanceScore`: 0.95
  - `totalDeviationCount`: 9 (`MISSING_ACTIVITY`: 2, `UNEXPECTED_ACTIVITY`: 4, `ORDER_VIOLATION`: 1, `TERMINAL_ACTIVITY_VIOLATION`: 2).
- **Latency / Performance**: API response latency measured $\approx 15\text{ ms} - 47\text{ ms}$ on local test dataset. *Local dataset size is limited; production-scale p95/p99 latency should be evaluated under full load.*
- **Limitations**: Deterministic, rule-based structural conformance algorithm inspired by process mining concepts. Does not use ML, fuzzy matching, or statistical token-based fitness alignment.





