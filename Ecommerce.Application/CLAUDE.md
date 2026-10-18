# Ecommerce.Application — Conventions

Use-case layer: commands, queries, handlers, validators, responses, and the ports (interfaces) that Infrastructure implements. It references **Domain only**, plus Dapper, FluentValidation, Scrutor, Serilog and logging abstractions.

## Folder layout

```
Abstractions/
  Auth/        IAuthService (salt + hash), IJwtService (access token)
  Behaviors/   LoggingDecorator, ValidationDecorator
  Caching/     ICacheService
  Clock/       IDateTimeProvider
  Data/        ISqlConnectionFactory (Dapper read side)
  Emails/      IEmailService
  Messaging/   ICommand, ICommand<T>, IQuery<T>, ICommandHandler<...>, IQueryHandler<...>
Exceptions/    ConcurrencyException
{Module}/
  {Entity}Response.cs            shared response at module root
  {Verb}{Entity}/                one folder per operation
    {Verb}{Entity}Command.cs | {Name}Query.cs
    {...}Handler.cs
    {...}Validator.cs            commands only
    {Feature}Response.cs         feature-specific response, if any
DependencyInjection.cs           AddApplication()
```

---

## Messaging abstractions (no MediatR)

```csharp
public interface ICommand;
public interface ICommand<TResponse>;
public interface IQuery<TResponse>;

public interface ICommandHandler<in TCommand> where TCommand : ICommand
{ Task<Result> Handle(TCommand command, CancellationToken cancellationToken); }

public interface ICommandHandler<in TCommand, TResponse> where TCommand : ICommand<TResponse>
{ Task<Result<TResponse>> Handle(TCommand command, CancellationToken cancellationToken); }

public interface IQueryHandler<in TQuery, TResponse> where TQuery : IQuery<TResponse>
{ Task<Result<TResponse>> Handle(TQuery query, CancellationToken cancellationToken); }
```

Controllers inject the closed handler interface directly (`ICommandHandler<AddProductCommand, Guid>`). There is no mediator or dispatcher.

### DI and decorators (`DependencyInjection.AddApplication`)

- Scrutor scans this assembly and registers **every** `IQueryHandler<,>`, `ICommandHandler<>`, `ICommandHandler<,>` and `IDomainEventHandler<>` implementation (including `internal` ones) as **scoped**. New handlers need no registration.
- Decorators are applied with `TryDecorate`. The resulting order is **`LoggingDecorator` → `ValidationDecorator` → handler** for commands, and **`LoggingDecorator` → handler** for queries.
- `AddValidatorsFromAssembly` auto-registers all validators.
- **Query validators never run**, because no validation decorator wraps `IQueryHandler`. Validate query input in the handler (as `SearchProductQueryHandler` does with page defaults and a sort whitelist).
- A new cross-cutting concern is a new decorator class in `Abstractions/Behaviors/`, registered with `TryDecorate`. Only add one when asked; there are no MediatR-style pipeline behaviors.
- **Domain event handlers** (`internal sealed class XDomainEventHandler : IDomainEventHandler<XDomainEvent>`) live in Application and are picked up by the scan. None exist yet. Put the first one in the module folder next to the use case that triggers it (e.g. `Users/Register/SendWelcomeEmailDomainEventHandler.cs`).

---

## Commands & queries

**Positional records**, one per file, carrying **primitives only** (`Guid`, `string`, `decimal`, `int`, nullable for optional filters). Never use domain types.

```csharp
public record AddProductCommand(string Name, string Description, string PriceCurrency, decimal PriceAmount, int Quantity, Guid CategoryId) : ICommand<Guid>;
public record EditCategoryCommand(Guid Id, string Name, string Code) : ICommand<Guid>;
public record DeleteCategoryCommand(Guid Id) : ICommand<Guid>;

public record GetCategoriesQuery() : IQuery<IReadOnlyList<CategoryResponse>>;
public record GetProductByIdQuery(Guid Id) : IQuery<ProductResponse>;
public record SearchProductQuery(Guid? CategoryId, decimal? MinPrice, decimal? MaxPrice, string? Keyword,
    int Page, int PageSize, string? SortColumn, string? SortOrder) : IQuery<PagedList<ProductResponse>>;

// Nested line items: a small record in the same file
public record OrderStockItem(Guid ProductId, int Quantity);
public record PlaceOrderCommand(Guid UserId, List<OrderStockItem> OrderStockItems) : ICommand<Guid>;
```

- **Write commands return `Guid`**: the affected entity's id (create, edit and delete alike).
- List queries return `IReadOnlyList<{Entity}Response>`. Single-item queries return `{Entity}Response`. Paged queries return `PagedList<{Entity}Response>`.
- The acting user is a command field (`Guid UserId`) supplied by the controller from JWT claims.
- Login is modeled as a query (`LoginUserQuery : IQuery<AccessTokenResponse>`) because it changes no state.

---

## Command handlers

```csharp
internal sealed class EditCategoryCommandHandler(
    ICategoryRepository categoryRepository,
    IUnitOfWork unitOfWork,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        category = Category.Update(
            category,
            new CategoryName(request.Name),
            new CategoryCode(request.Code),
            dateTimeProvider.UtcNow);

        categoryRepository.Update(category);

        await unitOfWork.SaveChangesAsync(cancellationToken);

        return category.Id.Value;
    }
}
```

Rules:

- **`internal sealed class`** with a **primary constructor**. Dependencies are Domain repository interfaces, `IUnitOfWork`, and Application ports (`IDateTimeProvider`, `ICacheService`, `IAuthService`, ...). Never inject `ApplicationDbContext` or other Infrastructure types.
- The `Handle` parameter is named `request`.
- **Ordering:**
  1. Load each referenced entity. If one is missing, `return Result.Failure<T>({Entity}Errors.NotFound);`.
  2. Run business checks such as duplicates. Failures return `{Entity}Errors.X` (e.g. `UserErrors.AlreadyExists` when `GetByEmail` finds a user).
  3. Build value objects from primitives (`new ProductName(request.Name)`, `new Money(request.PriceAmount, Currency.FromCode(request.PriceCurrency))`, `Rating.Create(...)`). **If a VO factory returns `Result`, check `IsFailure` and return its error.**
  4. Change state **only through the domain**: `X.Create(...)`, `X.Update(x, ...)`, `order.AddOrderItem(...)`. Never set entity properties from a handler. Domain methods that return `Result` are propagated the same way:
     ```csharp
     Result addItemResult = order.AddOrderItem(product, item.Quantity, dateTimeProvider.UtcNow);

     if (addItemResult.IsFailure)
     {
         return Result.Failure<Guid>(addItemResult.Error);
     }
     ```
  5. Stage the change with the repository's `Add` / `Update` / `Remove`.
  6. `await unitOfWork.SaveChangesAsync(cancellationToken);`. Exactly once, at the end, and **never on a failure path**.
  7. Invalidate the cache if the write affects cached reads (see Caching).
  8. `return entity.Id.Value;` (implicit `Result<Guid>`).
- Timestamps always come from `dateTimeProvider.UtcNow`.
- Pass `cancellationToken` through every async call.
- Side effects such as email belong in a domain-event handler, not inline here.

---

## Validators

```csharp
public class AddProductCommandValidator : AbstractValidator<AddProductCommand>
{
    public AddProductCommandValidator()
    {
        RuleFor(c => c.Name).NotEmpty();
        RuleFor(c => c.PriceAmount).NotEmpty();
        RuleFor(c => c.CategoryId).NotEmpty();
    }
}
```

- One `public class {Command}Validator : AbstractValidator<{Command}>` per command, in its feature folder, with a **parameterless** constructor.
- Lambda parameter `c` (or the entity's initial, e.g. `r` for reviews).
- Messages use FluentValidation defaults, or inline `.WithMessage("Please provide a comment.")` when a friendlier message helps. There is no message catalog.
- Validators check **shape** (required, ranges, lengths). Rules that need the database (existence, uniqueness) go in the handler and return a domain error.
- **Every command has a validator.** String fields get `.MaximumLength(...)` matching their column (names/codes/emails 200, descriptions 2000), currency codes get `.Must(Currency.IsSupported)`, money amounts get `.GreaterThan(0)`, and stock quantities get `.GreaterThanOrEqualTo(0)`. Input that would otherwise fail in the database or in `Currency.FromCode` must be a 400 here.
- Collections: `RuleFor(c => c.Items).NotEmpty();` plus `RuleForEach(c => c.Items).ChildRules(item => { item.RuleFor(i => i.Quantity).GreaterThan(0); });` (see `PlaceOrderCommandValidator`).
- On failure the decorator returns `ValidationError` (400). Its `Errors` contains `Error.BadRequest(PropertyName, ErrorMessage)` per failure, and the handler is never called.

---

## Query handlers & responses

### Repository-backed queries (default for get-by-id / get-all)

```csharp
internal sealed class GetCategoryByIdQueryHandler(ICategoryRepository categoryRepository)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        Category? category = await categoryRepository.GetByIdAsync(new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<CategoryResponse>(CategoryErrors.NotFound);
        }

        var categoryResponse = new CategoryResponse()
        {
            Id = category.Id.Value,
            Name = category.Name.Value,
            Code = category.Code.Value
        };

        return categoryResponse;
    }
}
```

### Responses

```csharp
public sealed class ProductResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; }
    public decimal PriceAmount { get; init; }
    public string PriceCurrency { get; init; }
    public DateTime CreatedOnUtc { get; init; }
    public DateTime? UpdatedOnUtc { get; init; }
    public List<ReviewResponse> Reviews { get; init; } = [];
}
```

- `public sealed class` with `{ get; init; }` properties. The one exception is the tiny `sealed record AccessTokenResponse(string AccessToken)`.
- **Flatten value objects to primitives**: IDs as `Guid` (`x.Id.Value`), wrapped strings via `.Value`, `Money` as `{Prop}Amount` + `{Prop}Currency` (`Price.Currency.Code`), enums via `.ToString()`.
- Child collections are nested response lists initialized to `[]`.
- **Map inline in the handler** with object initializers and LINQ `Select`. Never return domain entities, and don't add mapper classes or AutoMapper.

### Dapper read side (search, filtering, paging)

For projections that would be awkward or slow through EF, query handlers use Dapper via `ISqlConnectionFactory` (reference: `SearchProductQueryHandler`):

```csharp
using IDbConnection connection = sqlConnectionFactory.CreateConnection();   // returns an already-open connection

var sqlBuilder = new StringBuilder(@"
    SELECT
        p.id AS Id,
        p.price_amount AS PriceAmount,
        p.created_on_utc AS CreatedOnUtc,
        COUNT(*) OVER () AS TotalRecords
    FROM products AS p");

var conditions = new List<string>();
if (request.CategoryId.HasValue)
{
    conditions.Add("p.category_id = @CategoryId");
}
// ... WHERE string.Join(" AND ", conditions)

string[] allowedSortColumns = ["Name", "PriceAmount", "CreatedOnUtc"];
// ORDER BY {whitelisted column} {ASC|DESC}
// OFFSET (@PageSize * (@Page - 1)) ROWS FETCH NEXT @PageSize ROWS ONLY;

IEnumerable<ProductResponse> products = await connection.QueryAsync<ProductResponse>(sql, new { request.CategoryId, Page = page, PageSize = pageSize });
return PagedList<ProductResponse>.Create(products, page, pageSize, totalRecords);
```

Rules:

- Write SQL against **snake_case** tables/columns and alias each column **`AS PascalCase`** to match the response property.
- **Always parameterize values** (`@Param` plus an anonymous object). Never interpolate user input. Dynamic `ORDER BY` columns come **only from a whitelist array**; direction is normalized to `ASC`/`DESC`.
- Get the paged total from **`COUNT(*) OVER () AS TotalRecords`** on each row (no second COUNT query), then read it from `FirstOrDefault()?.TotalRecords ?? 0`.
- Default `page = 1` and `pageSize = 10` when the input is ≤ 0.
- Return `PagedList<T>.Create(items, page, pageSize, totalCount)`. It lives in `Application/Products/PagedList.cs` and exposes `Items`, `Page`, `PageSize`, `TotalCount`, `HasPreviousPage`, `HasNextPage`. Reuse it; don't create another paging type.

---

## Caching

- `ICacheService`: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `RemoveByPrefixAsync`. Backed by Redis, with no expiration set.
- **Cached queries** build the key `"{prefix}-{hash}"`, where the hash is Base64(SHA-256(JSON of every query parameter)). Return the cached value if it is present and non-empty; otherwise query and `SetAsync`.
- **Writes that affect cached data** call `await cacheService.RemoveByPrefixAsync("products", cancellationToken);` **after** `SaveChangesAsync`. Every product command (Add/Edit/Delete) does this. Use a per-module prefix for any new cached query.

---

## Auth use cases

- Register: `IUserRepository.GetByEmail` duplicate check → `UserErrors.AlreadyExists`, then `IAuthService.GenerateSalt()` + `HashPassword(password, salt)`, then `User.Create(..., isAdmin: false)`.
- Login: load by email, then `IAuthService.VerifyPassword(password, salt, hash)`. A missing user **and** a wrong password both return `UserErrors.InvalidCredentials` (401), so the API never reveals whether an email is registered. Then call `IJwtService.GetAccessToken(email, userId)` and return `AccessTokenResponse`.
- Never compare password hashes in a handler; always go through `VerifyPassword` (constant-time, handles legacy hashes).
