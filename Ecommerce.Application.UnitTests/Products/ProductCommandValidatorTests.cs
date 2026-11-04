using Ecommerce.Application.Products.AddProduct;
using Ecommerce.Application.Products.EditProduct;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Products;

public class ProductCommandValidatorTests
{
    private static readonly AddProductCommandValidator AddValidator = new();
    private static readonly EditProductCommandValidator EditValidator = new();
    private static readonly AddProductCommand ValidAddCommand = new("Keyboard", "Mechanical keyboard", "USD", 49.99m, 10, Guid.CreateVersion7());
    private static readonly EditProductCommand ValidEditCommand = new(Guid.CreateVersion7(), "Keyboard", "Mechanical keyboard", "USD", 49.99m, 10, Guid.CreateVersion7());

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandsAreValid()
    {
        Assert.True(AddValidator.Validate(ValidAddCommand).IsValid);
        Assert.True(EditValidator.Validate(ValidEditCommand).IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenQuantityIsZero()
    {
        Assert.True(AddValidator.Validate(ValidAddCommand with { Quantity = 0 }).IsValid);
        Assert.True(EditValidator.Validate(ValidEditCommand with { Quantity = 0 }).IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenNameIsEmpty()
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { Name = "" }), nameof(AddProductCommand.Name));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { Name = "" }), nameof(EditProductCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenNameIsLongerThan200Characters()
    {
        string name = new('a', 201);

        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { Name = name }), nameof(AddProductCommand.Name));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { Name = name }), nameof(EditProductCommand.Name));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenDescriptionIsLongerThan2000Characters()
    {
        string description = new('a', 2001);

        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { Description = description }), nameof(AddProductCommand.Description));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { Description = description }), nameof(EditProductCommand.Description));
    }

    [Theory]
    [InlineData("EUR")]
    [InlineData("usd")]
    [InlineData("")]
    public void Validate_Should_ReturnError_WhenCurrencyIsNotSupported(string currency)
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { PriceCurrency = currency }), nameof(AddProductCommand.PriceCurrency));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { PriceCurrency = currency }), nameof(EditProductCommand.PriceCurrency));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_ReturnError_WhenPriceIsNotPositive(decimal amount)
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { PriceAmount = amount }), nameof(AddProductCommand.PriceAmount));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { PriceAmount = amount }), nameof(EditProductCommand.PriceAmount));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenQuantityIsNegative()
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { Quantity = -1 }), nameof(AddProductCommand.Quantity));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { Quantity = -1 }), nameof(EditProductCommand.Quantity));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCategoryIdIsEmpty()
    {
        ValidationAssert.HasSingleError(AddValidator.Validate(ValidAddCommand with { CategoryId = Guid.Empty }), nameof(AddProductCommand.CategoryId));
        ValidationAssert.HasSingleError(EditValidator.Validate(ValidEditCommand with { CategoryId = Guid.Empty }), nameof(EditProductCommand.CategoryId));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenEditIdIsEmpty()
    {
        ValidationResult result = EditValidator.Validate(ValidEditCommand with { Id = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(EditProductCommand.Id));
    }
}
