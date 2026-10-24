using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;

namespace Ecommerce.Domain.UnitTests.Shared;

public class ValueObjectTests
{
    [Fact]
    public void ProductNameCreate_Should_ReturnName_WhenValueIsValid()
    {
        Result<ProductName> result = ProductName.Create("Keyboard");

        Assert.True(result.IsSuccess);
        Assert.Equal("Keyboard", result.Value.Value);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void ProductNameCreate_Should_ReturnInvalid_WhenValueIsEmpty(string? value)
    {
        Result<ProductName> result = ProductName.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(ProductName.Invalid, result.Error);
    }

    [Fact]
    public void ProductNameCreate_Should_ReturnInvalid_WhenValueIsTooLong()
    {
        Result<ProductName> result = ProductName.Create(new string('a', ProductName.MaxLength + 1));

        Assert.True(result.IsFailure);
        Assert.Equal(ProductName.Invalid, result.Error);
    }

    [Fact]
    public void CommentCreate_Should_ReturnInvalid_WhenValueIsEmpty()
    {
        Result<Comment> result = Comment.Create("");

        Assert.True(result.IsFailure);
        Assert.Equal(Comment.Invalid, result.Error);
    }

    [Fact]
    public void EmailCreate_Should_ReturnEmail_WhenValueIsValid()
    {
        Result<Email> result = Email.Create("jane@example.com");

        Assert.True(result.IsSuccess);
        Assert.Equal("jane@example.com", result.Value.Value);
    }

    [Theory]
    [InlineData("jane")]
    [InlineData("@example.com")]
    [InlineData("jane@")]
    [InlineData("jane@@example.com")]
    public void EmailCreate_Should_ReturnInvalid_WhenValueIsNotAnAddress(string value)
    {
        Result<Email> result = Email.Create(value);

        Assert.True(result.IsFailure);
        Assert.Equal(Email.Invalid, result.Error);
    }

    [Fact]
    public void CurrencyFromCode_Should_ReturnCurrency_WhenCodeIsSupported()
    {
        Result<Currency> result = Currency.FromCode("USD");

        Assert.True(result.IsSuccess);
        Assert.Equal(Currency.Usd, result.Value);
    }

    [Fact]
    public void CurrencyFromCode_Should_ReturnUnsupported_WhenCodeIsUnknown()
    {
        Result<Currency> result = Currency.FromCode("EUR");

        Assert.True(result.IsFailure);
        Assert.Equal(Currency.Unsupported, result.Error);
    }

    [Fact]
    public void MoneyAdd_Should_ReturnSum_WhenCurrenciesMatch()
    {
        Result<Money> result = new Money(10m, Currency.Usd).Add(new Money(5m, Currency.Usd));

        Assert.True(result.IsSuccess);
        Assert.Equal(new Money(15m, Currency.Usd), result.Value);
    }

    [Fact]
    public void MoneyAdd_Should_ReturnCurrencyMismatch_WhenCurrenciesDiffer()
    {
        Result<Money> result = new Money(10m, Currency.Usd).Add(new Money(5m, Currency.Bdt));

        Assert.True(result.IsFailure);
        Assert.Equal(Money.CurrencyMismatch, result.Error);
    }
}
