---
paths:
  - "**/*.cs"
---

# C# code conventions

## Technologies

| Technology                              | Purpose                                                        |
| --------------------------------------- | -------------------------------------------------------------- |
| EF Core + Npgsql                        | Handlers use `IApplicationDbContext` directly; change tracking, migrations |
| EFCore.NamingConventions                | `UseSnakeCaseNamingConvention()`: all tables/columns snake_case |
| Dapper                                  | Hand-written SQL reads behind query repositories in Infrastructure |
| FluentValidation                        | One validator per command, run by `ValidatingHandlers`        |
| Scrutor                                 | Assembly scanning for handlers + decorator registration        |
| Redis + `HybridCache`                   | Query result caching: in-memory (L1) in front of Redis (L2), cleared by tag |
| JWT Bearer (`JsonWebTokenHandler`)       | Auth for a browser frontend; 15-minute access token + rotating refresh token, both only in HttpOnly `Secure` `SameSite=Strict` cookies (never the body or an `Authorization` header), plus a required `X-CSRF: 1` header on writes; `[Authorize]` on controllers, `[Authorize(Roles = Roles.Admin)]` on admin writes |
| BCrypt.Net-Next                         | Password hashing (bcrypt, work factor 12)                      |
| Microsoft.AspNetCore.RateLimiting       | Per-IP sliding-window limits on the `/auth` endpoints                |
| Asp.Versioning                          | URL segment only (`/api/v1/...`)                               |
| Microsoft.AspNetCore.OpenApi            | `MapOpenApi()` (Development only)                              |
| Microsoft.Extensions.Logging            | `ILogger<T>` with message templates; no third-party logging library |
| OpenTelemetry                           | Logs/traces/metrics → OTLP collector → Loki/Tempo/Prometheus → Grafana |
| Bogus                                   | Development data seeding (`DataSeeder`)                        |
| SonarAnalyzer.CSharp                    | Static analysis (warnings are errors)                          |
| Testcontainers, Respawn, WebApplicationFactory | Integration tests against real PostgreSQL and Redis     |
| ArchUnitNET                             | Architecture tests: layer dependency rules                     |

## Layer responsibilities

| Layer          | Role                                                    | Key patterns                                                   |
| -------------- | ------------------------------------------------------- | -------------------------------------------------------------- |
| Domain         | Business state, invariants, factories                   | `Entity<TId>`, record value objects, `{Entity}Errors`, domain events |
| Application    | Use-case orchestration, validation, read projections    | `ICommandHandler`/`IQueryHandler`, decorators, FluentValidation, `IApplicationDbContext` |
| Infrastructure | Persistence and external services                       | `ApplicationDbContext : IApplicationDbContext`, Dapper query repositories, `IEntityTypeConfiguration<T>` |
| API            | HTTP, routing, auth, request → command mapping          | Resource controllers, action-injected handlers, `Error.ToActionResult()` |

## Request lifecycle

**Command** (POST/PUT/DELETE):

1. The controller action receives a `{Verb}{Entity}Request` plus an `ICommandHandler<TCommand, TResponse>` injected **as an action parameter**.
2. The controller maps the request to a command. The route `id` and the user id from JWT claims are passed in explicitly.
3. The decorator chain runs **`LoggingHandlers` (outer) → `ValidatingHandlers` → handler**. On validation failure it returns `ValidationError` (400) without calling the handler.
4. The handler runs its lookups (failures return `Result.Failure`), builds value objects, calls the domain `Create`/`Update`, stages new or removed entities with `dbContext.{Set}.Add`/`Remove` (tracked entities need no `Update` call), then `dbContext.SaveChangesAsync`.
5. `ApplicationDbContext.SaveChangesAsync` saves, then **dispatches the collected domain events synchronously** (each in a new DI scope).
6. The handler clears the product cache by tag if needed (a failed clear is logged, not returned) and returns `Result<T>`. The controller returns 2xx, or `result.Error.ToActionResult()`.

**Query** (GET): `LoggingHandlers` (outer) → `ValidatingHandlers` → handler. A query with a validator returns `ValidationError` (400) before the handler runs; a query without one passes straight through. The handler reads through `IApplicationDbContext` with `AsNoTracking()` (simple reads), or through a Dapper query repository (multi-table joins, selected columns, stored procedures, SQL that is easier or faster by hand), optionally via the cache.

## Domain Modules

Each module uses the same name in every layer (plural folders in Domain, Application and API, e.g. `Products/`):

| Module       | Aggregate(s)          | Operations                                               |
| ------------ | --------------------- | -------------------------------------------------------- |
| `Categories` | `Category`            | Add, Edit, Delete, GetCategories, GetCategoryById        |
| `Products`   | `Product`             | Add, Edit, Delete, GetProductById (with reviews), SearchProduct (Dapper, paged, cached except keyword searches, trigram-indexed keyword) |
| `Orders`     | `Order` → `OrderItem` | PlaceOrder (optional coupon), CancelOrder, SearchOrders (the user's own; paged, sorted, filtered) |
| `Coupons`    | `Coupon`              | Add, Edit, Delete, GetCoupons (admins only)               |
| `Reviews`    | `Review`              | AddReview                                                |
| `Users`      | `User`, `RefreshToken` | Register (command), Login (command → access + refresh token), Refresh, Logout; exposed via `AuthController` |
| `Shared`     | `Money`, `Currency`   | Value objects reused across aggregates                   |

## Naming Conventions

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

## Code Style (enforced at build: warnings are errors)

`Directory.Build.props` sets `AnalysisMode=All`, `TreatWarningsAsErrors`, `EnforceCodeStyleInBuild`, plus SonarAnalyzer. `.editorconfig` severities are mostly `error`, so style violations **break the build**.

- **File-scoped namespaces**; `using` directives **outside** the namespace, `System.*` first. The namespace must match the folder.
- **Always use braces**, even for single-line `if`.
- **Explicit types.** Use `var` only when the type is apparent from the right-hand side (`new X(...)`, `X.Create(...)`). Otherwise spell it out:
  ```csharp
  var category = Category.Create(new CategoryName(request.Name), new CategoryCode(request.Code), timeProvider.GetUtcNow().UtcDateTime);
  Category? category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);
  Result<Guid> result = await handler.HandleAsync(command, cancellationToken);
  IReadOnlyList<ProductResponse> products = await productQueryRepository.SearchAsync(request, page, pageSize, cancellationToken);
  ```
- **Primary constructors** for DI in handlers, query repositories, services, controllers and middleware. Use the parameters directly; there are no `_field` assignments.
- No `this.` qualification. Use predefined types (`string`, not `String`). Use collection expressions (`[]`, `[.. items]`).
- Expression-bodied members for properties, operators and lambdas. Methods and constructors use block bodies.
- Pattern matching: `is null` / `is not null`, `value is < 1 or > 5`, `switch` expressions.
- **Visibility:** handlers, query repositories, EF configurations and most infrastructure services are **`internal sealed`**. Domain entities are `public sealed`. Validators are `public class`. Responses are `public sealed class`.
- Nullable reference types are enabled, but `CS8618`/`CS8600`/`CS8603` are suppressed (the EF private constructors leave non-nullable properties unset).
- Structured logging templates only (`logger.LogInformation("Processing command {Command}", commandName)`), never string interpolation in log calls.

## Result & Error Pattern (all layers)

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

## Time & Identity

- **Always UTC.** Application code gets time from `TimeProvider.GetUtcNow().UtcDateTime` and passes it into domain factories/mutations (`createdOnUtc`, `updatedOnUtc`). Never call `DateTime.Now`, and don't call `DateTime.UtcNow` in Domain or Application.
- **Strongly typed Guid IDs** (`record ProductId(Guid Value)`) are generated **in the domain** (`ProductId.New()` inside `Create`), not by the database. Handlers return `entity.Id.Value` (a `Guid`).
- The current user's id comes from the JWT `ClaimTypes.NameIdentifier` claim. **Controllers** read it and pass it in the command (`UserId`). There is no current-user service in Application.

## Adding a New Feature

1. **Domain:** entity (`Entity<TId>`, private constructors, static `Create`/`Update`), `{Entity}Id`, value objects, `{Entity}Errors`, `{Entity}CreatedDomainEvent`.
2. **Infrastructure:** `{Entity}Configuration` (table, ID/VO conversions, owned `Money`, FKs), a `DbSet<{Entity}>` on `IApplicationDbContext` and `ApplicationDbContext`, then an EF migration. Add a query repository only for a read that needs hand-written SQL.
3. **Application:** feature folder containing the Command/Query record, `internal sealed` Handler, Validator (commands), and Response. Handlers are auto-registered by Scrutor; there is nothing to wire.
4. **API:** `{Verb}{Entity}Request` record and a controller action (versioned route, handler injected as an action parameter, `ToActionResult()` on failure).
5. **Side effects** (emails, etc.) go in an `IDomainEventHandler<TEvent>` for an event raised by the entity, never inline in the handler. Cache invalidation is the exception: it is done inline after `SaveChangesAsync` (see the Application guide).
6. **Tests:** HTTP tests in `Ecommerce.IntegrationTests/{Module}/` against the real PostgreSQL container, unit tests in `Ecommerce.Domain.UnitTests/{Module}/` for new domain rules, and validator tests in `Ecommerce.Application.UnitTests/{Module}/` for new validation rules. Cover the success path **and every distinct failure path**. Never mock `DbSet` or use SQLite or the EF InMemory provider.

## Known Quirks

These exist in the codebase today. Leave them alone unless asked:

- Passwords are bcrypt hashes. A row with an older hash (PBKDF2 or SHA-256, from a dev database made before bcrypt) can no longer log in; recreate the dev data. There is no password-change endpoint; when one is added it must revoke all of the user's refresh tokens.
- Emails are compared exactly as typed, so `Jane@x.com` and `jane@x.com` are two accounts (the unique index is case-sensitive).
- Login reports a missing user and a wrong password with the same `UserErrors.InvalidCredentials` (401), so it doesn't reveal which emails are registered. Keep it that way.
- `OrderResponse.ShippedOnUtc/DeliveredOnUtc` and `OrderItemResponse.UpdatedOnUtc` exist but are never populated.
- `HybridCache` has no backplane. `RemoveByTagAsync` clears the calling instance's in-memory copy and marks the tag in Redis, but other running instances keep serving their own in-memory copy until it expires (`LocalCacheExpiration`, 1 minute). With several instances, a product change can show as stale for up to that long on the others.
- Domain event handlers are auto-registered by the Scrutor scan in `Ecommerce.Application/DependencyInjection.cs`.
- A coupon's usage limit is shared by all customers ("the first 100 orders"). There's no per-customer limit.
