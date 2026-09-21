# SupplyChainX — Process Analytics Service (`process-service`)

**Version**: `v1.9 — Phase 1 (Foundation)`  
**Technology**: Java 21 / Spring Boot 3.3.3 / Spring Data JPA / PostgreSQL

---

## Purpose & Architecture Role

`process-service` is a dedicated Java Spring Boot microservice designed to provide high-throughput process analytics, workflow instance reconstruction, and step-level tracking for SupplyChainX enterprise operations.

In the polyglot SupplyChainX architecture:
- **C# / .NET 8 Web API** (`backend/`): Core transactional operations (Products, Warehouses, Inventory, RBAC, AI Copilot, MCP Server).
- **Java Spring Boot Microservice** (`services/process-service/`): Asynchronous process analytics, workflow state reconstruction, step execution tracking, and historical event persistence.
- **PostgreSQL**: Shared relational storage with isolated tables (`process_instances`, `process_events`, `process_steps`).
- **Apache Kafka**: Primary domain event bus (integration coming in Phase 2).

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
    │   │   │   └── ProcessService.java               # Business Logic & DTO Mapping
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
    │   │       └── GlobalExceptionHandler.java      # Centralized HTTP 404/400/500 Exception Handler
    │   └── resources/
    │       ├── application.yml                      # Production / PostgreSQL Configuration
    │       └── application-test.yml                 # Fast In-Memory H2 Testing Configuration
    └── test/
        └── java/com/supplychainx/processservice/
            ├── ProcessServiceApplicationTests.java   # Spring Context Load Test
            ├── repository/
            │   └── ProcessRepositoryTests.java      # DataJpaTest for Entity Persistence & Queries
            └── controller/
                └── ProcessControllerTests.java          # WebMvcTest MockMvc Controller Tests
```

---

## Configuration & Environment Variables

PostgreSQL parameters are fully configurable via environment variables without hardcoded credentials:

| Environment Variable | Default Value | Description |
| :--- | :--- | :--- |
| `PORT` | `8081` | HTTP Port for the Spring Boot Service |
| `DB_HOST` | `localhost` | PostgreSQL Database Host |
| `DB_PORT` | `5432` | PostgreSQL Database Port |
| `DB_NAME` | `supplychainx_db` | PostgreSQL Database Name |
| `DB_USERNAME` | `postgres` | PostgreSQL Database User |
| `DB_PASSWORD` | `postgres_dev_password` | PostgreSQL Database Password |
| `DDL_AUTO` | `update` | Hibernate DDL Auto Schema Strategy |

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

java -jar target/process-service-1.0.0-SNAPSHOT.jar
```
*The service will start on port `8081` with Actuator health probes at `http://localhost:8081/actuator/health`.*

---

## REST API Foundation (Phase 1)

| HTTP Method | Path | Description | Response Code |
| :--- | :--- | :--- | :--- |
| **GET** | `/api/v1/processes` | List all persisted process instances | `200 OK` |
| **GET** | `/api/v1/processes/{id}` | Get process instance details (including steps and events) | `200 OK` / `404 Not Found` |
| **GET** | `/api/v1/processes/{id}/events` | Get all events for a process instance ordered by timestamp | `200 OK` / `404 Not Found` |
| **POST** | `/api/v1/processes` | Create a new process instance | `201 Created` / `400 Bad Request` |
| **GET** | `/actuator/health` | Health Check Probe | `200 OK` |

---

## Current Limitations & Planned Next Phase

- **Phase 1 Limitations**: Kafka consumer bindings, real-time analytics aggregation, and JWT authentication integration are deferred to subsequent phases.
- **Next Phase (Phase 2)**: Integration of Spring Kafka consumers to ingest domain events directly from Kafka primary topics (`supplychainx.product.events`, `supplychainx.warehouse.events`, `supplychainx.inventory.events`) and reconstruct workflow lifecycles automatically.
