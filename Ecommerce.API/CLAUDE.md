# Ecommerce.API — Conventions

HTTP layer: resource-based, versioned controllers that map request DTOs to commands/queries, invoke the handler, and map the `Result` to an HTTP response. It contains no business logic.

## Folder layout

```
Controllers/{Resource}/     {Resource}Controller.cs + {Verb}{Entity}Request.cs records
Extensions/                 ErrorMappingExtensions (Error → IActionResult), MiddlewareExtensions,
                            MigrationExtensions (dev migrate + seed), ApplicationBuilderExtensions
Infrastructure/             ExceptionHandler (IExceptionHandler → 500 ProblemDetails)
Logging/                    ActivityEnricher (Serilog trace_id / span_id)
Middleware/                 RequestContextLoggingMiddleware (CorrelationId)
DependencyInjection.cs      AddPresentation()
Program.cs                  Host, Serilog, OpenTelemetry, versioning, pipeline
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
- Attributes: `[ApiController]` always. `[Authorize]` at class level for protected resources, `[AllowAnonymous]` on public reads (category/product GETs). `AuthController` is anonymous.
- **`[MapToApiVersion(1)]` on every action.**
- **Inject the handler as an action parameter** (`ICommandHandler<TCommand, TResponse>` / `IQueryHandler<TQuery, TResponse>`); `[ApiController]` infers `[FromServices]`. Parameter order is route values, request body/query, handler, then `CancellationToken cancellationToken` last.
- Action names are `{Verb}{Entity}` (`AddProduct`, `GetProduct`, `SearchProduct`, `EditProduct`, `DeleteProduct`, `PlaceOrder`, `GiveReview`).
- **Map request → command/query explicitly** with `new XCommand(...)`. The route `id` goes into the command separately from the body.
- **Current user:** read `User?.FindFirst(ClaimTypes.NameIdentifier)`. If it's missing, return `Unauthorized()`. Otherwise `Guid.Parse` it and pass it as the command's `UserId`. Never accept the user id from the request body.
- One `if (result.IsFailure) { return result.Error.ToActionResult(); }` block, then the success response.

### Success responses

| Operation             | Response                                                                 |
| --------------------- | ------------------------------------------------------------------------ |
| Create (POST)         | 201: `Created()` (Categories, Products) or `Created(string.Empty, result.Value)` (Orders, Reviews). Stay consistent within a controller; prefer returning the id for new resources |
| Get / list / search   | 200: `Ok(result.Value)`                                                   |
| Update (PUT)          | 200: `Ok(new { id = result.Value })`                                      |
| Delete (DELETE)       | 200: `Ok(new { id = result.Value })`                                      |
| Register / Login      | 200: `Ok(result.Value)` (user id / `AccessTokenResponse`)                 |

### Error responses

`ErrorMappingExtensions.ToActionResult(this Error error)` is the **only** place `Error.Code` is mapped to HTTP:

| `Error.Code` (`System.Net.HttpStatusCode`) | Result                                     |
| ------------------------------------------ | ------------------------------------------ |
| `BadRequest`                               | `BadRequestObjectResult(error)` (400). Includes `ValidationError.Errors` |
| `Unauthorized`                             | `UnauthorizedObjectResult(error)` (401)     |
| `NotFound`                                 | `NotFoundObjectResult(error)` (404)         |
| `Conflict`                                 | `ConflictObjectResult(error)` (409)         |
| `InternalServerError` / any other          | `ObjectResult(error) { StatusCode = 500 }` |

If a domain error uses a status that isn't listed here, add a case to this switch; otherwise it is returned as 500. Unhandled exceptions go to `ExceptionHandler`, which logs and writes a 500 `ProblemDetails` ("Server failure").

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
- API versioning: default `1.0`, `AssumeDefaultVersionWhenUnspecified`, `ReportApiVersions`. The version is read from the URL segment **or** the `X-Api-Version` header. The API explorer uses `GroupNameFormat = "'v'V"` and `SubstituteApiVersionInUrl`.
- Development only: `app.MapOpenApi()` and `app.ApplyMigrations()` (**drops**, migrates and seeds the database).
- Middleware order (keep it): `UseRequestContextLogging()` → `UseSerilogRequestLogging()` → `UseExceptionHandler()` → `UseAuthentication()` → `UseAuthorization()` → `MapControllers()`.
- Keep `public partial class Program;` at the end; `WebApplicationFactory<Program>` needs it.

---

## Logging & observability

- Serilog is configured from the `Serilog` config section (JSON file sinks `logs/log-.json`, `logs/errorlog-.json`), enriched `FromLogContext`, thread and machine name, plus `ActivityEnricher` (`trace_id`, `span_id`). `writeToProviders: true` feeds OpenTelemetry logging.
- `RequestContextLoggingMiddleware` pushes `CorrelationId` from the `X-Correlation-Id` header, falling back to `TraceIdentifier`.
- OpenTelemetry covers traces (ASP.NET Core + HttpClient), metrics (ASP.NET Core, HttpClient, runtime) and logs. Each OTLP/HTTP-protobuf exporter is enabled only when its endpoint is configured: `Otlp:LogsEndpoint`, `Otlp:TracesEndpoint`, `Otlp:MetricsEndpoint`.
- Command/query logging already happens in Application's `LoggingDecorator`. Don't add per-action logging in controllers.

---

## Configuration

- `appsettings.json` / `appsettings.Development.json` sections: `Serilog`, `Otlp`, `Jwt` (`Issuer`, `Audience`, `SecretKey`), `ConnectionStrings` (`Database`, `Redis`). Hostnames are the Docker service names (`database-postgres`, `cache-redis`, `otel-collector`).
- The committed JWT secret and DB password are dev values. Real secrets go in user secrets (`UserSecretsId` is set) or environment variables. Never commit new secrets.
