using Ecommerce.Application.Abstractions.Clock;
using Ecommerce.Application.Reviews.AddReview;
using Ecommerce.Domain.Abstractions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Shared;
using Moq;

namespace Ecommerce.Application.UnitTests.Reviews;
public class AddReviewCommandHandlerTests
{
    private static readonly DateTime UtcNow = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly Mock<IProductRepository> _productRepositoryMock;
    private readonly Mock<IReviewRepository> _reviewRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;

    public AddReviewCommandHandlerTests()
    {
        _productRepositoryMock = new();
        _reviewRepositoryMock = new();
        _unitOfWorkMock = new();
        _dateTimeProviderMock = new();

        _dateTimeProviderMock.Setup(x => x.UtcNow).Returns(UtcNow);
    }

    [Fact]
    public async Task Handle_Should_ReturnSuccessResult_WhenReviewIsValid()
    {
        // Arrange
        Product product = MakeProduct();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        AddReviewCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(MakeCommand(product.Id.Value, rating: 5), default);

        // Assert
        Assert.True(result.IsSuccess);
        _reviewRepositoryMock.Verify(x => x.Add(It.Is<Review>(r => r.Id.Value == result.Value)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_Should_ReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        _productRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<ProductId>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Product?)null);

        AddReviewCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(MakeCommand(Guid.NewGuid(), rating: 5), default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ProductErrors.NotFound, result.Error);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task Handle_Should_ReturnInvalidRating_WhenRatingIsOutOfRange(int rating)
    {
        // Arrange
        Product product = MakeProduct();

        _productRepositoryMock.Setup(x => x.GetByIdAsync(product.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(product);

        AddReviewCommandHandler handler = CreateHandler();

        // Act
        Result<Guid> result = await handler.Handle(MakeCommand(product.Id.Value, rating), default);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(Rating.Invalid, result.Error);
        _reviewRepositoryMock.Verify(x => x.Add(It.IsAny<Review>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    private AddReviewCommandHandler CreateHandler()
    {
        return new AddReviewCommandHandler(
            _productRepositoryMock.Object,
            _reviewRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _dateTimeProviderMock.Object);
    }

    private static AddReviewCommand MakeCommand(Guid productId, int rating)
    {
        return new AddReviewCommand(productId, Guid.NewGuid(), rating, "Great product");
    }

    private static Product MakeProduct()
    {
        return Product.Create(
            new ProductName("Keyboard"),
            new ProductDescription("Mechanical keyboard"),
            new Money(100m, Currency.Usd),
            10,
            CategoryId.New(),
            UtcNow);
    }
}
