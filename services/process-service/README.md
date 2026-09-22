# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v2.0 — Process Analytics Engine`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / Spring Kafka / PostgreSQL

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, step-level tracking, and deterministic bottleneck detection for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics engine, workflow state reconstruction, step execution tracking, and historical event persistence.
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
    │   │   ├── analytics/                           # v2.0 Process Analytics Engine Layer
    │   │   │   ├── ProcessAnalyticsController.java  # REST Controller (/api/v1/analytics/*)
    │   │   │   ├── ProcessAnalyticsService.java     # Process Analytics Engine Business Logic
    │   │   │   ├── ProcessAnalyticsRepository.java  # Custom Specification & Aggregation Queries
    │   │   │   └── dto/
    │   │   │       ├── ProcessMetricsResponse.java  # Single Process Instance Analytics DTO
    │   │   │       ├── StageMetricsResponse.java    # Stage Breakdown DTO
    │   │   │       ├── ProcessAnalyticsSummaryResponse.java # Aggregate Metrics DTO
    │   │   │       ├── ThroughputResponse.java       # Process Throughput Metrics DTO
    │   │   │       ├── BottleneckResponse.java       # Stage Bottleneck Metrics DTO
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
            ├── analytics/                           # v2.0 Analytics Unit & Controller Tests
            │   ├── ProcessAnalyticsServiceTests.java # Analytics Unit Tests (Math & Aggregation)
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

## v2.0 Process Analytics

### 1. Process Lifecycle Reconstruction
- Reconstructs end-to-end process instances (`PRODUCT_LIFECYCLE`, `WAREHOUSE_LIFECYCLE`, `INVENTORY_MANAGEMENT`) from persisted domain event history.
- Events are ordered strictly by source event timestamp (`timestamp`), not database insertion time.
- Terminal events (`ProductDeletedEvent`, `WarehouseDeletedEvent`) transition status to `COMPLETED` and set `completedAt`.

### 2. Metrics & Formulas
- **Cycle Time** (Completed Processes):  
  $$\text{cycleTimeMs} = \text{completedAt} - \text{startedAt}$$  
  *Returned as `null` for active processes, exposing `elapsedDurationMs` separately for active duration tracking.*
- **Stage Duration**:  
  $$\text{stageDurationMs} = \text{step.completedAt} - \text{step.startedAt}$$
- **Throughput**:  
  $$\text{throughputPerHour} = \frac{\text{completedProcesses}}{\text{timeWindowHours}}$$  
  *Calculated over completed processes only.*
- **Bottleneck Contribution**:  
  $$\text{processTimeContribution} = \frac{\text{stageTotalDuration}}{\text{totalDurationOfAnalyzedStages}}$$  
  *Provides a deterministic ratio of time consumed by each stage.*

---

## API Endpoints & Filtering

| Method | Endpoint | Description | Query Parameters |
| :--- | :--- | :--- | :--- |
| `GET` | `/api/v1/analytics/processes/{id}` | Single process instance cycle-time & stage count | None |
| `GET` | `/api/v1/analytics/processes/{id}/stages` | Chronological stage duration breakdown | None |
| `GET` | `/api/v1/analytics/summary` | Aggregate summary (processes, cycle times, throughput) | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/throughput` | Process throughput over time range | `processType`, `from`, `to` |
| `GET` | `/api/v1/analytics/bottlenecks` | Stage bottleneck analysis & duration contribution | `processType`, `from`, `to` |

---

## Database & Query Optimization

- **Composite & Field Indexes**:
  - `process_instances`: `started_at`, `completed_at`, `(process_type, started_at)`, `status`.
  - `process_steps`: `step_name`, `completed_at`.
- **Dynamic JPA Specifications**:
  - Utilizes Spring Data `JpaSpecificationExecutor` (`Specification<ProcessInstance>`) to dynamically build predicate `WHERE` clauses for optional filters (`processType`, `from`, `to`) without triggering PostgreSQL JDBC type-casting issues on null parameters.

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

java -jar target/process-service-1.0.0-SNAPSHOT.jar
```

---

## Verification & Benchmark Results

- **Java Unit & Integration Test Suite**: `37/37 passed` (Analytics service logic, repository queries, WebMvcTest controllers, Kafka consumers, entity mappings).
- **C# ASP.NET Core Regression Suite**: `102/102 passed`.
- **E2E Kafka → Spring Boot → PostgreSQL → Analytics Verification**: Verified with real domain events emitted from ASP.NET Core API via Kafka into PostgreSQL and read through `/api/v1/analytics/*`.
- **SQL Mathematical Verification**: Independent PostgreSQL calculations verified exact match against API responses for cycle time (84,000 ms), average cycle time (44,053.5 ms), min cycle time (4,107 ms), completed count (2), active count (3), and total count (5).
- **Latency / Performance**: API response latency measured $< 15\text{ ms}$ for all endpoints on local test dataset (5 processes, 9 events, 9 steps). *Local dataset too small for a meaningful multi-thousand record p95/p99 latency benchmark.*


