using Ecommerce.Application.Categories.DeleteCategory;
using Ecommerce.Application.Coupons.DeleteCoupon;
using Ecommerce.Application.Orders.CancelOrder;
using Ecommerce.Application.Products.DeleteProduct;
using FluentValidation.Results;

namespace Ecommerce.Application.UnitTests;

// The validators whose only rule is that the ids are not empty.
public class IdValidatorTests
{
    [Fact]
    public void DeleteCategory_Should_ReturnError_WhenIdIsEmpty()
    {
        ValidationResult result = new DeleteCategoryCommandValidator().Validate(new DeleteCategoryCommand(Guid.Empty));

        ValidationAssert.HasSingleError(result, nameof(DeleteCategoryCommand.Id));
    }

    [Fact]
    public void DeleteCategory_Should_ReturnNoErrors_WhenIdIsSet()
    {
        ValidationResult result = new DeleteCategoryCommandValidator().Validate(new DeleteCategoryCommand(Guid.CreateVersion7()));

        Assert.True(result.IsValid);
    }

    [Fact]
    public void DeleteProduct_Should_ReturnError_WhenIdIsEmpty()
    {
        ValidationResult result = new DeleteProductCommandValidator().Validate(new DeleteProductCommand(Guid.Empty));

        ValidationAssert.HasSingleError(result, nameof(DeleteProductCommand.Id));
    }

    [Fact]
    public void DeleteCoupon_Should_ReturnError_WhenIdIsEmpty()
    {
        ValidationResult result = new DeleteCouponCommandValidator().Validate(new DeleteCouponCommand(Guid.Empty));

        ValidationAssert.HasSingleError(result, nameof(DeleteCouponCommand.Id));
    }

    [Fact]
    public void CancelOrder_Should_ReturnError_WhenOrderIdIsEmpty()
    {
        ValidationResult result = new CancelOrderCommandValidator().Validate(new CancelOrderCommand(Guid.CreateVersion7(), Guid.Empty));

        ValidationAssert.HasSingleError(result, nameof(CancelOrderCommand.OrderId));
    }

    [Fact]
    public void CancelOrder_Should_ReturnError_WhenUserIdIsEmpty()
    {
        ValidationResult result = new CancelOrderCommandValidator().Validate(new CancelOrderCommand(Guid.Empty, Guid.CreateVersion7()));

        ValidationAssert.HasSingleError(result, nameof(CancelOrderCommand.UserId));
    }
}
