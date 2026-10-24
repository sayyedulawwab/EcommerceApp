# Ecommerce.Infrastructure — Conventions

Implements the Application ports, including `IApplicationDbContext`. Persistence is **EF Core + Npgsql (PostgreSQL)** with snake_case naming, plus Dapper query repositories for hand-written SQL.

## Folder layout

```
ApplicationDbContext.cs     DbContext + IApplicationDbContext + domain-event dispatch
Auth/                       AuthService, JwtService, JwtOptions, JwtOptionsSetup, JwtBearerOptionsSetup
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
- `order_items.product_id` and `orders.coupon_id` are `Restrict`, not cascade: deleting a product or coupon must not delete order history. `DeleteProduct` returns `ProductErrors.HasOrders` (409) first.
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
- Existing migrations: `Initial`, `UpdateDateTimeFields`, `MakeOrderUpdatedOnUtcNullable`, `AddProductConcurrencyToken`, `AddCouponsAndOrderCancellation` (backfills `order_items.unit_price_*` from the product's price at migration time).
- The only acceptable hand edit is adding a data fix-up (`migrationBuilder.Sql(...)`) to a **new** migration you just generated, e.g. resetting values that the old schema forced (see `MakeOrderUpdatedOnUtcNullable`). Never edit a migration that has already been applied anywhere.
- Before committing, check that the generated `Up`/`Down` contains only the change you intended; anything else means the model drifted.
- Migrations are applied at startup **in Development only**, by `app.ApplyMigrations()` (extension in `Ecommerce.API/Extensions/MigrationExtensions.cs`). It runs **`EnsureDeleted()`**, then `Migrate()`, then `DataSeeder.SeedData`. The dev database is recreated on every run.

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

- `JwtService`: HS256, claims `ClaimTypes.Email` + `ClaimTypes.NameIdentifier` (user id), 1-hour expiry, issuer/audience/secret from the `Jwt` section.
- `JwtBearerOptionsSetup` (`IConfigureNamedOptions<JwtBearerOptions>`) validates issuer, audience, lifetime and signing key.
- `AuthService`: 128-bit random salt (Base64), and **PBKDF2-HMAC-SHA512, 210,000 iterations, 64-byte output** (`Rfc2898DeriveBytes.Pbkdf2`, no extra package), stored as Base64.
- `VerifyPassword` compares in constant time (`CryptographicOperations.FixedTimeEquals`). It also accepts the **legacy** `SHA256(password + salt)` hash, so accounts registered before the switch can still log in. New hashes are always PBKDF2. Changing the parameters invalidates existing hashes unless you add another fallback.

---

## Caching

- `CacheService` serializes with **Newtonsoft.Json** and uses `PrivateResolver` plus `AllowNonPublicDefaultConstructor`, so types with private setters and constructors round-trip.
- Keys are tracked in a **static in-process `ConcurrentDictionary`** to support `RemoveByPrefixAsync`. Prefix removal therefore only clears keys written by the same app instance.

---

## Seeding (`DataSeeder`, Development only)

Seeds 10 users (password `test1234`, random `IsAdmin`), 10 categories and 100 products (BDT) with Bogus, always through domain factories (`User.Create`, `Category.Create`, `Product.Create`).
