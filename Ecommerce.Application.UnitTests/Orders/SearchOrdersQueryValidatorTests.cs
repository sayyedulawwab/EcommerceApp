using Ecommerce.Application.Orders.SearchOrders;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Orders;

public class SearchOrdersQueryValidatorTests
{
    private static readonly SearchOrdersQueryValidator Validator = new();
    private static readonly DateTime FromUtc = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);
    private static readonly SearchOrdersQuery ValidQuery = new(Guid.CreateVersion7(), null, null, null, 1, 20, null, null);

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenOnlyTheRequiredValuesAreSet()
    {
        ValidationResult result = Validator.Validate(ValidQuery);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenEveryFilterIsValid()
    {
        SearchOrdersQuery query = ValidQuery with
        {
            Status = "cancelled",
            FromUtc = FromUtc,
            ToUtc = FromUtc,
            SortColumn = "totalprice",
            SortOrder = "desc",
        };

        ValidationResult result = Validator.Validate(query);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenUserIdIsEmpty()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { UserId = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.UserId));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPageIsZero()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { Page = 0 });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.Page));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(SearchOrdersQueryValidator.MaxPageSize + 1)]
    public void Validate_Should_ReturnError_WhenPageSizeIsOutOfRange(int pageSize)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { PageSize = pageSize });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.PageSize));
    }

    [Theory]
    [InlineData("Shipped")]
    [InlineData("99")]
    public void Validate_Should_ReturnError_WhenStatusIsNotAnOrderStatus(string status)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { Status = status });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.Status));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenToIsBeforeFrom()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { FromUtc = FromUtc, ToUtc = FromUtc.AddDays(-1) });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.ToUtc));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenSortColumnIsNotAllowed()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { SortColumn = "UserId" });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.SortColumn));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenSortOrderIsNotAscOrDesc()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { SortOrder = "up" });

        ValidationAssert.HasSingleError(result, nameof(SearchOrdersQuery.SortOrder));
    }
}
