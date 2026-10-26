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

---

## 1. Stack

- **.NET 10** (`net10.0`, set once in `Directory.Build.props`), ASP.NET Core **controllers**
- **Clean Architecture** with strict layer separation
- **CQRS**: custom `ICommand` / `IQuery` handlers (no MediatR)
- **Domain-Driven Design**: aggregates, value objects, strongly typed IDs, domain events
- **PostgreSQL 18**: **EF Core 10** for the write side and migrations, **Dapper** for read-side projections
- **xUnit** for tests, using the **default `Assert.*` only** (no FluentAssertions, no mocking library)

| Technology                              | Purpose                                                        |
| --------------------------------------- | -------------------------------------------------------------- |
| EF Core + Npgsql                        | Handlers use `IApplicationDbContext` directly; change tracking, migrations |
| EFCore.NamingConventions                | `UseSnakeCaseNamingConvention()`: all tables/columns snake_case |
| Dapper                                  | Hand-written SQL reads behind query repositories in Infrastructure |
| FluentValidation                        | One validator per command, run by `ValidationDecorator`        |
| Scrutor                                 | Assembly scanning for handlers + decorator registration        |
| Redis (`IDistributedCache`)             | Query result caching via `ICacheService`                       |
| JWT Bearer                              | Auth; `[Authorize]` on controllers, `[Authorize(Roles = Roles.Admin)]` on admin writes |
| Asp.Versioning                          | URL segment (`/api/v1/...`) + `X-Api-Version` header           |
| Microsoft.AspNetCore.OpenApi            | `MapOpenApi()` (Development only)                              |
| Serilog                                 | Structured logging, `ActivityEnricher` (trace_id/span_id)      |
| OpenTelemetry                           | Logs/traces/metrics → OTLP collector → Loki/Tempo/Prometheus → Grafana |
| Bogus                                   | Development data seeding (`DataSeeder`)                        |
| SonarAnalyzer.CSharp                    | Static analysis (warnings are errors)                          |
| Testcontainers, Respawn, WebApplicationFactory | Integration tests against real PostgreSQL               |
| ArchUnitNET                             | Architecture tests: layer dependency rules                     |

---

## 2. Commands

```bash
# Build (must finish with 0 warnings: warnings are errors)
dotnet build EcommerceApp.slnx

# Domain unit tests (no external dependencies)
dotnet test --project Ecommerce.Domain.UnitTests/Ecommerce.Domain.UnitTests.csproj

# Architecture tests (layer dependencies, ArchUnitNET)
dotnet test --project Ecommerce.ArchitectureTests/Ecommerce.ArchitectureTests.csproj

# Integration tests (Docker must be running: Testcontainers starts postgres:18)
dotnet test --project Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj

# Add an EF migration (EF tools package lives in the API project)
dotnet ef migrations add <Name> --project Ecommerce.Infrastructure --startup-project Ecommerce.API

# Run the full stack in Docker. Infra is a separate compose project (`ecommerce-infra`) that creates the shared
# external network and keeps running while the API stack is stopped/rebuilt (VS Docker Compose reads .env.dev via the dcproj)
docker compose -f infra.compose.yml --env-file .env.dev up -d
docker compose -f docker-compose.yml -f docker-compose.override.yml --env-file .env.dev up --build
# API: http://localhost:5000 (direct) or http://localhost:8080 (via nginx); Grafana: http://localhost:3000
```

Build and environment notes:

- `TreatWarningsAsErrors` is on solution-wide, so **NuGet audit warnings are errors too**, locally, in CI and in the Docker build. A package with a known vulnerability, even a transitive one, fails restore. Only NU1900 (vulnerability data couldn't be downloaded) stays a warning. There are no automatic package upgrades, so the audit is the signal. When it fails:
  1. Upgrade the package: change its `PackageVersion` in `Directory.Packages.props`. For a transitive package, add a `PackageVersion` for it; transitive pinning moves every project to that version. Or remove the package that pulls it in.
  2. Only when no fixed version exists yet, or an urgent fix can't wait for the upgrade to be tested, suppress that one advisory in `Directory.Packages.props` with `<NuGetAuditSuppress Include="https://github.com/advisories/GHSA-..." />` and a comment saying why it's safe for now and when to remove it. Never turn the audit off or add NU1901-NU1904 to `WarningsNotAsErrors`.
- **Pin exact package versions** (no floating `10.0.*`). Keep ASP.NET Core / EF Core packages on the same patch version: `Microsoft.EntityFrameworkCore.Relational` is pinned in Infrastructure for this reason, and `Microsoft.Extensions.ApiDescription.Server` must stay compatible with the pinned `Microsoft.OpenApi`, or the build-time OpenAPI step (which regenerates `Ecommerce.API/Ecommerce.API.json`) fails.
- `appsettings.json` holds only environment-agnostic settings (Serilog). Connection strings, OTLP endpoints and `Jwt` values live in `appsettings.Development.json` (Docker hostnames `database-postgres`, `cache-redis`, `otel-collector`). Anything running outside Development (Production, containers) must supply them as environment variables (`ConnectionStrings__Database`, `ConnectionStrings__Redis`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__SecretKey`, `Otlp__*Endpoint`); `docker-compose.yml` does this from `.env.*` values, and compose fails fast if `JWT_SECRET_KEY` is unset. Running with `dotnet run` on the host needs overrides pointing at `localhost` (and `--no-launch-profile` to control the URL).
- `.env.prod` deliberately has no `POSTGRES_PASSWORD` or `JWT_SECRET_KEY`; provide them from the deployment environment.
- The Docker build passes `/p:OpenApiGenerateDocuments=false` to `dotnet publish`: build-time OpenAPI generation boots the app, which has no connection string there. `Ecommerce.API.json` is regenerated by normal local builds.
- Asp.Versioning 10.2+ adds analyzers (AV0016/AV0029/AV0030) that fail this build; the package is pinned at 10.0.0 until the OpenAPI setup is migrated.
- **In Development, startup runs `ApplyMigrations()`, which calls `EnsureDeleted()`, then migrates and reseeds with Bogus data.** The dev database is wiped on every start.

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
├── Ecommerce.ArchitectureTests/      # Layer dependency rules (ArchUnitNET)
└── Ecommerce.IntegrationTests/       # End-to-end HTTP tests against PostgreSQL in Docker
```

Supporting folders: `nginx/`, `otel/`, `loki/`, `tempo/`, `prometheus/`, `grafana/` (observability stack config), `docker-compose*.yml`, `infra.compose.yml`, `.env.dev` / `.env.prod`.

### Dependency direction

```
API → Application → Domain
API → Infrastructure                     (DI wiring + DbContext for migrations)
Infrastructure → Application → Domain    (implements the Application abstractions)
```

- **Domain** references nothing: no EF, no Dapper, no attributes.
- **Application** defines the ports (`IApplicationDbContext`, `IProductQueryRepository`, `IDateTimeProvider`, `ICacheService`, `IAuthService`, `IJwtService`, `IEmailService`). It references EF Core (for `DbSet<T>` and LINQ in handlers) but not Dapper or Npgsql.
- **Infrastructure** implements those ports. `ApplicationDbContext` implements `IApplicationDbContext`; the Dapper query repositories implement the query repository interfaces.

### Layer responsibilities

| Layer          | Role                                                    | Key patterns                                                   |
| -------------- | ------------------------------------------------------- | -------------------------------------------------------------- |
| Domain         | Business state, invariants, factories                   | `Entity<TId>`, record value objects, `{Entity}Errors`, domain events |
| Application    | Use-case orchestration, validation, read projections    | `ICommandHandler`/`IQueryHandler`, decorators, FluentValidation, `IApplicationDbContext` |
| Infrastructure | Persistence and external services                       | `ApplicationDbContext : IApplicationDbContext`, Dapper query repositories, `IEntityTypeConfiguration<T>` |
| API            | HTTP, routing, auth, request → command mapping          | Resource controllers, action-injected handlers, `Error.ToActionResult()` |

### Request lifecycle

**Command** (POST/PUT/DELETE):

1. The controller action receives a `{Verb}{Entity}Request` plus an `ICommandHandler<TCommand, TResponse>` injected **as an action parameter**.
2. The controller maps the request to a command. The route `id` and the user id from JWT claims are passed in explicitly.
3. The decorator chain runs **`LoggingDecorator` (outer) → `ValidationDecorator` → handler**. On validation failure it returns `ValidationError` (400) without calling the handler.
4. The handler runs its lookups (failures return `Result.Failure`), builds value objects, calls the domain `Create`/`Update`, stages new or removed entities with `dbContext.{Set}.Add`/`Remove` (tracked entities need no `Update` call), then `dbContext.SaveChangesAsync`.
5. `ApplicationDbContext.SaveChangesAsync` saves, then **dispatches the collected domain events synchronously** (each in a new DI scope).
6. The handler invalidates cache prefixes if needed and returns `Result<T>`. The controller returns 2xx, or `result.Error.ToActionResult()`.

**Query** (GET): `LoggingDecorator` → handler, with **no validation decorator**. The handler reads through `IApplicationDbContext` with `AsNoTracking()` (simple reads), or through a Dapper query repository (multi-table joins, selected columns, stored procedures, SQL that is easier or faster by hand), optionally via the cache.

---

## 4. Domain Modules

Each module uses the same name in every layer (plural folders in Domain, Application and API, e.g. `Products/`):

| Module       | Aggregate(s)          | Operations                                               |
| ------------ | --------------------- | -------------------------------------------------------- |
| `Categories` | `Category`            | Add, Edit, Delete, GetCategories, GetCategoryById        |
| `Products`   | `Product`             | Add, Edit, Delete, GetProductById (with reviews), SearchProduct (Dapper, paged, cached) |
| `Orders`     | `Order` → `OrderItem` | PlaceOrder (optional coupon), CancelOrder, SearchOrders (the user's own; paged, sorted, filtered) |
| `Coupons`    | `Coupon`              | Add, Edit, Delete, GetCoupons (admins only)               |
| `Reviews`    | `Review`              | AddReview                                                |
| `Users`      | `User`                | Register (command), Login (query → JWT); exposed via `AuthController` |
| `Shared`     | `Money`, `Currency`   | Value objects reused across aggregates                   |

---

## 5. Naming Conventions

| Artifact              | Pattern                                                                                   | Example                                   |
| --------------------- | ----------------------------------------------------------------------------------------- | ----------------------------------------- |
| Commands              | `Add{Entity}Command`, `Edit{Entity}Command`, `Delete{Entity}Command`, or a domain verb    | `EditProductCommand`, `PlaceOrderCommand`, `RegisterUserCommand` |
| Queries               | `Get{Entity}ByIdQuery`, `Get{Entities}Query` / `GetAll{Entities}Query`, `Search{Entity}Query` | `GetCategoryByIdQuery`, `SearchProductQuery` |
| Handlers              | `{CommandOrQuery}Handler`                                                                 | `AddCategoryCommandHandler`               |
| Validators            | `{Command}Validator`                                                                      | `AddProductCommandValidator`              |
| Feature folder        | `Application/{Module}/{Verb}{Entity}/`, named after the operation, not the command        | `Application/Products/EditProduct/`       |
| Responses             | `{Entity}Response` (`sealed class`)                                                        | `ProductResponse`, `OrderItemResponse`    |
| API request DTOs      | `{Verb}{Entity}Request` (`record`) next to the controller                                  | `AddProductRequest`, `GiveReviewRequest`  |
| Controllers           | `{Resource}Controller` in `API/Controllers/{Resource}/`                                    | `ProductsController`                      |
| Strongly typed IDs    | `{Entity}Id`                                                                              | `ProductId`                               |
| Domain errors         | `{Entity}Errors` static class; codes `"{Entity}.{Reason}"`                                 | `UserErrors.AlreadyExists`                |
| Domain events         | `{Entity}{PastTenseVerb}DomainEvent` in `Domain/{Module}/Events/`                          | `ProductCreatedDomainEvent`               |
| Query repositories    | `I{Entity}QueryRepository` (Application) / `{Entity}QueryRepository` (Infrastructure)      | `IProductQueryRepository`                 |
| EF configurations     | `{Entity}Configuration`                                                                   | `OrderItemConfiguration`                  |
| DB tables / columns   | snake_case plural tables, snake_case columns (automatic)                                  | `order_items.created_on_utc`              |
| Timestamps            | `CreatedOnUtc` (`DateTime`), `UpdatedOnUtc` (`DateTime?`)                                  |                                           |

Each Command/Query record, Handler and Validator gets **its own file** in the feature folder. A response shared by several features lives at the module root (`Application/Categories/CategoryResponse.cs`). A response used by one feature lives in that feature folder (`Application/Orders/GetAllOrders/OrderResponse.cs`).

---

## 6. Code Style (enforced at build: warnings are errors)

`Directory.Build.props` sets `AnalysisMode=All`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, plus SonarAnalyzer. `.editorconfig` severities are mostly `error`, so style violations **break the build**.

- **File-scoped namespaces**; `using` directives **outside** the namespace, `System.*` first. The namespace must match the folder.
- **Always use braces**, even for single-line `if`.
- **Explicit types.** Use `var` only when the type is apparent from the right-hand side (`new X(...)`, `X.Create(...)`). Otherwise spell it out:
  ```csharp
  var category = Category.Create(new CategoryName(request.Name), new CategoryCode(request.Code), dateTimeProvider.UtcNow);
  Category? category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);
  Result<Guid> result = await handler.Handle(command, cancellationToken);
  IReadOnlyList<ProductResponse> products = await productQueryRepository.SearchAsync(request, page, pageSize, cancellationToken);
  ```
- **Primary constructors** for DI in handlers, query repositories, services, controllers and middleware. Use the parameters directly; there are no `_field` assignments.
- No `this.` qualification. Use predefined types (`string`, not `String`). Use collection expressions (`[]`, `[.. items]`).
- Expression-bodied members for properties, operators and lambdas. Methods and constructors use block bodies.
- Pattern matching: `is null` / `is not null`, `value is < 1 or > 5`, `switch` expressions.
- **Visibility:** handlers, query repositories, EF configurations and most infrastructure services are **`internal sealed`**. Domain entities are `public sealed`. Validators are `public class`. Responses are `public sealed class`.
- Nullable reference types are enabled, but `CS8618`/`CS8600`/`CS8603` are suppressed (the EF private constructors leave non-nullable properties unset).
- Structured logging templates only (`logger.LogInformation("Processing command {Command}", commandName)`), never string interpolation in log calls.

---

## 7. Result & Error Pattern (all layers)

Defined in `Ecommerce.Domain/Abstractions/`:

- `Result` / `Result<TValue>`: `IsSuccess`, `IsFailure`, `Error`, `Value`. Reading `Value` on a failure throws.
- `Result<T>` has an **implicit conversion from `T`**, so handlers `return category.Id.Value;` and `return categoryResponse;`.
- Failures: `return Result.Failure<Guid>(CategoryErrors.NotFound);`
- `Error(string Name, string Description, HttpStatusCode Code)` is a record. `Code` is `System.Net.HttpStatusCode`; there is no custom status enum. Use the `Error.BadRequest/Unauthorized/NotFound/Conflict/InternalServerError(name, description)` factories, or (preferred) a `static readonly Error` on `{Entity}Errors`.
- Status codes in use: `BadRequest` (400), `Unauthorized` (401), `NotFound` (404), `Conflict` (409), `InternalServerError` (500).
- `ValidationError : Error` (400) carries `Error[] Errors`, one per failed property.

Rules:

- **Expected business failures return `Result.Failure`; do not throw.** Unhandled exceptions become a 500 `ProblemDetails` from `API/Infrastructure/ExceptionHandler`, except `ConcurrencyException`, which becomes a 409.
- The API maps `Error.Code` to an HTTP status in one place: `ErrorMappingExtensions.ToActionResult()`.
- Value objects that validate input return `Result<T>` from `Create` (see `Rating`), and aggregate methods that enforce invariants return `Result` (see `Order.AddOrderItem`). Callers must check `IsFailure` before continuing or using `.Value`.
- Validators catch bad input before it reaches the database. String limits mirror the column lengths (200/2000), and currency codes are checked with `Currency.IsSupported`, so bad input is a 400, not a 500.

---

## 8. Time & Identity

- **Always UTC.** Application code gets time from `IDateTimeProvider.UtcNow` and passes it into domain factories/mutations (`createdOnUtc`, `updatedOnUtc`). Never call `DateTime.Now`, and don't call `DateTime.UtcNow` in Domain or Application.
- **Strongly typed Guid IDs** (`record ProductId(Guid Value)`) are generated **in the domain** (`ProductId.New()` inside `Create`), not by the database. Handlers return `entity.Id.Value` (a `Guid`).
- The current user's id comes from the JWT `ClaimTypes.NameIdentifier` claim. **Controllers** read it and pass it in the command (`UserId`). There is no current-user service in Application.

---

## 9. Adding a New Feature

1. **Domain:** entity (`Entity<TId>`, private constructors, static `Create`/`Update`), `{Entity}Id`, value objects, `{Entity}Errors`, `{Entity}CreatedDomainEvent`.
2. **Infrastructure:** `{Entity}Configuration` (table, ID/VO conversions, owned `Money`, FKs), a `DbSet<{Entity}>` on `IApplicationDbContext` and `ApplicationDbContext`, then an EF migration. Add a query repository only for a read that needs hand-written SQL.
3. **Application:** feature folder containing the Command/Query record, `internal sealed` Handler, Validator (commands), and Response. Handlers are auto-registered by Scrutor; there is nothing to wire.
4. **API:** `{Verb}{Entity}Request` record and a controller action (versioned route, handler injected as an action parameter, `ToActionResult()` on failure).
5. **Side effects** (emails, etc.) go in an `IDomainEventHandler<TEvent>` for an event raised by the entity, never inline in the handler. Cache invalidation is the exception: it is done inline after `SaveChangesAsync` (see the Application guide).
6. **Tests:** HTTP tests in `Ecommerce.IntegrationTests/{Module}/` against the real PostgreSQL container, and unit tests in `Ecommerce.Domain.UnitTests/{Module}/` for new domain rules. Cover the success path **and every distinct failure path**. Never mock `DbSet` or use SQLite or the EF InMemory provider.

---

## 10. Testing (summary; details in each test project's `CLAUDE.md`)

- xUnit `[Fact]` / `[Theory]` + `[InlineData]`, default **`Assert.*`** only. No mocking library.
- **AAA:** `// Arrange`, `// Act`, `// Assert` comments in every integration test.
- Handlers are tested over HTTP against PostgreSQL in Testcontainers, not with mocks. Never mock `DbSet`, and never swap in SQLite or the EF InMemory provider: they don't behave like PostgreSQL.
- Domain rules (`Order.AddOrderItem`, `Rating.Create`, ...) are plain unit tests of the entities: no database, no mocks.
- Build domain objects through their real factories (`User.Create(...)`).
- Assert failures against the **domain error instance**: `Assert.Equal(OrderErrors.InvalidQuantity, result.Error)`. Over HTTP, assert the status code and the database state.
- Naming: domain tests `{Method}_Should_{Outcome}_When{Condition}`; integration tests `{Action}_Should{Outcome}_When{Condition}`.
- Use `Options.Create(new XOptions { ... })` for options in tests; never mock `IOptions<T>`.

---

## 11. Known Quirks

These exist in the codebase today. Leave them alone unless asked:

- `AuthService.VerifyPassword` falls back to the legacy salted SHA-256 hash, so accounts created before the PBKDF2 switch can still log in. Keep the fallback until those accounts are gone, and never use it for new hashes.
- Login reports a missing user and a wrong password with the same `UserErrors.InvalidCredentials` (401), so it doesn't reveal which emails are registered. Keep it that way.
- `OrderResponse.ShippedOnUtc/DeliveredOnUtc` and `OrderItemResponse.UpdatedOnUtc` exist but are never populated. `ProductResponse.TotalRecords` exists only to carry the Dapper window count.
- `CacheService.RemoveByPrefixAsync` only clears keys written by the same app instance (in-process key registry).
- Domain event handlers are auto-registered by the Scrutor scan in `Ecommerce.Application/DependencyInjection.cs`.
- A coupon's usage limit is shared by all customers ("the first 100 orders"). There's no per-customer limit.
