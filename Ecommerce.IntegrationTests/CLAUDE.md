# Ecommerce.IntegrationTests — Conventions

End-to-end tests over **real HTTP** against the full API host (`WebApplicationFactory<Program>`) and a **real PostgreSQL** container (Testcontainers). The database is reset between tests with Respawn.

- xUnit (`Xunit` is a global using), default **`Assert.*` only**. There is no mocking library; real implementations are used except for the overrides below.
- **Docker must be running.** Run: `dotnet test Ecommerce.IntegrationTests/Ecommerce.IntegrationTests.csproj`.
- Each test class gets its own `IntegrationTestFactory`, which means its own container.

## Layout

```
Infrastructure/
  IntegrationTestFactory.cs       WebApplicationFactory<Program> + IAsyncLifetime (the fixture every test uses)
  DatabaseReset.cs                Respawn wrapper (Postgres adapter, ignores __EFMigrationsHistory)
  FakeEmailService.cs             Records sent emails (SentEmail: To, Subject, Body)
  TestAuthenticationHandler.cs    "Test" scheme: every request authenticated (user id from X-Test-User-Id, else a fixed id)
Helpers/
  {Entity}Data.cs                 Static builders with defaults: CategoryData, UserData, ProductData
{Module}/
  {Action}Tests.cs                One class per endpoint/action, e.g. Products/AddProductTests.cs, Orders/PlaceOrderTests.cs
```

## What `IntegrationTestFactory` sets up

- A `postgres:18` container. `ConnectionStrings:Database` is overridden to point at it, and `ApplicationDbContext` and `ISqlConnectionFactory` are re-registered against it.
- Redis is replaced with `AddDistributedMemoryCache()` (and `CacheService` is re-registered). `IEmailService` is replaced by `FakeEmailService` (`_factory.Emails`), which records each email so tests can assert what a domain event handler sent. `ResetDatabaseAsync()` clears it. Seeding a user raises its welcome email, so call `_factory.Emails.Clear()` after seeding when a test checks a later email.
- Authentication is replaced by `TestAuthenticationHandler`. Every request is authenticated. `ClaimTypes.NameIdentifier` is the `X-Test-User-Id` header value if present, otherwise `TestAuthenticationHandler.UserId` (`11111111-1111-1111-1111-111111111111`, which doesn't exist in `users`).
- The environment is `Development`, so **host startup runs `ApplyMigrations()` (drop + migrate + Bogus seed)** against the container. The test calls `MigrateAsync()` again and then creates the Respawn checkpoint.
- Helpers: `Client` (pre-authorized `HttpClient` using the fixed user id), `CreateClientForUser(Guid userId)` (authorized as a specific user; dispose it with `using`), `CreateAdminClient()` (an admin, for product, category and coupon writes), `ResetDatabaseAsync()`, `ExecuteScopeAsync(...)` / `ExecuteScopeAsync<TResult>(...)` for direct DbContext access, `CreateScope()`.

## Test structure

```csharp
public sealed class AddProductTests : IClassFixture<IntegrationTestFactory>
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
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/products", request);

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

- `public sealed class {Action}Tests : IClassFixture<IntegrationTestFactory>`, with the factory injected through the constructor into a `private readonly` field.
- Test names: **`{Action}_Should{Outcome}_When{Condition}`** (`AddProduct_ShouldReturnNotFound_WhenCategoryDoesNotExist`).
- **Start every test with `await _factory.ResetDatabaseAsync();`**. The dev seed data and other tests' data must not leak in.
- **AAA** comments in every test.
- **Seed through domain factories** via `Helpers/{Entity}Data` builders (optional parameters with defaults) and `ExecuteScopeAsync` + `ApplicationDbContext`. Never use raw SQL inserts.
- In `Helpers/`, `Create(...)` returns the API's `{Verb}{Entity}Request` where one exists (`ProductData.Create(categoryId)` → `AddProductRequest`). Domain-entity builders get an explicit name (`ProductData.CreateProduct(categoryId, priceAmount, currencyCode)`). `CategoryData.Create` / `UserData.Create` return entities because those resources have no request in these tests.
- **Endpoints that use the current user** (orders, reviews) need a real user because `orders.user_id` / `reviews.user_id` are FKs to `users`. Seed `UserData.Create()`, then call the API with `using HttpClient client = _factory.CreateClientForUser(user.Id.Value);`. `_factory.Client`'s fixed user is fine only for paths that fail before inserting (validation, 404).
- Don't use `!` on values that `Assert.NotNull` already proved non-null; the analyzer flags it (S8969) and the build fails.
- Call versioned routes (`/api/v1/{resource}`) with `PostAsJsonAsync` / `GetAsync` / `PutAsJsonAsync` / `DeleteAsync`.
- Assert the **HTTP status first**, then verify persisted state by querying `ApplicationDbContext` in a fresh scope. Compare value objects by constructing them (`ProductName.Create(...).Value`, `category.Id`).
- Cover the success path and each failure status the endpoint can return (400 validation, 401 credentials, 404 lookup, 409 conflict).
