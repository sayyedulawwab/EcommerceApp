using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Orders.PlaceOrder;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Orders;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Moq;

namespace Ecommerce.Application.UnitTests.Orders;
public class PlaceOrderCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IOrderRepository> _orderRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public PlaceOrderCommandHandlerTests()
    {
        _productRepositoryMock = new();
        _orderRepositoryMock = new();
        _unitOfWorkMock = new();
        _dateTimeProviderMock = new();

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task Handle_Should_AddOrderWithTotalOfAllLines_WhenProductsExist()
    {
        // Arrange
        Product keyboard = MakeProduct(100m, Currency.Usd);
        Product mouse = MakeProduct(25m, Currency.Usd);

        SetupProducts(keyboard, mouse);

        Order? captured = null;
        _orderRepositoryMock.Setup(x => x.Add(It.IsAny<Order>())).Callback<Order>(o => captured = o);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new OrderStockItem(keyboard.Id.Value, 2), new OrderStockItem(mouse.Id.Value, 3)]);

        PlaceOrderCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(captured);
        Assert.Equal(275m, captured.TotalPrice.Amount);
        Assert.Equal(Currency.Usd, captured.TotalPrice.Currency);
        Assert.Equal(2, captured.OrderItems.Count);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        _productRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new OrderStockItem(Guid.NewGuid(), 1)]);

        PlaceOrderCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.NotFound, result.Error);
        _orderRepositoryMock.Verify(x => x.Add(It.IsAny<Order>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnCurrencyMismatch_WhenProductsHaveDifferentCurrencies()
    {
        // Arrange
        Product usdProduct = MakeProduct(10m, Currency.Usd);
        Product bdtProduct = MakeProduct(10m, Currency.Bdt);

        SetupProducts(usdProduct, bdtProduct);

        var command = new PlaceOrderCommand(
            Guid.NewGuid(),
            [new OrderStockItem(usdProduct.Id.Value, 1), new OrderStockItem(bdtProduct.Id.Value, 1)]);

        PlaceOrderCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.CurrencyMismatch, result.Error);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_Should_ReturnInvalidQuantity_WhenQuantityIsNotPositive()
    {
        // Arrange
        Product product = MakeProduct(10m, Currency.Usd);

        SetupProducts(product);

        var command = new PlaceOrderCommand(Guid.NewGuid(), [new OrderStockItem(product.Id.Value, 0)]);

        PlaceOrderCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(command, default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(OrderErrors.InvalidQuantity, result.Error);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private void SetupProducts(params Product[] products)
    {
        foreach (Product product in products)
        {
            _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
                .ReturnsAsync(product);
        }
    }

    private PlaceOrderCommandHandler CreateHandler()
    {
        return new PlaceOrderCommandHandler(
            _productRepositoryMock.Object,
            _orderRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object);
    }

    private static Product MakeProduct(decimal priceAmount, Currency currency)
    {
        return Product.Create(
            new ProductName("Product"),
            new ProductDescription("Description"),
            new Money(priceAmount, currency),
            10,
            CategoryId.New(),
            UtcNow);
    }
}
