using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Domain.Coupons;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests.Orders;

public class PlaceOrderCommandValidatorTests
{
    private static readonly PlaceOrderCommandValidator Validator = new();

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCommandIsValid()
    {
        ValidationResult result = Validator.Validate(CreateCommand());

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnNoErrors_WhenCouponCodeIsSet()
    {
        ValidationResult result = Validator.Validate(CreateCommand() with { CouponCode = "SAVE10" });

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenUserIdIsEmpty()
    {
        ValidationResult result = Validator.Validate(CreateCommand() with { UserId = Guid.Empty });

        ValidationAssert.HasSingleError(result, nameof(PlaceOrderCommand.UserId));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenThereAreNoItems()
    {
        ValidationResult result = Validator.Validate(CreateCommand() with { OrderStockItems = [] });

        ValidationAssert.HasSingleError(result, nameof(PlaceOrderCommand.OrderStockItems));
    }

    [Fact]
    public void Validate_Should_ReturnError_WhenCouponCodeIsLongerThanMaxLength()
    {
        ValidationResult result = Validator.Validate(CreateCommand() with { CouponCode = new string('A', CouponCode.MaxLength + 1) });

        ValidationAssert.HasSingleError(result, nameof(PlaceOrderCommand.CouponCode));
    }

    [Fact]
    public void Validate_Should_ReturnErrorForTheItem_WhenAnItemHasNoProductId()
    {
        PlaceOrderCommand command = CreateCommand() with
        {
            OrderStockItems = [new OrderStockItem(Guid.CreateVersion7(), 1), new OrderStockItem(Guid.Empty, 1)],
        };

        ValidationResult result = Validator.Validate(command);

        ValidationAssert.HasSingleError(result, "OrderStockItems[1].ProductId");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_Should_ReturnErrorForTheItem_WhenAnItemQuantityIsNotPositive(int quantity)
    {
        PlaceOrderCommand command = CreateCommand() with { OrderStockItems = [new OrderStockItem(Guid.CreateVersion7(), quantity)] };

        ValidationResult result = Validator.Validate(command);

        ValidationAssert.HasSingleError(result, "OrderStockItems[0].Quantity");
    }

    // A new list each time, so no test can change another test's command.
    private static PlaceOrderCommand CreateCommand()
    {
        return new PlaceOrderCommand(Guid.CreateVersion7(), [new OrderStockItem(Guid.CreateVersion7(), 2)]);
    }
}
