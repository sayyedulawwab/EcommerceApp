# Ecommerce.Application.UnitTests — Conventions

Unit tests for Application command/query handlers. The project references **Ecommerce.Application only** (plus Domain transitively). Tests mock the Domain/Application interfaces and never touch Infrastructure, the database or HTTP.

- xUnit (`Xunit` is a global using), **Moq**, default **`Assert.*` only** (no FluentAssertions).
- Handlers are `internal`. `InternalsVisibleTo("Ecommerce.Application.UnitTests")` in `Ecommerce.Application.csproj` makes them constructible here.
- Run: `dotnet test Ecommerce.Application.UnitTests/Ecommerce.Application.UnitTests.csproj`.

## Layout & naming

- `{Module}/{HandlerName}Tests.cs`, e.g. `Users/RegisterUserCommandHandlerTests.cs`. Use **one test class per handler**.
- Test names: **`Handle_Should_{ExpectedOutcome}_When{Condition}`**, e.g. `Handle_Should_ReturnFailureResult_WhenEmailIsNotUnique`, `Handle_Should_NotCallUnitOfWork_WhenEmailIsNotUnique`.
- `[Fact]` per scenario; `[Theory]` + `[InlineData]` for boundary tables (e.g. `Rating` 0/1/5/6).

## Structure

```csharp
public class RegisterUserCommandHandlerTests
{
    private readonly Mock<IAuthService> _authServiceMock;
    private readonly Mock<IUserRepository> _userRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;

    public RegisterUserCommandHandlerTests()
    {
        _userRepositoryMock = new();
        _unitOfWorkMock = new();
        _authServiceMock = new();
    }

    [Fact]
    public async Task Handle_Should_ReturnFailureResult_WhenEmailIsNotUnique()
    {
        // Arrange
        var existingUser = User.Create(new FirstName("Existing"), new LastName("User"), new Email("test@gmail.com"),
            "existingPasswordHash", "existingPasswordSalt", isAdmin: false);

        var command = new RegisterUserCommand("Existing", "User", "test@gmail.com", "test12345");

        _userRepositoryMock.Setup(x => x.GetByEmail(It.IsAny<string>()))
            .ReturnsAsync(existingUser);

        var handler = new RegisterUserCommandHandler(
            _authServiceMock.Object,
            _userRepositoryMock.Object,
            _unitOfWorkMock.Object);

        // Act
        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(UserErrors.AlreadyExists, result.Error);
    }
}
```

Rules:

- **Mocks** are `private readonly Mock<T> _{name}Mock` fields, created with `new()` in the **constructor**. Don't use shared setup methods or static state.
- Construct the handler under test in **Arrange** from the mocks' `.Object`, or through a `private XHandler CreateHandler()` helper when most tests build it the same way.
- **AAA:** `// Arrange`, `// Act`, `// Assert` comments in every test.
- **Domain objects use their real factories** (`User.Create(...)`, `Category.Create(...)`, `Product.Create(...)`). Never mock entities or value objects.
- Handlers that use `IDateTimeProvider` get a `Mock<IDateTimeProvider>` with `Setup(x => x.UtcNow).Returns(fixedUtcDate)`. Never rely on the real clock.
- Handlers that use `ICacheService` get a mock, and the test verifies `RemoveByPrefixAsync("products", ...)` on success.
- For repeated complex inputs, add a private static factory (`private static AddProductCommand MakeCommand(...)`) instead of duplicating the shape across tests.
- Settings: `Options.Create(new XOptions { ... })`; never mock `IOptions<T>`.

## Assertions

```csharp
// Success
Assert.True(result.IsSuccess);
Assert.NotEqual(Guid.Empty, result.Value);

// Failure: compare against the domain error instance
Assert.True(result.IsFailure);
Assert.Equal(CategoryErrors.NotFound, result.Error);

// Interactions
_userRepositoryMock.Verify(x => x.Add(It.Is<User>(u => u.Id.Value == result.Value)), Times.Once);
_unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

// Capture for property assertions
Product? captured = null;
_productRepositoryMock.Setup(r => r.Add(It.IsAny<Product>())).Callback<Product>(p => captured = p);
```

## Required coverage per handler

- Happy path: success result **and** the repository `Add`/`Update`/`Remove` and `SaveChangesAsync` each called `Times.Once`.
- Each missing lookup (`GetByIdAsync` returns `null`): the matching `{Entity}Errors.NotFound`, with `SaveChangesAsync` `Times.Never`.
- Each business-rule failure (duplicate, invalid VO such as `Rating.Invalid`, invalid credentials): the matching domain error, with no writes.
- Side effects such as cache invalidation are verified on success and `Times.Never` on failure.

Validators can be tested directly (`new AddProductCommandValidator().Validate(command)`, then assert on `IsValid` / `Errors`). Remember that the validation decorator only runs for commands.
