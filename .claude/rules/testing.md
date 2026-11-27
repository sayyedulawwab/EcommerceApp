---
paths:
  - "Ecommerce.*Tests/**"
---

# Testing (summary; details in each test project's `CLAUDE.md`)

- xUnit `[Fact]` / `[Theory]` + `[InlineData]`, default **`Assert.*`** only. No mocking library.
- **AAA:** `// Arrange`, `// Act`, `// Assert` comments in every integration test.
- Handlers are tested over HTTP against PostgreSQL in Testcontainers, not with mocks. Never mock `DbSet`, and never swap in SQLite or the EF InMemory provider: they don't behave like PostgreSQL.
- Domain rules (`Order.AddOrderItem`, `Rating.Create`, ...) are plain unit tests of the entities: no database, no mocks.
- Validator rules are unit tests of the validator: build one valid command, change one field with `with`, and assert the single error's `PropertyName` (`ValidationAssert.HasSingleError`). Integration tests keep one 400 test per endpoint; the permutations live here.
- Build domain objects through their real factories (`User.Create(...)`).
- Assert failures against the **domain error instance**: `Assert.Equal(OrderErrors.InvalidQuantity, result.Error)`. Over HTTP, assert the status code and the database state.
- Naming: domain and validator tests `{Method}_Should_{Outcome}_When{Condition}`; integration tests `{Action}_Should{Outcome}_When{Condition}`.
- Use `Options.Create(new XOptions { ... })` for options in tests; never mock `IOptions<T>`.
