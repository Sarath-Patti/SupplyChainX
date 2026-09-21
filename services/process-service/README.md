# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v1.9 — Phase 2 (Kafka Event Integration)`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / Spring Kafka / PostgreSQL

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, and step-level tracking for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics, workflow state reconstruction, step execution tracking, and historical event persistence.
- **PostgreSQL**: Shared relational storage with isolated tables (`process_instances`, `process_events`, `process_steps`).
- **Apache Kafka**: Primary domain event bus consumed asynchronously by `process-service`.

---

## Directory Structure

```
services/process-service/
├── pom.xml
├── README.md
└── src/
    ├── main/
    │   ├── java/com/supplychainx/processservice/
    │   │   ├── ProcessServiceApplication.java       # Spring Boot Application Entrypoint
    │   │   ├── controller/
    │   │   │   └── ProcessController.java            # REST API Endpoints (/api/v1/processes)
    │   │   ├── service/
    │   │   │   └── ProcessService.java               # Business Logic, State Transitions & Idempotency
    │   │   ├── kafka/
    │   │   │   ├── ProcessEventConsumer.java         # @KafkaListener Consumer for Domain Events
    │   │   │   ├── KafkaEventMapper.java             # Maps JSON Events to Entities & Steps
    │   │   │   ├── KafkaConsumerConfig.java          # Spring Kafka Container Factory & Ack Configuration
    │   │   │   └── model/
    │   │   │       └── SupplyChainXDomainEventDto.java # Jackson DTO for Domain Event Payloads
    │   │   ├── repository/
    │   │   │   ├── ProcessInstanceRepository.java   # Spring Data JPA Repository for ProcessInstance
    │   │   │   ├── ProcessEventRepository.java      # Spring Data JPA Repository for ProcessEvent
    │   │   │   └── ProcessStepRepository.java       # Spring Data JPA Repository for ProcessStep
    │   │   ├── entity/
    │   │   │   ├── ProcessInstance.java             # Process Instance JPA Entity
    │   │   │   ├── ProcessEvent.java                # Process Event JPA Entity
    │   │   │   └── ProcessStep.java                 # Process Step JPA Entity
    │   │   ├── dto/
    │   │   │   ├── ProcessInstanceResponseDto.java
    │   │   │   ├── ProcessInstanceDetailResponseDto.java
    │   │   │   ├── ProcessEventResponseDto.java
    │   │   │   ├── ProcessStepResponseDto.java
    │   │   │   ├── CreateProcessInstanceRequestDto.java
    │   │   │   └── ErrorResponseDto.java
    │   │   └── exception/
    │   │       ├── ResourceNotFoundException.java
    │   │       └── GlobalExceptionHandler.java      # Centralized HTTP Exception Handler
    │   └── resources/
    │       ├── application.yml                      # Production / PostgreSQL & Kafka Configuration
    │       └── application-test.yml                 # Fast In-Memory H2 Testing Configuration
    └── test/
        └── java/com/supplychainx/processservice/
            ├── ProcessServiceApplicationTests.java   # Spring Context Load Test
            ├── repository/
            │   └── ProcessRepositoryTests.java      # DataJpaTest for Entity Persistence & Queries
            ├── controller/
            │   └── ProcessControllerTests.java      # WebMvcTest MockMvc Controller Tests
            ├── service/
            │   └── ProcessServiceTests.java         # Unit & Integration Tests for Event Ingestion
            └── kafka/
                ├── KafkaEventMapperTests.java       # Unit Tests for Mapping Logic & Steps
                └── ProcessEventConsumerTests.java   # Tests for Listener Execution & Error Handling
```

---

## Configuration & Environment Variables

PostgreSQL and Kafka parameters are fully configurable via environment variables:

| Environment Variable | Default Value | Description |
| :--- | :--- | :--- |
| `PORT` | `8081` | HTTP Port for the Spring Boot Service |
| `DB_HOST` | `localhost` | PostgreSQL Database Host |
| `DB_PORT` | `5432` | PostgreSQL Database Port |
| `DB_NAME` | `supplychainx_db` | PostgreSQL Database Name |
| `DB_USERNAME` | `postgres` | PostgreSQL Database User |
| `DB_PASSWORD` | `postgres_dev_password` | PostgreSQL Database Password |
| `DDL_AUTO` | `update` | Hibernate DDL Auto Schema Strategy |
| `KAFKA_BOOTSTRAP_SERVERS` | `localhost:9092` | Kafka Bootstrap Broker List |
| `KAFKA_PROCESS_GROUP_ID` | `supplychainx-process-analytics-group` | Consumer Group ID |
| `KAFKA_TOPIC_PRODUCT` | `supplychainx.product.events` | Product Domain Event Topic |
| `KAFKA_TOPIC_WAREHOUSE` | `supplychainx.warehouse.events` | Warehouse Domain Event Topic |
| `KAFKA_TOPIC_INVENTORY` | `supplychainx.inventory.events` | Inventory Domain Event Topic |

---

## Kafka Integration & Event Mapping (Phase 2)

### Consumed Topics & Event Types
`process-service` consumes existing C# ASP.NET Core domain events from three primary Kafka topics:
1. `supplychainx.product.events`: `ProductCreatedEvent`, `ProductUpdatedEvent`, `ProductDeletedEvent`
2. `supplychainx.warehouse.events`: `WarehouseCreatedEvent`, `WarehouseUpdatedEvent`, `WarehouseDeletedEvent`
3. `supplychainx.inventory.events`: `InventoryAdjustedEvent`

### Process Instance & Step Mapping
- **Business Keys**: Extracted automatically as `PRODUCT-{productId}`, `WAREHOUSE-{warehouseId}`, or `INVENTORY-{inventoryId}`.
- **Process Types**: `PRODUCT_LIFECYCLE`, `WAREHOUSE_LIFECYCLE`, `INVENTORY_MANAGEMENT`.
- **Lifecycle Transitions**: Initial events create active process instances. Terminal events (`ProductDeletedEvent`, `WarehouseDeletedEvent`) transition status to `COMPLETED` and record `completedAt`.
- **Process Steps**: Automatically generated per event (e.g. `PRODUCT_CREATION`, `PRODUCT_UPDATE`, `INVENTORY_ADJUSTMENT`).

### Idempotency & Transaction Control
- **Dual-Layer Idempotency**:
  1. Application-level check: `existsByEventId(UUID eventId)` before processing.
  2. Database-level constraint: `@Index(unique = true)` on `process_events.event_id` catching `DataIntegrityViolationException`.
- **Manual Acknowledgment**: Kafka offset is acknowledged (`AckMode.MANUAL_IMMEDIATE`) only after transactional database persistence completes.

---

## Build and Run Instructions

### 1. Build and Run Unit/Integration Tests
```bash
export PATH="/opt/homebrew/opt/openjdk/bin:/opt/homebrew/bin:$PATH"
cd services/process-service
mvn clean test
```

### 2. Package Executable Application JAR
```bash
mvn clean package
```

### 3. Run Service Locally
```bash
export DB_HOST=localhost
export DB_PORT=5432
export DB_NAME=supplychainx_db
export DB_USERNAME=postgres
export DB_PASSWORD=postgres_dev_password
export KAFKA_BOOTSTRAP_SERVERS=localhost:9092

java -jar target/process-service-1.0.0-SNAPSHOT.jar
```
*The service will start on port `8081` with Actuator health probes at `http://localhost:8081/actuator/health`.*

---

## Verification Results

- **Java Unit/Integration Test Suite**: `24/24 passed` (Spring context, Repositories, Controllers, Service layer, Mapper, and Kafka Consumer).
- **C# ASP.NET Core Test Suite**: `102/102 passed`.

