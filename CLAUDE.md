# EcommerceApp — Codebase & Conventions Guide

Ecommerce REST API (products, categories, orders, reviews, users/auth). This file and the per-project `CLAUDE.md` files are the source of truth for architecture, naming, and testing conventions.

**New code must match existing patterns exactly. Do not introduce new abstractions or libraries.** In particular:

- **No MediatR.** It was removed on purpose (commit `001c4e6`). CQRS dispatch uses the custom `ICommandHandler` / `IQueryHandler` interfaces plus Scrutor decorators.
- **No AutoMapper or mapper classes, and no generic CRUD handlers.** Every endpoint has its own command/query and handler.

| Guide                                                                          | Covers                                                        |
| ------------------------------------------------------------------------------ | ------------------------------------------------------------- |
| [Ecommerce.Domain/CLAUDE.md](Ecommerce.Domain/CLAUDE.md)                       | Entities, value objects, strongly typed IDs, errors, events    |
| [Ecommerce.Application/CLAUDE.md](Ecommerce.Application/CLAUDE.md)             | Commands/queries, handlers, validators, responses, Dapper reads |
| [Ecommerce.Infrastructure/CLAUDE.md](Ecommerce.Infrastructure/CLAUDE.md)       | EF Core config, Dapper query repositories, migrations, DI, auth, caching |
| [Ecommerce.API/CLAUDE.md](Ecommerce.API/CLAUDE.md)                             | Controllers, request DTOs, error mapping, versioning, logging  |
| [Ecommerce.IntegrationTests/CLAUDE.md](Ecommerce.IntegrationTests/CLAUDE.md)   | HTTP-level tests (Testcontainers + Respawn)                    |

Topic rules in `.claude/rules/` load when matching files are opened: `code.md` (any `.cs` file: technologies, request lifecycle, modules, naming, code style, Result & errors, time, adding a feature, known quirks), `testing.md` (test projects), `build-and-deploy.md` (packages, configuration, Docker, pipelines, deployment).

---

## 1. Stack

- **.NET 10** (`net10.0`, set once in `Directory.Build.props`), ASP.NET Core **controllers**
- **Clean Architecture** with strict layer separation
- **CQRS**: custom `ICommand` / `IQuery` handlers (no MediatR)
- **Domain-Driven Design**: aggregates, value objects, strongly typed IDs, domain events
- **PostgreSQL 18**: **EF Core 10** for the write side and migrations, **Dapper** for read-side projections
- **xUnit** for tests, using the **default `Assert.*` only** (no FluentAssertions, no mocking library)

---

## 2. Commands

```bash
# Build (must finish with 0 warnings: warnings are errors)
dotnet build EcommerceApp.slnx

# Domain unit tests (no external dependencies)
dotnet test --project Ecommerce.Domain.UnitTests/Ecommerce.Domain.UnitTests.csproj

# Validator unit tests (no external dependencies)
dotnet test --project Ecommerce.Application.UnitTests/Ecommerce.Application.UnitTests.csproj

# Architecture tests (layer dependencies, ArchUnitNET)
dotnet test --project Ecommerce.ArchitectureTests/Ecommerce.ArchitectureTests.csproj

# Integration tests (Docker must be running: Testcontainers starts postgres:18.6 and redis:8.10.3)
dotnet test --project Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj

# Add an EF migration (EF tools package lives in the API project)
dotnet ef migrations add <Name> --project Ecommerce.Infrastructure --startup-project Ecommerce.API

# Pipelines: .github/workflows/ci.yml (PR build and test), azure-pipelines.yml + .azure-pipelines/templates/ (build and deploy on the server's agent), deploy/
# Check deploy scripts after a change: bash -n deploy/deploy.sh deploy/host/*.sh (and shellcheck)

# Run the full stack in Docker. Infra is a separate compose project (`ecommerce-infra`) that creates the shared
# external network and keeps running while the API stack is stopped/rebuilt (VS Docker Compose reads .env.dev via the dcproj)
docker compose -f infra.compose.yml --env-file .env.dev up -d
docker compose -f docker-compose.yml -f docker-compose.override.yml --env-file .env.dev up --build
# API: http://localhost:5000 (direct) or http://localhost:8080 (via nginx); Grafana: http://localhost:3000
```

- **In Development, startup runs `PrepareDevelopmentDatabaseAsync()`: it migrates, then seeds Bogus data only if `users` is empty.** The dev database keeps its data across restarts; to start fresh, drop it (or remove the Postgres volume).

---

## 3. Architecture

### Project structure

```
EcommerceApp.slnx
├── Ecommerce.Domain/                 # Entities, value objects, IDs, errors, domain events (no dependencies)
├── Ecommerce.Application/            # Commands/queries + handlers, validators, responses, decorators, IApplicationDbContext and other ports
├── Ecommerce.Infrastructure/         # EF Core DbContext/configs/migrations, Dapper query repositories, auth, caching, clock, DI
├── Ecommerce.API/                    # Controllers, request DTOs, error mapping, middleware, Program.cs
├── Ecommerce.Domain.UnitTests/       # Domain rule tests (no database, no mocks)
├── Ecommerce.Application.UnitTests/  # Validator tests (no database, no mocks)
├── Ecommerce.ArchitectureTests/      # Layer dependency rules (ArchUnitNET)
└── Ecommerce.IntegrationTests/       # End-to-end HTTP tests against PostgreSQL in Docker
```

### Dependency direction

```
API → Application → Domain
API → Infrastructure                     (DI wiring + DbContext for migrations)
Infrastructure → Application → Domain    (implements the Application abstractions)
```

- **Domain** references nothing: no EF, no Dapper, no attributes.
- **Application** defines the ports (`IApplicationDbContext`, `IProductQueryRepository`, `IAuthService`, `IJwtService`, `IEmailService`); time comes from .NET's `TimeProvider`. It references EF Core (for `DbSet<T>` and LINQ in handlers) and the `HybridCache` abstraction (`Microsoft.Extensions.Caching.Abstractions`), but not Dapper, Npgsql or Redis.
- **Infrastructure** implements those ports. `ApplicationDbContext` implements `IApplicationDbContext`; the Dapper query repositories implement the query repository interfaces.
