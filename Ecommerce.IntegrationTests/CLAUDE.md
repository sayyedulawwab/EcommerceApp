# Ecommerce.IntegrationTests — Conventions

End-to-end tests over **real HTTP** against the full API host (`WebApplicationFactory<Program>`) and **real PostgreSQL and Redis** containers (Testcontainers). The database is reset between tests with Respawn.

- xUnit (`Xunit` is a global using), default **`Assert.*` only**. There is no mocking library; real implementations are used except for the overrides below.
- **Docker must be running.** Run: `dotnet test Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj`.
- One `IntegrationTestFactory` is shared by the whole run (`[assembly: AssemblyFixture]` in `Infrastructure/AssemblySetup.cs`): one PostgreSQL container, one Redis container and one API host. The tests share that state, so they run one at a time (`CollectionBehavior.CollectionPerAssembly`).

## Layout

```
Infrastructure/
  AssemblySetup.cs                Registers the factory as the assembly fixture and turns off parallel test classes
  IntegrationTestFactory.cs       WebApplicationFactory<Program> + IAsyncLifetime (the fixture every test uses)
  DatabaseReset.cs                Respawn wrapper (Postgres adapter, ignores __EFMigrationsHistory)
  FakeEmailService.cs             Records sent emails (SentEmail: To, Subject, Body)
  TestAuthenticationHandler.cs    "Test" scheme: user id from X-Test-User-Id (else a fixed id), admin role from its role header
Helpers/
  {Entity}Data.cs                 Static builders with defaults: CategoryData, UserData, ProductData, CouponData, OrderData
{Module}/
  {Action}Tests.cs                One class per endpoint/action, e.g. Products/AddProductTests.cs, Orders/PlaceOrderTests.cs
```

## What `IntegrationTestFactory` sets up

- A `postgres:18` container. `ConnectionStrings:Database` is overridden to point at it, and `ApplicationDbContext` and `ISqlConnectionFactory` are re-registered against it.
- A `redis:8` container (the image `infra.compose.yml` runs). `ConnectionStrings:Redis` points at it, so caching runs against real Redis. `ResetDatabaseAsync()` flushes it along with the database, so a cached result from an earlier test can't hide newly seeded rows. `IEmailService` is replaced by `FakeEmailService` (`_factory.Emails`), which records each email so tests can assert what a domain event handler sent. `ResetDatabaseAsync()` clears it. Seeding a user raises its welcome email, so call `_factory.Emails.Clear()` after seeding when a test checks a later email.
- Authentication keeps the real JwtBearer scheme and adds the `Test` scheme behind a policy scheme (`TestOrBearer`). A request with `Authorization: Test` goes to `TestAuthenticationHandler`; anything else (a real bearer token, or no header) goes to JwtBearer, so 401s and token validation are real.
- In the `Test` scheme, `ClaimTypes.NameIdentifier` is the `X-Test-User-Id` header value if present, otherwise `TestAuthenticationHandler.UserId` (`11111111-1111-1111-1111-111111111111`, which doesn't exist in `users`).
- The environment is `Development`, so **host startup runs `ApplyMigrations()` (drop + migrate + Bogus seed)** against the container. The test calls `MigrateAsync()` again and then creates the Respawn checkpoint.
- Helpers: `Client` (`Test` scheme, the fixed user id), `CreateClient()` (no `Authorization` header, so it goes to JwtBearer: use it for anonymous calls and 401s), `CreateClientForUser(Guid userId)` (authorized as a specific user; dispose it with `using`), `CreateAdminClient()` (an admin, for product, category and coupon writes), `ResetDatabaseAsync()`, `ExecuteScopeAsync(...)` / `ExecuteScopeAsync<TResult>(...)` for direct DbContext access, `CreateScope()`.

## Test structure

```csharp
public sealed class AddProductTests
{
    private readonly IntegrationTestFactory _factory;

    public AddProductTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddProduct_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(category);
            await db.SaveChangesAsync();
        });

        AddProductRequest request = ProductData.Create(category.Id.Value);

        // Act
        using HttpClient adminClient = _factory.CreateAdminClient();

        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/products", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            ProductName productName = ProductName.Create(request.Name).Value;
            return await db.Set<Product>().FirstOrDefaultAsync(p => p.Name == productName);
        });

        Assert.NotNull(product);
        Assert.Equal(request.Name, product.Name.Value);
        Assert.Equal(category.Id, product.CategoryId);
    }
}
```

Rules:

- `public sealed class {Action}Tests`, with the assembly's factory injected through the constructor into a `private readonly` field. Don't add `IClassFixture<IntegrationTestFactory>`: it would start a second set of containers.
- Test names: **`{Action}_Should{Outcome}_When{Condition}`** (`AddProduct_ShouldReturnNotFound_WhenCategoryDoesNotExist`).
- **Start every test with `await _factory.ResetDatabaseAsync();`**. The dev seed data and other tests' data must not leak in.
- **AAA** comments in every test.
- **Seed through domain factories** via `Helpers/{Entity}Data` builders (optional parameters with defaults) and `ExecuteScopeAsync` + `ApplicationDbContext`. Never use raw SQL inserts.
- In `Helpers/`, `Create(...)` returns the API's `{Verb}{Entity}Request` where one exists (`ProductData.Create(categoryId)` → `AddProductRequest`). Domain-entity builders get an explicit name (`ProductData.CreateProduct(categoryId, priceAmount, currencyCode)`). `CategoryData.Create` / `UserData.Create` return entities because those resources have no request in these tests.
- **Endpoints that use the current user** (orders, reviews) need a real user because `orders.user_id` / `reviews.user_id` are FKs to `users`. Seed `UserData.Create()`, then call the API with `using HttpClient client = _factory.CreateClientForUser(user.Id.Value);`. `_factory.Client`'s fixed user is fine only for paths that fail before inserting (validation, 404).
- Don't use `!` on values that `Assert.NotNull` already proved non-null; the analyzer flags it (S8969) and the build fails.
- Call versioned routes (`/api/v1/{resource}`) with `PostAsJsonAsync` / `GetAsync` / `PutAsJsonAsync` / `DeleteAsync`.
- Assert the **HTTP status first**, then verify persisted state by querying `ApplicationDbContext` in a fresh scope. Compare value objects by constructing them (`ProductName.Create(...).Value`, `category.Id`).
- Cover the success path and each failure status the endpoint can return (400 validation, 401 credentials, 403 role, 404 lookup, 409 conflict). Failures are `ProblemDetails`: read them with `ReadFromJsonAsync<ProblemDetails>()` and assert the `code` extension (`problem.Extensions["code"]?.ToString()`).
- A rule backed by a unique index gets a concurrency test: fire the same request several times with `Task.WhenAll` and assert one 2xx, the rest 409, and one row (`AddCategoryTests`, `AddCouponTests`).
- `Auth/BearerTokenTests` covers the real JWT path with tokens from `/api/v1/auth/login`. Other tests use the `Test` scheme helpers.
