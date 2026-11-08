# Ecommerce.Infrastructure — Conventions

Implements the Application ports, including `IApplicationDbContext`. Persistence is **EF Core + Npgsql (PostgreSQL)** with snake_case naming, plus Dapper query repositories for hand-written SQL.

## Folder layout

```
ApplicationDbContext.cs     DbContext + IApplicationDbContext + domain-event dispatch
Auth/                       AuthService (bcrypt), PasswordHashingOptions, JwtService, JwtOptions, JwtOptionsSetup, JwtOptionsValidator, JwtBearerOptionsSetup
Caching/                    CacheService (IDistributedCache/Redis), PrivateResolver
Clock/                      DateTimeProvider
Configurations/             {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
Data/                       ISqlConnectionFactory + SqlConnectionFactory (Npgsql), DateOnlyTypeHandler (Dapper), DataSeeder (Bogus)
DomainEvents/               IDomainEventsDispatcher, DomainEventsDispatcher
Emails/                     EmailService (stub, no-op)
Migrations/                 EF generated code: never hand-edit
Repositories/               Dapper query repositories ({Entity}QueryRepository)
DependencyInjection.cs      AddInfrastructure(services, configuration)
```

---

## ApplicationDbContext

```csharp
public sealed class ApplicationDbContext(DbContextOptions options, IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IApplicationDbContext
```

- One `DbSet<T>` property per aggregate root, declared on `IApplicationDbContext` and implemented as `public DbSet<Product> Products => Set<Product>();`. Child entities (`OrderItem`) have no set; they load through their root.
- `OnModelCreating` → `ApplyConfigurationsFromAssembly(...)`, so a new `IEntityTypeConfiguration<T>` is picked up automatically.
- `SaveChangesAsync`:
  1. `base.SaveChangesAsync` (commits).
  2. Collects domain events from every tracked `IEntity`, clears them, and calls `IDomainEventsDispatcher.DispatchAsync`.
  3. Rethrows `DbUpdateConcurrencyException` as `Application.Exceptions.ConcurrencyException`, which the API returns as 409.
- `Product`, `Order` and `Coupon` have a concurrency token: a shadow `uint Version` mapped to PostgreSQL's `xmin` system column with `IsRowVersion()`. Two orders that read the same stock or the last coupon use can't both save, and two cancels of the same order can't both give the stock back. Npgsql doesn't create a column for `xmin`, so its migration only adds a history row.
- **Dispatch is synchronous and in-request, after commit, with no outbox.** `DomainEventsDispatcher` creates a new DI scope per event and resolves every `IDomainEventHandler<TEvent>`. A handler exception surfaces to the HTTP request (→ 500) even though the data is already saved. Keep handlers idempotent and resilient.

---

## Entity configurations

```csharp
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("products");

        builder.HasKey(product => product.Id);

        builder.Property(product => product.Id)
               .HasConversion(productId => productId.Value, value => new ProductId(value));

        builder.Property(product => product.Name)
               .HasMaxLength(200)
               .HasConversion(name => name.Value, value => ProductName.Create(value).Value);

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code).Value)
                        .HasDefaultValue(Currency.Bdt);
        });

        builder.Property(product => product.CreatedOnUtc);
        builder.Property(product => product.UpdatedOnUtc);

        builder.HasOne<Category>()
               .WithMany()
               .HasForeignKey(product => product.CategoryId);
    }
}
```

Rules:

- `internal sealed class {Entity}Configuration : IEntityTypeConfiguration<{Entity}>`, one per entity, in `Configurations/`.
- `ToTable("{snake_case_plural}")` explicitly. Columns get snake_case automatically from `UseSnakeCaseNamingConvention()`.
- **Every strongly typed ID** (key and FK) uses `HasConversion(id => id.Value, value => new XId(value))`.
- **Single-value VOs** convert back through their factory: `HasConversion(vo => vo.Value, value => ProductName.Create(value).Value)`. An invalid value in the database is a bug, so `.Value` throwing there is intended.
- **`Money`** uses `OwnsOne` with `decimal(18, 2)` amount and currency stored as its code via `Currency.FromCode(code).Value`, default `Currency.Bdt`. The resulting columns are `{prop}_amount` and `{prop}_currency`.
- Max lengths: names/codes/emails `200`, descriptions `2000`.
- `order_items.product_id`, `orders.coupon_id` and `products.category_id` are `Restrict`, not cascade: deleting a product, coupon or category must not delete order history or products. The delete handlers check first and return 409 (`HasOrders`, `HasProducts`); if a row is added between the check and the save, they catch the `DbUpdateException`, check again and return the same 409. `DeleteProduct` returns `ProductErrors.HasOrders` (409) first.
- **Cross-aggregate FKs** use `HasOne<OtherAggregate>().WithMany().HasForeignKey(x => x.OtherId)`, with no navigation property. Use a real navigation only where the domain has one (`OrderItem.Product`, `Order.OrderItems`).
- List each persisted property explicitly (`builder.Property(x => x.CreatedOnUtc);`), even when it needs no extra config, for readability.

---

## Query repositories (Dapper)

```csharp
internal sealed class ProductQueryRepository(ISqlConnectionFactory sqlConnectionFactory) : IProductQueryRepository
{
    public async Task<IReadOnlyList<ProductResponse>> SearchAsync(SearchProductQuery query, int page, int pageSize, CancellationToken cancellationToken = default)
    {
        using IDbConnection connection = sqlConnectionFactory.CreateConnection();   // returns an already-open connection

        // ... build the SQL, then
        CommandDefinition command = new(sql, new { query.CategoryId, Page = page, PageSize = pageSize }, cancellationToken: cancellationToken);

        IEnumerable<ProductResponse> products = await connection.QueryAsync<ProductResponse>(command);

        return products.ToList();
    }
}
```

- Only for SQL that is better written by hand (see the Application guide). There are no generic or CRUD repositories: handlers use `IApplicationDbContext`.
- `ISqlConnectionFactory` is internal to Infrastructure; Application never sees a connection.
- Write SQL against **snake_case** tables/columns and alias each column **`AS PascalCase`** to match the response property.
- **Always parameterize values** (`@Param` plus an anonymous object). Never interpolate user input. Dynamic `ORDER BY` columns come **only from a whitelist array**; direction is normalized to `ASC`/`DESC`.
- Get the paged total from **`COUNT(*) OVER () AS TotalRecords`** on each row (no second COUNT query).
- Pass the `cancellationToken` through `CommandDefinition`.
- Register each one in `AddPersistence`: `services.AddScoped<IXQueryRepository, XQueryRepository>();`.

---

## Migrations

- Create with: `dotnet ef migrations add <Name> --project Ecommerce.Infrastructure --startup-project Ecommerce.API`.
- `Migrations/**` is marked `generated_code` in `.editorconfig`. **Never hand-edit it.** Change the configuration and generate a new migration.
- Existing migrations: `Initial`, `UpdateDateTimeFields`, `MakeOrderUpdatedOnUtcNullable`, `AddProductConcurrencyToken`, `AddCouponsAndOrderCancellation` (backfills `order_items.unit_price_*` from the product's price at migration time), `RestrictCategoryDelete`.
- The only acceptable hand edit is adding a data fix-up (`migrationBuilder.Sql(...)`) to a **new** migration you just generated, e.g. resetting values that the old schema forced (see `MakeOrderUpdatedOnUtcNullable`). Never edit a migration that has already been applied anywhere.
- Before committing, check that the generated `Up`/`Down` contains only the change you intended; anything else means the model drifted.
- Migrations are applied at startup **in Development only**, by `app.ApplyMigrations()` (extension in `Ecommerce.API/Extensions/MigrationExtensions.cs`). It runs `Migrate()`, then `DataSeeder.SeedData` only if `users` is empty, so a restart keeps the data and doesn't insert the fixed accounts twice.

---

## Dependency injection (`AddInfrastructure`)

| Service                                           | Lifetime   | Notes                                                    |
| ------------------------------------------------- | ---------- | -------------------------------------------------------- |
| `IDateTimeProvider` → `DateTimeProvider`          | Transient  | `UtcNow => DateTime.UtcNow`                              |
| `IEmailService` → `EmailService`                  | Transient  | Stub, returns `Task.CompletedTask`                        |
| `IDomainEventsDispatcher` → `DomainEventsDispatcher` | Transient |                                                         |
| `IDistributedCache` (Redis)                       | –          | `ConnectionStrings:Redis`                                |
| `ICacheService` → `CacheService`                  | Singleton  |                                                          |
| `ApplicationDbContext`                            | Scoped     | `UseNpgsql(...).UseSnakeCaseNamingConvention()`, `ConnectionStrings:Database` |
| `IApplicationDbContext` → same `ApplicationDbContext` | Scoped | `sp.GetRequiredService<ApplicationDbContext>()`           |
| `I{Entity}QueryRepository` → `{Entity}QueryRepository` | Scoped | Registered explicitly, one line each                     |
| `ISqlConnectionFactory` → `SqlConnectionFactory`  | Singleton  | Returns an **opened** `NpgsqlConnection`                  |
| `IJwtService`, `IAuthService`                     | Singleton  |                                                          |

The Dapper `DateOnlyTypeHandler` is registered globally with `SqlMapper.AddTypeHandler`.

---

## Options pattern

Bind configuration with an `IConfigureOptions<T>` setup class, registered via `services.ConfigureOptions<TSetup>()`:

```csharp
internal sealed class JwtOptionsSetup(IConfiguration configuration) : IConfigureOptions<JwtOptions>
{
    private const string SectionName = "Jwt";

    public void Configure(JwtOptions options)
    {
        configuration.GetSection(SectionName).Bind(options);
    }
}
```

Consumers take `IOptions<T>`. Follow this pattern for new settings.

---

## Auth

- `JwtService`: issues the access token with `JsonWebTokenHandler` (not `JwtSecurityTokenHandler`): HS256, claims `ClaimTypes.Email` + `ClaimTypes.NameIdentifier` (user id) + `ClaimTypes.Role` for admins, 15-minute expiry (`Jwt:AccessTokenLifetimeMinutes`). It also makes refresh tokens: 32 bytes from `RandomNumberGenerator`, Base64Url, and hashes them with SHA-256 (hex) for storage.
- `JwtBearerOptionsSetup` (`IConfigureNamedOptions<JwtBearerOptions>`) validates issuer, audience, lifetime (30 s clock skew), signing key and the algorithm (HS256 only). Validation runs through `JsonWebTokenHandler`: `UseSecurityTokenValidators` stays `false`. The claims keep the `ClaimTypes.*` names `JwtService` wrote, so `ClaimTypes.NameIdentifier` resolves in controllers whatever `MapInboundClaims` is.
- `JwtOptionsValidator` (`IValidateOptions<JwtOptions>`, run by `ValidateOnStart`): the host fails to start when `Jwt:SecretKey` is missing or shorter than 32 bytes. There is **no signing key in any committed file**: set it with `dotnet user-secrets` (`UserSecretsId` is on the API project) or the `Jwt__SecretKey` environment variable. The one host that skips the check is the build-time OpenAPI generator (`GetDocument.Insider`), which boots the app without configuration.
- `AuthService`: **bcrypt** through `BCrypt.Net-Next`, work factor 12 by default (`PasswordHashing:WorkFactor`, accepted range 10 to 16). The salt is inside the hash, so `users` has no salt column. No pre-hashing: bcrypt only uses the first 72 bytes of a password, so the register validator rejects longer ones (72 UTF-8 bytes) and login never matches them.
- `VerifyPassword` returns `false` for a stored value that is not a bcrypt hash (no exception). `NeedsRehash` is true when the stored work factor differs from the configured one; login then saves a new hash. `VerifyAgainstDummyHash` does the same work for an unknown email, so login takes about as long whether or not the email exists.
- `RefreshToken` rows (table `refresh_tokens`, unique index on `token_hash`) are revoked with `ExecuteUpdateAsync` and a `RevokedOnUtc IS NULL` condition, so only one of two concurrent refreshes with the same token wins.
- `UniqueConstraintException` (a `DbUpdateException`): `ApplicationDbContext.SaveChangesAsync` throws it for PostgreSQL error 23505, with the constraint name. Handlers that already catch `DbUpdateException` keep working; the register handler catches this one to return 409.

---

## Caching

- `CacheService` serializes with **Newtonsoft.Json** and uses `PrivateResolver` plus `AllowNonPublicDefaultConstructor`, so types with private setters and constructors round-trip.
- Keys are tracked in a **static in-process `ConcurrentDictionary`** to support `RemoveByPrefixAsync`. Prefix removal therefore only clears keys written by the same app instance.

---

## Seeding (`DataSeeder`, Development only)

Seeds 10 users (password `test1234`, random `IsAdmin`), 10 categories and 100 products (BDT) with Bogus, always through domain factories (`User.Create`, `Category.Create`, `Product.Create`).
