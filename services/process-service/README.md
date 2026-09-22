# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v2.2 — Rework Detection`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / Spring Kafka / PostgreSQL

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, step-level tracking, deterministic bottleneck detection, process variant analysis, and rework detection for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics engine, workflow state reconstruction, step execution tracking, process variant discovery, rework analysis, and historical event persistence.
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
    │   │   ├── analytics/                           # Process Analytics & Rework Engine Layer
    │   │   │   ├── ProcessAnalyticsController.java  # REST Controller (/api/v1/analytics/*)
    │   │   │   ├── ProcessAnalyticsService.java     # Metrics, Variant & Rework Business Logic
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
            │   ├── ProcessAnalyticsServiceTests.java # Analytics Unit Tests (Math, Variants & Rework)
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

## v2.2 Rework Detection Engine

### 1. Definition & Detection Algorithm
In SupplyChainX, **rework** is defined deterministically as repeated execution of the same activity within a single process instance.
- "Rework detection" means repeated activity detection. The system detects repeated execution without assuming business intent or error causation.
- Event ordering is preserved using domain event timestamps.
- Repetitions are counted beyond the first occurrence:
  $$\text{reworkOccurrencesForActivity}(A) = \max(N(A) - 1, 0)$$
  $$\text{totalProcessRework}(P) = \sum_{A} \max(N_P(A) - 1, 0)$$

### 2. Process-Level Metrics
- **reworkRate** (Completed processes):  
  $$\text{reworkRate} = \frac{\text{reworkedProcessCount}}{\text{totalCompletedProcesses}} \times 100$$
- **averageReworkOccurrencesPerReworkedProcess**:  
  $$\text{avgReworkPerReworked} = \frac{\text{totalReworkOccurrences}}{\text{reworkedProcessCount}}$$

### 3. Activity-Level Metrics
- **totalExecutionCount**: Total executions of activity across completed processes.
- **reworkOccurrences**: Total rework occurrences for activity across completed processes.
- **affectedProcessCount**: Number of completed processes containing repetition ($N(A) > 1$).
- **averageReworkOccurrencesPerAffectedProcess**: $\frac{\text{reworkOccurrences}}{\text{affectedProcessCount}}$
- **reworkContributionPercentage**: $\frac{\text{activityReworkOccurrences}}{\text{totalReworkOccurrences}} \times 100$

### 4. Cycle-Time Impact Comparison
Compares completed processes with rework against completed processes without rework:
- `averageCycleTimeWithReworkMs`, `averageCycleTimeWithoutReworkMs`
- `minCycleTimeWithReworkMs`, `maxCycleTimeWithReworkMs`
- `minCycleTimeWithoutReworkMs`, `maxCycleTimeWithoutReworkMs`
- `cycleTimeDifferenceMs` = $\text{averageCycleTimeWithReworkMs} - \text{averageCycleTimeWithoutReworkMs}$
*Note*: Cycle-time difference represents an observed association in historical event logs, not a causal business inference.

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

---

## Example Process Sequences & E2E Validation

| Process | Sequence | Rework Occurrences | Repetition Type |
| :--- | :--- | :--- | :--- |
| **Process A** | `CREATE -> UPDATE -> DELETE` | 0 | No rework |
| **Process B** | `CREATE -> UPDATE -> UPDATE -> DELETE` | 1 (`UPDATE`) | Consecutive repetition |
| **Process C** | `CREATE -> UPDATE -> UPDATE -> UPDATE -> DELETE` | 2 (`UPDATE`) | Triple execution |
| **Process D** | `CREATE -> DELETE` | 0 | Minimal sequence |
| **Process E** | `CREATE -> UPDATE -> DELETE -> UPDATE` | 1 (`UPDATE`) | Non-consecutive repetition |

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

- **Java Unit & Integration Test Suite**: `46/46 passed` (Analytics service logic, variant grouping, rework math, active process handling, repository queries, WebMvcTest controllers, Kafka consumers).
- **C# ASP.NET Core Regression Suite**: `102/102 passed`.
- **E2E Kafka → Spring Boot → PostgreSQL → Rework REST API Verification**: Generated real domain events via ASP.NET Core API (`/api/v1/products`) through Kafka into PostgreSQL.
- **PostgreSQL vs REST API Verification**:
  - Process A (`CREATE > UPDATE > DELETE`): `hasRework = false`, `totalReworkOccurrences = 0`.
  - Process B (`CREATE > UPDATE > UPDATE > DELETE`): `hasRework = true`, `totalReworkOccurrences = 1` (`PRODUCT_UPDATE`: 1).
  - Process C (`CREATE > UPDATE > UPDATE > UPDATE > DELETE`): `hasRework = true`, `totalReworkOccurrences = 2` (`PRODUCT_UPDATE`: 2).
  - Process D (`CREATE > DELETE`): `hasRework = false`, `totalReworkOccurrences = 0`.
  - Process E (`CREATE > UPDATE > DELETE > UPDATE`): `hasRework = true`, `totalReworkOccurrences = 1` (`PRODUCT_UPDATE`: 1 non-consecutive).
- **Aggregate Summary Verification**:
  - `totalCompletedProcesses`: 11
  - `reworkedProcessCount`: 4
  - `nonReworkedProcessCount`: 7
  - `reworkRate`: 36.36%
  - `totalReworkOccurrences`: 5
  - `averageReworkOccurrencesPerReworkedProcess`: 1.25
  - `activities[0]`: `PRODUCT_UPDATE`, `totalExecutionCount`: 14, `reworkOccurrences`: 5, `affectedProcessCount`: 4, `reworkContributionPercentage`: 100.0%.
- **Latency / Performance**: API response latency measured $< 25\text{ ms}$ on local test dataset. *Local dataset size is limited; production-scale p95/p99 latency should be evaluated under full load.*




