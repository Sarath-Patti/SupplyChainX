# SupplyChainX

**Version**: `v0.1.0`  
**Milestone**: `v0.1 – Project Foundation`

SupplyChainX is an enterprise inventory and order management platform built on a modern, modular, scalable event-driven architecture.

---

## v0.1 Scope & Foundation

Milestone **v0.1** establishes the core technology stack, project directory hierarchy, containerization infrastructure, backend ASP.NET Core solution layout, and frontend Angular initialization application.

> [!NOTE]
> Business functionality (inventory management, warehouse routing, order execution, shipments, authentication screens, domain entities, and database tables) is intentionally deferred to subsequent milestones.

---

## Technology Stack

- **Backend**: C# 12 / .NET 8 ASP.NET Core Web API
- **Frontend**: Angular 19+ (TypeScript, Standalone Component Architecture)
- **Database**: PostgreSQL 16
- **ORM**: Entity Framework Core 8
- **Messaging**: Apache Kafka (KRaft mode)
- **Authentication**: JWT (Infrastructure foundation ready)
- **Testing**: xUnit
- **Containerization**: Docker & Docker Compose
- **Version Control**: Git

---

## Repository Structure

```
SupplyChainX/
├── frontend/                # Angular SPA foundation
│   ├── src/
│   │   ├── app/
│   │   │   ├── core/        # Core singletons and initialization logic
│   │   │   └── shared/      # Shared components and primitives
│   │   └── assets/
│   ├── angular.json
│   ├── package.json
│   └── tsconfig.json
├── backend/                 # ASP.NET Core Web API solution
│   ├── SupplyChainX.sln
│   └── src/
│       ├── SupplyChainX.Api/          # Web API host & health endpoints
│       ├── SupplyChainX.Application/  # Application service interfaces & result abstractions
│       ├── SupplyChainX.Domain/       # Core domain primitives (Entity, AggregateRoot)
│       └── SupplyChainX.Infrastructure/# DbContext baseline & external integration abstractions
├── messaging/               # Kafka schemas and event contract specifications
├── tests/                   # Automated test suites
│   └── SupplyChainX.UnitTests/ # xUnit backend unit test baseline
├── docs/                    # Architecture and developer documentation
├── scripts/                 # Environment and development scripts
├── infrastructure/          # Container orchestration (Docker Compose)
│   ├── docker-compose.yml
│   └── .env.example
├── LICENSE                  # MIT License
└── README.md                # Project documentation
```

---

## Infrastructure Setup

Docker Compose configuration is available in `infrastructure/docker-compose.yml` to provision local development dependencies:

- **PostgreSQL 16**: Port `5432`
- **Apache Kafka (KRaft)**: Port `9092`

Copy `infrastructure/.env.example` to `infrastructure/.env` before starting services.

```bash
cd infrastructure
cp .env.example .env
docker compose up -d
```

---

## Verification & Health Check

The backend ASP.NET Core API provides a minimal startup status endpoint at:

```
GET /health
```

Response schema:

```json
{
  "status": "Healthy",
  "service": "SupplyChainX API",
  "version": "v0.1.0",
  "timestamp": "2026-08-23T18:35:26Z"
}
```
