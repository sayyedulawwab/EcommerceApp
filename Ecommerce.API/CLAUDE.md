# Ecommerce.API — Conventions

HTTP layer: resource-based, versioned controllers that map request DTOs to commands/queries, invoke the handler, and map the `Result` to an HTTP response. It contains no business logic.

## Folder layout

```
Controllers/{Resource}/     {Resource}Controller.cs + {Verb}{Entity}Request.cs records
Extensions/                 ErrorMappingExtensions (Error → IActionResult), MiddlewareExtensions,
                            MigrationExtensions (dev migrate + seed), ApplicationBuilderExtensions
Infrastructure/             ExceptionHandler (IExceptionHandler → 409 for ConcurrencyException, else 500 ProblemDetails)
Middleware/                 RequestContextLoggingMiddleware (CorrelationId)
DependencyInjection.cs      AddPresentation()
Program.cs                  Host, logging, OpenTelemetry, versioning, pipeline
```

Resources: `auth`, `categories`, `products`, `orders`, `reviews`.

---

## Controllers

```csharp
[Route("api/v{v:apiVersion}/categories")]
[ApiController]
[Authorize]
public class CategoriesController() : ControllerBase
{
    [MapToApiVersion(1)]
    [HttpPut("{id}")]
    public async Task<IActionResult> EditCategory(Guid id, EditCategoryRequest request, ICommandHandler<EditCategoryCommand, Guid> handler, CancellationToken cancellationToken)
    {
        var command = new EditCategoryCommand(id, request.Name, request.Code);

        Result<Guid> result = await handler.Handle(command, cancellationToken);

        if (result.IsFailure)
        {
            return result.Error.ToActionResult();
        }

        return Ok(new { id = result.Value });
    }
}
```

Rules:

- **REST resource routes** use `[Route("api/v{v:apiVersion}/{resource-plural}")]` at class level. The verbs are `[HttpPost]` create, `[HttpGet]` list/search, `[HttpGet("{id}")]`, `[HttpPut("{id}")]`, `[HttpDelete("{id}")]`. Non-CRUD actions get a named sub-route (`[HttpPost("register")]`, `[HttpPost("login")]`).
- `public class {Resource}Controller() : ControllerBase`, with an empty primary constructor. There is **no shared base controller**.
- Attributes: `[ApiController]` always. `[Authorize]` at class level for protected resources, `[AllowAnonymous]` on public reads (category/product GETs). `AuthController` is anonymous. Writes to products, categories and coupons are `[Authorize(Roles = Roles.Admin)]` (`Application/Abstractions/Auth/Roles.cs`); login adds the role claim when `User.IsAdmin` is true. A signed-in customer gets 403.
- **`[MapToApiVersion(1)]` on every action.**
- **Inject the handler as an action parameter** (`ICommandHandler<TCommand, TResponse>` / `IQueryHandler<TQuery, TResponse>`); `[ApiController]` infers `[FromServices]`. Parameter order is route values, request body/query, handler, then `CancellationToken cancellationToken` last.
- Action names are `{Verb}{Entity}` (`AddProduct`, `GetProduct`, `SearchProduct`, `EditProduct`, `DeleteProduct`, `PlaceOrder`, `GiveReview`).
- **Map request → command/query explicitly** with `new XCommand(...)`. The route `id` goes into the command separately from the body.
- **Current user:** read `User?.FindFirst(ClaimTypes.NameIdentifier)`. If it's missing, return `Unauthorized()`. Otherwise `Guid.Parse` it and pass it as the command's `UserId`. Never accept the user id from the request body.
- One `if (result.IsFailure) { return result.Error.ToActionResult(); }` block, then the success response.

### Success responses

| Operation             | Response                                                                 |
| --------------------- | ------------------------------------------------------------------------ |
| Create (POST)         | 201: `Created(string.Empty, new { id = result.Value })`                   |
| Get / list / search   | 200: `Ok(result.Value)`                                                   |
| Update (PUT)          | 200: `Ok(new { id = result.Value })`                                      |
| Delete (DELETE)       | 200: `Ok(new { id = result.Value })`                                      |
| Register / Login      | 200: `Ok(new { id = result.Value })` / `Ok(result.Value)` (`AccessTokenResponse`) |

### Error responses

Every failure is an RFC 9457 `ProblemDetails` (`application/problem+json`). `ErrorMappingExtensions.ToProblemDetails(this Error error)` is the **only** place `Error.Code` is mapped to HTTP, and both `ToActionResult()` and `ExceptionHandler` go through it:

| `Error.Code` (`System.Net.HttpStatusCode`) | Status | `title`          |
| ------------------------------------------ | ------ | ---------------- |
| `BadRequest`                               | 400    | `Bad Request`    |
| `Unauthorized`                             | 401    | `Unauthorized`   |
| `Forbidden`                                | 403    | `Forbidden`      |
| `NotFound`                                 | 404    | `Not Found`      |
| `Conflict`                                 | 409    | `Conflict`       |
| `InternalServerError` / any other          | 500    | `Server failure` |

- `type` links the RFC 9110 section for the status, `detail` is `Error.Description`, and the `code` extension is `Error.Name` (`Category.NameAlreadyExists`), which clients branch on.
- A `ValidationError` adds an `errors` extension: one `{ code, detail }` per failed rule.
- If a domain error uses a status that isn't listed here, add a case to this switch; otherwise it is returned as 500.
- Unhandled exceptions go to `ExceptionHandler`. A `ConcurrencyException` is logged as a warning and becomes a 409 with code `Concurrency.Conflict`. Anything else is logged as an error and becomes a 500 with code `Server.Failure` and a fixed message: the exception message can contain SQL, table names or stack details, so it never reaches the client.

---

## Request DTOs

- `record` types in the controller's folder, named `{Verb}{Entity}Request`. **They are separate from Application commands** and are never passed to handlers directly.
- Simple requests use a positional record: `public record AddCategoryRequest(string Name, string Code);`.
- Requests with value-type fields that must be supplied use property records with `[JsonRequired]`, so a missing `decimal`/`int`/`Guid` isn't silently defaulted:
  ```csharp
  public record AddProductRequest
  {
      [JsonRequired] public string Name { get; init; }
      [JsonRequired] public decimal PriceAmount { get; init; }
      [JsonRequired] public Guid CategoryId { get; init; }
  }
  ```
- Search/filter requests bind with `[FromQuery]` and use nullable properties for optional filters (`SearchProductRequest`).
- Nested line items are a second record in the same file (`PlaceOrderProductRequest` inside `PlaceOrderRequest.cs`).

---

## Program.cs & pipeline

- Service registration: `AddApplication()` → `AddPresentation()` → `AddInfrastructure(configuration)`, then `AddOpenApi()` and `AddApiVersioning(...)`.
- API versioning: default `1.0`, `ReportApiVersions`. The version is read from the URL segment only (`UrlSegmentApiVersionReader`), so a route without `v{n}` doesn't match. The API explorer uses `GroupNameFormat = "'v'V"` and `SubstituteApiVersionInUrl`.
- Development only: `app.MapOpenApi()` and `app.ApplyMigrations()` (migrates, and seeds the database when it is empty).
- Middleware order (keep it): `UseRequestContextLogging()` → `UseExceptionHandler()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`, then `MapHealthChecks("/health")`.
- `GET /health` is anonymous, unversioned and returns only `Healthy` (200) or `Unhealthy` (503): never details or exceptions. The checks (`DatabaseHealthCheck`, `RedisHealthCheck`) live in Infrastructure's `Health/`. The compose healthcheck and the deploy script rely on it.
- Don't add `public partial class Program;`. Since .NET 10 a source generator makes the top-level `Program` public, which is all `WebApplicationFactory<Program>` needs (analyzer ASP0027 flags the declaration as redundant).

---

## Logging & observability

- Logging is `Microsoft.Extensions.Logging` only. Don't add Serilog or another logging library: the default console provider covers `docker logs`, and the OpenTelemetry provider exports to OTLP with the trace and span IDs on every record. Levels come from the `Logging` section in `appsettings.json`.
- `builder.Logging.AddOpenTelemetry` sets `IncludeScopes` (so scope values such as `CorrelationId` are exported) and `IncludeFormattedMessage`.
- Request logs come from the ASP.NET Core traces and the `http.server.request.duration` metric, not from a per-request log line.
- `RequestContextLoggingMiddleware` opens an `ILogger.BeginScope` with `CorrelationId` from the `X-Correlation-Id` header, falling back to `TraceIdentifier`.
- OpenTelemetry covers traces (ASP.NET Core + HttpClient), metrics (ASP.NET Core, HttpClient, runtime) and logs. Each OTLP/HTTP-protobuf exporter is enabled only when its endpoint is configured: `Otlp:LogsEndpoint`, `Otlp:TracesEndpoint`, `Otlp:MetricsEndpoint`.
- Command/query logging already happens in Application's `LoggingDecorator`. Don't add per-action logging in controllers.

---

## Configuration

- `appsettings.json` has only the environment-agnostic `Logging` section. `appsettings.Development.json` has `Otlp`, `Jwt` (`Issuer`, `Audience`, `SecretKey`) and `ConnectionStrings` (`Database`, `Redis`), using Docker service names (`database-postgres`, `cache-redis`, `otel-collector`).
- Outside Development these must come from environment variables (`ConnectionStrings__Database`, `Jwt__SecretKey`, `Otlp__LogsEndpoint`, ...); without a connection string the host fails at startup. Without `Otlp:*` endpoints the OTLP exporters are simply off.
- The committed JWT secret and DB password are dev values. Real secrets go in user secrets (`UserSecretsId` is set) or environment variables. Never commit new secrets.
