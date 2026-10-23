# Ecommerce.Domain — Conventions

Pure domain model with **no project or package dependencies** (BCL only, e.g. `System.Net.HttpStatusCode` for error codes): no EF Core, no Dapper, no serialization attributes. Persistence mapping lives in `Ecommerce.Infrastructure/Configurations/`.

## Folder layout

```
Abstractions/   Entity<TId>, IEntity, IDomainEvent, IDomainEventHandler<T>,
                Result, Result<T>, Error, ValidationError
Shared/         Money, Currency (cross-aggregate value objects)
{Module}/       {Entity}.cs, {Entity}Id.cs, one file per value object, {Entity}Errors.cs,
                Events/{Entity}CreatedDomainEvent.cs
```

Modules: `Categories`, `Products`, `Orders`, `Reviews`, `Users`.

---

## Entities

```csharp
public sealed class Product : Entity<ProductId>
{
    // 1. Private full constructor: every persisted field, passes id to base
    private Product(ProductId id, ProductName name, ProductDescription description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc) : base(id)
    {
        Name = name;
        // ...
        CreatedOnUtc = createdOnUtc;
    }

    // 2. Private parameterless constructor: for EF Core materialization
    private Product()
    {
    }

    // 3. Properties: FK/business fields, then timestamps. All { get; private set; }
    public CategoryId CategoryId { get; private set; }
    public ProductName Name { get; private set; }
    public Money Price { get; private set; }
    public int Quantity { get; private set; }
    public DateTime CreatedOnUtc { get; private set; }
    public DateTime? UpdatedOnUtc { get; private set; }

    // 4. Static factory: generates the id, raises the created event
    public static Product Create(ProductName name, ProductDescription description, Money price, int quantity, CategoryId categoryId, DateTime createdOnUtc)
    {
        var product = new Product(ProductId.New(), name, description, price, quantity, categoryId, createdOnUtc);

        product.RaiseDomainEvent(new ProductCreatedDomainEvent(product.Id));

        return product;
    }

    // 5. Static update: mutates and returns the same instance
    public static Product Update(Product product, ProductName name, /* ... */ DateTime updatedOnUtc)
    {
        product.Name = name;
        // ...
        product.UpdatedOnUtc = updatedOnUtc;

        return product;
    }
}
```

Rules:

- `public sealed class {Entity} : Entity<{Entity}Id>`. `Id` comes from the base class (`{ get; init; }`); don't redeclare it.
- **Two private constructors:** a full one (calls `: base(id)`) and an empty one for EF.
- **All properties use `{ get; private set; }`.** State changes only through the entity's static/instance methods; external code never assigns properties.
- Properties are typed as **value objects and strongly typed IDs**, not primitives (`ProductName`, `Money`, `CategoryId`). Plain `int`/`bool`/`string` is fine where no VO exists (`Quantity`, `IsAdmin`, `PasswordHash`).
- **Timestamps:** `CreatedOnUtc` (`DateTime`) is set by `Create`. `UpdatedOnUtc` (`DateTime?`) is set by `Update`. Both are **passed in by the caller** (from `IDateTimeProvider`); the domain never reads the clock.
- **`static Create(...)`** takes value objects plus `createdOnUtc`, generates the id with `{Entity}Id.New()`, raises `{Entity}CreatedDomainEvent`, and returns the entity.
- **`static Update({Entity} entity, ...)`** is the established pattern here. It takes the entity plus the mutable fields and `updatedOnUtc`, mutates the entity, and returns it. Handlers reassign: `product = Product.Update(product, ...)`. It never touches `Id` or `CreatedOnUtc`. Match this shape for new entities.
- **Reference other aggregates by ID** (`Product.CategoryId`, `Review.ProductId`, `Review.UserId`, `Order.UserId`). The one exception is `OrderItem.Product`, a navigation used for pricing and order responses.
- Factories may take a related aggregate when they need data from it (`Review.Create(Product product, ...)` stores `product.Id`).

### Aggregates with children

`Order` owns `OrderItem`s:

- Add children **only through the aggregate**: `Result result = order.AddOrderItem(product, quantity, createdOnUtc)`. It enforces the invariants (quantity > 0, every line in the same currency), calls `OrderItem.Create(...)`, and keeps `TotalPrice` as the sum of `price × quantity` across lines.
- **Invariant violations return `Result.Failure({Entity}Errors.X)`** (`OrderErrors.InvalidQuantity`, `OrderErrors.CurrencyMismatch`); callers propagate the error. Don't throw for expected business failures.
- Never call `order.OrderItems.Add(...)` from outside the aggregate. The list is exposed (`List<OrderItem> { get; private set; } = []`) only so EF can map it.
- Child entities are also `sealed`, extend `Entity<{Child}Id>`, and have their own static `Create`.

---

## Strongly typed IDs

One file per ID:

```csharp
public record ProductId(Guid Value)
{
    public static ProductId New() => new(Guid.NewGuid());
}
```

Infrastructure maps them with `HasConversion(id => id.Value, value => new ProductId(value))`. Application converts at the boundary: `new ProductId(request.Id)` going in, `product.Id.Value` coming out.

---

## Value objects

All value objects are **records**. Each lives in its own file in the module folder, or in `Shared/` if it is reused.

| Kind              | Shape                                                                                         | Examples                                    |
| ----------------- | --------------------------------------------------------------------------------------------- | ------------------------------------------- |
| Simple wrapper    | `public record ProductName(string Value);`                                                    | `CategoryName`, `CategoryCode`, `ProductDescription`, `Comment`, `Email`, `FirstName`, `LastName` |
| Validated         | private constructor, `public static Result<T> Create(...)`, `public static readonly Error Invalid` on the type | `Rating` (1–5)                |
| Smart enum        | private constructor, `static readonly` instances, `All`, `IsSupported(code)`, `FromCode(code)` | `Currency` (`Usd`, `Bdt`)                   |
| Composite         | positional record with behavior/operators                                                     | `Money(decimal Amount, Currency Currency)`  |

Validated VO template (from `Rating`):

```csharp
public record Rating
{
    public static readonly Error Invalid = new("Rating.Invalid", "The rating is invalid", HttpStatusCode.BadRequest);

    private Rating(int value)
    {
        Value = value;
    }

    public int Value { get; init; }

    public static Result<Rating> Create(int value)
    {
        if (value is < 1 or > 5)
        {
            return Result.Failure<Rating>(Invalid);
        }

        return new Rating(value);
    }
}
```

`Money`: `+` throws `InvalidOperationException` on a currency mismatch. Check currencies first and return a domain error, as `Order.AddOrderItem` does. Use `Money.Zero()` / `Money.Zero(currency)` and `IsZero()`. Amounts persist as `decimal(18, 2)`.

`Currency.FromCode` throws on an unknown code, so it is only safe after validation. Validators check `Currency.IsSupported(code)`, and handlers then call `Currency.FromCode(...)`. There is no unvalidated `Currency.Create`.

---

## Enums

Use explicit values starting at 1 (`OrderStatus.Placed = 1, ... Cancelled = 5`). They are stored as integers by default.

---

## Errors

One static class per entity, in the module folder:

```csharp
using System.Net;
using Ecommerce.Domain.Abstractions;

public static class ReviewErrors
{
    public static readonly Error NotEligible = new(
        "Review.NotEligible",
        "The review is not eligible because the order is not yet delivered",
        HttpStatusCode.BadRequest);
}
```

- Codes follow `"{Entity}.{Reason}"` (`Product.NotFound`, `User.AlreadyExists`, `Order.CurrencyMismatch`, `Rating.Invalid`).
- The status is a `System.Net.HttpStatusCode`. Choose it by meaning: missing → `NotFound`, duplicate/state clash → `Conflict`, invalid input/rule → `BadRequest`, bad credentials → `Unauthorized`. If you use a status not yet mapped in `Ecommerce.API/Extensions/ErrorMappingExtensions.cs`, add it there (otherwise it becomes a 500).
- Application handlers return these instances, and unit tests assert equality against them.

---

## Domain events

```csharp
namespace Ecommerce.Domain.Products.Events;
public sealed record ProductCreatedDomainEvent(ProductId ProductId) : IDomainEvent;
```

- `sealed record`, past-tense name, lives in `{Module}/Events/`. It **carries IDs only**; handlers reload what they need.
- Raised **inside the entity** with `RaiseDomainEvent(...)` (protected on `Entity<TId>`), typically in `Create`. Never raised from Application code.
- They are collected on the entity and dispatched by `ApplicationDbContext` **after** `SaveChangesAsync` succeeds (see the Infrastructure guide).
- Handlers implement `IDomainEventHandler<TEvent>` (`Task Handle(T domainEvent, CancellationToken cancellationToken)`).

