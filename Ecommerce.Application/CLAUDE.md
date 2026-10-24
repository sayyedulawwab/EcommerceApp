# Ecommerce.Application — Conventions

Use-case layer: commands, queries, handlers, validators, responses, and the ports (interfaces) that Infrastructure implements. It references **Domain only**, plus EF Core (for `DbSet<T>` and LINQ), FluentValidation, Scrutor, Serilog and logging abstractions. It does not reference Dapper or Npgsql.

## Folder layout

```
Abstractions/
  Auth/        IAuthService (salt + hash), IJwtService (access token)
  Behaviors/   LoggingDecorator, ValidationDecorator
  Caching/     ICacheService
  Clock/       IDateTimeProvider
  Data/        IApplicationDbContext (DbSets + SaveChangesAsync)
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
- **Domain event handlers** (`internal sealed class XDomainEventHandler : IDomainEventHandler<XDomainEvent>`) live in Application and are picked up by the scan. Put each in the module folder next to the use case that triggers it (`Users/Register/SendWelcomeEmailDomainEventHandler.cs`, `Orders/PlaceOrder/SendOrderConfirmationDomainEventHandler.cs`). The event carries only IDs, so the handler loads what it needs with `AsNoTracking()` and returns quietly if the entity is gone.

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
    IApplicationDbContext dbContext,
    IDateTimeProvider dateTimeProvider)
    : ICommandHandler<EditCategoryCommand, Guid>
{
    public async Task<Result<Guid>> Handle(EditCategoryCommand request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories.FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);

        if (category is null)
        {
            return Result.Failure<Guid>(CategoryErrors.NotFound);
        }

        category = Category.Update(
            category,
            new CategoryName(request.Name),
            new CategoryCode(request.Code),
            dateTimeProvider.UtcNow);

        await dbContext.SaveChangesAsync(cancellationToken);

        return category.Id.Value;
    }
}
```

Rules:

- **`internal sealed class`** with a **primary constructor**. Dependencies are `IApplicationDbContext`, query repositories, and Application ports (`IDateTimeProvider`, `ICacheService`, `IAuthService`, ...). Never inject `ApplicationDbContext` or other Infrastructure types.
- The `Handle` parameter is named `request`.
- **Ordering:**
  1. Load each referenced entity. If one is missing, `return Result.Failure<T>({Entity}Errors.NotFound);`.
  2. Run business checks such as duplicates. Failures return `{Entity}Errors.X` (e.g. `UserErrors.AlreadyExists` when `dbContext.Users.AnyAsync(...)` finds the email).
  3. Build value objects from primitives with their `Create` (`ProductName.Create(request.Name)`, `Currency.FromCode(request.PriceCurrency)`, `Rating.Create(...)`). Build them before step 1 or 2 when a query needs them (`RegisterUserCommandHandler` checks for the email). If any fails, return `ValidationError.FromResults([name, description, currency])` (400 with every failed value):
     ```csharp
     if (name.IsFailure || description.IsFailure || currency.IsFailure)
     {
         return Result.Failure<Guid>(ValidationError.FromResults([name, description, currency]));
     }
     ```
  4. Change state **only through the domain**: `X.Create(...)`, `X.Update(x, ...)`, `order.AddOrderItem(...)`. Never set entity properties from a handler. Domain methods that return `Result` are propagated the same way:
     ```csharp
     Result addItemResult = order.AddOrderItem(product, item.Quantity, dateTimeProvider.UtcNow);

     if (addItemResult.IsFailure)
     {
         return Result.Failure<Guid>(addItemResult.Error);
     }
     ```
  5. Stage new or removed entities with `dbContext.{Set}.Add` / `Remove`. An entity loaded through the DbContext is tracked, so an edit needs no `Update` call.
  6. `await dbContext.SaveChangesAsync(cancellationToken);`. Exactly once, at the end, and **never on a failure path**.
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
- **Every command has a validator.** String fields get `.MaximumLength(...)` matching their column (names/codes/emails 200, descriptions 2000), currency codes get `.Must(Currency.IsSupported)`, money amounts get `.GreaterThan(0)`, and stock quantities get `.GreaterThanOrEqualTo(0)`. The value objects check the same rules again, so the validator is for early, per-field feedback, not the only guard.
- Collections: `RuleFor(c => c.Items).NotEmpty();` plus `RuleForEach(c => c.Items).ChildRules(item => { item.RuleFor(i => i.Quantity).GreaterThan(0); });` (see `PlaceOrderCommandValidator`).
- On failure the decorator returns `ValidationError` (400). Its `Errors` contains `Error.BadRequest(PropertyName, ErrorMessage)` per failure, and the handler is never called.

---

## Query handlers & responses

### DbContext queries (default for get-by-id / get-all)

```csharp
internal sealed class GetCategoryByIdQueryHandler(IApplicationDbContext dbContext)
    : IQueryHandler<GetCategoryByIdQuery, CategoryResponse>
{
    public async Task<Result<CategoryResponse>> Handle(GetCategoryByIdQuery request, CancellationToken cancellationToken)
    {
        Category? category = await dbContext.Categories
            .AsNoTracking()
            .FirstOrDefaultAsync(category => category.Id == new CategoryId(request.Id), cancellationToken);

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

- Use `AsNoTracking()` on reads that don't change anything, and `Include` for the children the response needs (`GetAllOrdersQueryHandler`).
- Load many entities by ID in one query (`Where(p => ids.Contains(p.Id))`), never one query per item in a loop (`PlaceOrderCommandHandler`).

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

### Query repositories (hand-written SQL with Dapper)

Commands and simple reads use `IApplicationDbContext`. A read goes behind a **query repository** when the SQL is better written by hand: joins over several tables that return selected columns, search with dynamic filters and paging, stored procedures, or operations that are easier or faster in raw SQL. The interface lives in Application (`Products/IProductQueryRepository.cs`), the Dapper implementation in Infrastructure (`Repositories/ProductQueryRepository.cs`). The handler keeps the defaults, caching and paging:

```csharp
IReadOnlyList<ProductResponse> products = await productQueryRepository.SearchAsync(request, page, pageSize, cancellationToken);

long totalRecords = products.Count > 0 ? products[0].TotalRecords : 0;

await cacheService.SetAsync(cacheKey, products, cancellationToken);

return PagedList<ProductResponse>.Create(products, page, pageSize, totalRecords);
```

Rules:

- Name it `I{Entity}QueryRepository` with methods named for the operation (`SearchAsync`). Reads return response types, never entities. A stored procedure or a write that is easier in raw SQL goes behind the same kind of interface, as a named method.
- Don't add a query repository for reads LINQ handles well. Don't add generic CRUD repositories.
- Default `page = 1` and `pageSize = 10` when the input is ≤ 0.
- Return `PagedList<T>.Create(items, page, pageSize, totalCount)`. It lives in `Application/Products/PagedList.cs` and exposes `Items`, `Page`, `PageSize`, `TotalCount`, `HasPreviousPage`, `HasNextPage`. Reuse it; don't create another paging type.
- The SQL rules (snake_case, parameters, whitelisted `ORDER BY`, `COUNT(*) OVER ()`) are in the Infrastructure guide.

---

## Caching

- `ICacheService`: `GetAsync<T>`, `SetAsync<T>`, `RemoveAsync`, `RemoveByPrefixAsync`. Backed by Redis, with no expiration set.
- **Cached queries** build the key `"{prefix}-{hash}"`, where the hash is Base64(SHA-256(JSON of every query parameter)). Return the cached value if it is present and non-empty; otherwise query and `SetAsync`.
- **Writes that affect cached data** call `await cacheService.RemoveByPrefixAsync("products", cancellationToken);` **after** `SaveChangesAsync`. Every product command (Add/Edit/Delete) does this. Use a per-module prefix for any new cached query.

---

## Auth use cases

- Register: `dbContext.Users.AnyAsync(user => user.Email == new Email(...))` duplicate check → `UserErrors.AlreadyExists`, then `IAuthService.GenerateSalt()` + `HashPassword(password, salt)`, then `User.Create(..., isAdmin: false)`.
- Login: load by email, then `IAuthService.VerifyPassword(password, salt, hash)`. A missing user **and** a wrong password both return `UserErrors.InvalidCredentials` (401), so the API never reveals whether an email is registered. Then call `IJwtService.GetAccessToken(email, userId)` and return `AccessTokenResponse`.
- Never compare password hashes in a handler; always go through `VerifyPassword` (constant-time, handles legacy hashes).
