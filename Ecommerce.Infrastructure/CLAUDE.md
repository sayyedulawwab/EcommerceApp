# Ecommerce.Infrastructure — Conventions

Implements the Domain repository interfaces and the Application ports. Persistence is **EF Core + Npgsql (PostgreSQL)** with snake_case naming, plus a Dapper connection factory for the read side.

## Folder layout

```
ApplicationDbContext.cs     DbContext + IUnitOfWork + domain-event dispatch
Auth/                       AuthService, JwtService, JwtOptions, JwtOptionsSetup, JwtBearerOptionsSetup
Caching/                    CacheService (IDistributedCache/Redis), PrivateResolver
Clock/                      DateTimeProvider
Configurations/             {Entity}Configuration : IEntityTypeConfiguration<{Entity}>
Data/                       SqlConnectionFactory (Npgsql), DateOnlyTypeHandler (Dapper), DataSeeder (Bogus)
DomainEvents/               IDomainEventsDispatcher, DomainEventsDispatcher
Emails/                     EmailService (stub, no-op)
Migrations/                 EF generated code: never hand-edit
Repositories/               Repository<TEntity, TEntityId> base + one {Entity}Repository per aggregate
DependencyInjection.cs      AddInfrastructure(services, configuration)
```

---

## ApplicationDbContext

```csharp
public sealed class ApplicationDbContext(DbContextOptions options, IDomainEventsDispatcher domainEventsDispatcher)
    : DbContext(options), IUnitOfWork
```

- **There are no `DbSet<T>` properties.** Use `DbContext.Set<T>()`.
- `OnModelCreating` → `ApplyConfigurationsFromAssembly(...)`, so a new `IEntityTypeConfiguration<T>` is picked up automatically.
- `SaveChangesAsync`:
  1. `base.SaveChangesAsync` (commits).
  2. Collects domain events from every tracked `IEntity`, clears them, and calls `IDomainEventsDispatcher.DispatchAsync`.
  3. Rethrows `DbUpdateConcurrencyException` as `Application.Exceptions.ConcurrencyException`.
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
               .HasConversion(name => name.Value, value => new ProductName(value));

        builder.OwnsOne(product => product.Price, priceBuilder =>
        {
            priceBuilder.Property(money => money.Amount)
                        .HasColumnType("decimal(18, 2)");

            priceBuilder.Property(money => money.Currency)
                        .HasConversion(currency => currency.Code, code => Currency.FromCode(code))
                        .HasDefaultValue(Currency.FromCode("BDT"));
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
- **Single-value VOs** use `HasConversion(vo => vo.Value, value => new VO(value))`. Validated VOs convert back through their factory (`Rating.Create(value).Value`).
- **`Money`** uses `OwnsOne` with `decimal(18, 2)` amount and currency stored as its code via `Currency.FromCode`, default `"BDT"`. The resulting columns are `{prop}_amount` and `{prop}_currency`.
- Max lengths: names/codes/emails `200`, descriptions `2000`.
- **Cross-aggregate FKs** use `HasOne<OtherAggregate>().WithMany().HasForeignKey(x => x.OtherId)`, with no navigation property. Use a real navigation only where the domain has one (`OrderItem.Product`, `Order.OrderItems`).
- List each persisted property explicitly (`builder.Property(x => x.CreatedOnUtc);`), even when it needs no extra config, for readability.

---

## Repositories

```csharp
internal abstract class Repository<TEntity, TEntityId>(ApplicationDbContext dbContext)
    where TEntity : Entity<TEntityId>
    where TEntityId : class
{
    protected readonly ApplicationDbContext DbContext = dbContext;

    public virtual Task<IReadOnlyList<TEntity>> GetAllAsync(CancellationToken cancellationToken = default);
    public virtual Task<TEntity?> GetByIdAsync(TEntityId id, CancellationToken cancellationToken = default);
    public void Add(TEntity entity);     // DbContext.Add
    public void Remove(TEntity entity);  // DbContext.Remove
    public void Update(TEntity entity);  // DbContext.Update
}

internal sealed class ProductRepository(ApplicationDbContext dbContext)
    : Repository<Product, ProductId>(dbContext), IProductRepository
{
}
```

- `internal sealed class {Entity}Repository(ApplicationDbContext dbContext) : Repository<{Entity}, {Entity}Id>(dbContext), I{Entity}Repository`.
- Repositories **never call `SaveChangesAsync`**. Handlers commit through `IUnitOfWork`.
- **Override** `GetAllAsync`/`GetByIdAsync` to eager-load aggregate children (`OrderRepository` does `.Include(o => o.OrderItems).ThenInclude(oi => oi.Product)`).
- Add extra queries with LINQ over `DbContext.Set<T>()` and strongly typed ID comparisons (`review.ProductId == productId`). Raw SQL only via **`FromSqlInterpolated`** (parameterized), never `FromSqlRaw` with concatenation.
- Register each repository in `AddPersistence`: `services.AddScoped<IXRepository, XRepository>();`.

---

## Migrations

- Create with: `dotnet ef migrations add <Name> --project Ecommerce.Infrastructure --startup-project Ecommerce.API`.
- `Migrations/**` is marked `generated_code` in `.editorconfig`. **Never hand-edit it.** Change the configuration and generate a new migration.
- Existing migrations: `Initial`, `UpdateDateTimeFields`, `MakeOrderUpdatedOnUtcNullable`.
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
| `IUnitOfWork` → same `ApplicationDbContext`       | Scoped     | `sp.GetRequiredService<ApplicationDbContext>()`           |
| `I{Entity}Repository` → `{Entity}Repository`      | Scoped     | Registered explicitly, one line each                     |
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
