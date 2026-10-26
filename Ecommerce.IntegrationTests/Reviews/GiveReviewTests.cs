using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Reviews;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Reviews;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Reviews;

public sealed class GiveReviewTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public GiveReviewTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GiveReview_ShouldCreateReview_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        (User user, Product product) = await SeedUserAndProductAsync();

        var request = new GiveReviewRequest { ProductId = product.Id.Value, Rating = 5, Comment = "Great keyboard" };

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/reviews", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Review? review = await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            return await db.Set<Review>().FirstOrDefaultAsync(r => r.ProductId == product.Id);
        });

        Assert.NotNull(review);
        Assert.Equal(5, review.Rating.Value);
        Assert.Equal(user.Id, review.UserId);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    public async Task GiveReview_ShouldReturnBadRequest_WhenRatingIsOutOfRange(int rating)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        (User user, Product product) = await SeedUserAndProductAsync();

        var request = new GiveReviewRequest { ProductId = product.Id.Value, Rating = rating, Comment = "Comment" };

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/api/v1/reviews", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GiveReview_ShouldReturnNotFound_WhenProductDoesNotExist()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new GiveReviewRequest { ProductId = Guid.NewGuid(), Rating = 4, Comment = "Comment" };

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/reviews", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    private async Task<(User User, Product Product)> SeedUserAndProductAsync()
    {
        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product product = ProductData.CreateProduct(category.Id);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            db.AddRange(user, category, product);

            await db.SaveChangesAsync();
        });

        return (user, product);
    }
}
