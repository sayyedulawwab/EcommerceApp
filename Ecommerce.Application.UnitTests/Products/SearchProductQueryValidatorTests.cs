using Ecommerce.Application.Products.SearchProduct;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Products;

public class SearchProductQueryValidatorTests
{
    private static readonly SearchProductQueryValidator Validator = new();
    private static readonly SearchProductQuery ValidQuery = new(null, null, null, null, 1, 20, null, null);

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenOnlyThePagingIsSet()
    {
        ValidationResult result = Validator.Validate(ValidQuery);

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("Name")]
    [InlineData("PriceAmount")]
    [InlineData("CreatedOnUtc")]
    public void Validate_Should_ReturnNoErrors_WhenSortColumnIsAllowed(string sortColumn)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { SortColumn = sortColumn });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenPageSizeIsTheMaximum()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { PageSize = SearchProductQueryValidator.MaxPageSize });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenPagingIsZero()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { Page = 0, PageSize = 0 });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenPageSizeIsOverTheMaximum()
    {
        ValidationResult result = Validator.Validate(ValidQuery with { PageSize = SearchProductQueryValidator.MaxPageSize + 1 });

        ValidationAssert.HasSingleError(result, nameof(SearchProductQuery.PageSize));
    }

    [Theory]
    [InlineData("Quantity")]
    [InlineData("name")]
    [InlineData("created_on_utc; DROP TABLE products")]
    public void Validate_Should_ReturnError_WhenSortColumnIsNotAllowed(string sortColumn)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { SortColumn = sortColumn });

        ValidationAssert.HasSingleError(result, nameof(SearchProductQuery.SortColumn));
    }

    [Theory]
    [InlineData("key")]
    [InlineData("100%")]
    [InlineData("   ")]
    public void Validate_Should_ReturnNoErrors_WhenKeywordIsLongEnoughOrBlank(string keyword)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { Keyword = keyword });

        Assert.True(result.IsValid);
    }

    [Theory]
    [InlineData("e")]
    [InlineData("ab")]
    [InlineData(" ab ")]
    public void Validate_Should_ReturnError_WhenKeywordIsShorterThanTheMinimum(string keyword)
    {
        ValidationResult result = Validator.Validate(ValidQuery with { Keyword = keyword });

        ValidationAssert.HasSingleError(result, nameof(SearchProductQuery.Keyword));
    }
}
