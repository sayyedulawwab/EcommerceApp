using System.Net;
using System.Net.Http.Json;
using Ecommerce.Application.Orders.SearchOrders;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Domain.Shared;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Orders;

public sealed class SearchOrdersTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public SearchOrdersTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SearchOrders_ShouldReturnOnlyTheUsersOwnOrdersWithThePricePaid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User owner = UserData.Create();
        User otherUser = UserData.Create(email: "other@example.com");
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(owner, otherUser, category, keyboard);

        using HttpClient ownerClient = _factory.CreateClientForUser(owner.Id.Value);
        using HttpClient otherClient = _factory.CreateClientForUser(otherUser.Id.Value);

        await OrderData.PlaceAsync(ownerClient, keyboard, 1);
        await OrderData.PlaceAsync(otherClient, keyboard, 1);

        // The price changes after the orders were placed.
        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            Product product = await db.Set<Product>().FirstAsync(p => p.Id == keyboard.Id);
            Product.Update(product, product.Name, product.Description, new Money(150m, product.Price.Currency), product.Quantity, product.CategoryId, DateTime.UtcNow);
            await db.SaveChangesAsync();
        });

        // Act
        PagedResponse<OrderResponse>? page = await ownerClient.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/v1/orders");

        // Assert
        OrderResponse order = Assert.Single(page!.Items);
        Assert.Equal(owner.Id.Value, order.UserId);
        Assert.Equal(100m, Assert.Single(order.OrderItems).PriceAmount);
    }

    [Fact]
    public async Task SearchOrders_ShouldReturnTheRequestedPageSortedByTotalPrice()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        await OrderData.PlaceAsync(client, keyboard, 2);
        await OrderData.PlaceAsync(client, keyboard, 1);
        await OrderData.PlaceAsync(client, keyboard, 3);

        // Act
        PagedResponse<OrderResponse>? page = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>(
            "/api/v1/orders?sortColumn=TotalPrice&sortOrder=asc&page=1&pageSize=2");

        // Assert
        Assert.Equal([100m, 200m], page!.Items.Select(order => order.TotalPriceAmount));
        Assert.Equal(3, page.TotalCount);
        Assert.True(page.HasNextPage);
    }

    [Fact]
    public async Task SearchOrders_ShouldFilterByStatus()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        User user = UserData.Create();
        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await SeedAsync(user, category, keyboard);

        using HttpClient client = _factory.CreateClientForUser(user.Id.Value);

        Guid cancelledOrderId = await OrderData.PlaceAsync(client, keyboard, 1);
        await OrderData.PlaceAsync(client, keyboard, 1);
        await client.PostAsync($"/api/v1/orders/{cancelledOrderId}/cancel", null);

        // Act
        PagedResponse<OrderResponse>? page = await client.GetFromJsonAsync<PagedResponse<OrderResponse>>("/api/v1/orders?status=cancelled");

        // Assert
        OrderResponse order = Assert.Single(page!.Items);
        Assert.Equal(cancelledOrderId, order.Id);
        Assert.Equal("Cancelled", order.Status);
    }

    [Theory]
    [InlineData("sortColumn=UserId")]
    [InlineData("status=Shipped")]
    [InlineData("pageSize=101")]
    [InlineData("from=2026-02-01T00:00:00Z&to=2026-01-01T00:00:00Z")]
    public async Task SearchOrders_ShouldReturnBadRequest_WhenTheSearchIsInvalid(string queryString)
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient client = _factory.CreateClientForUser(Guid.NewGuid());

        // Act
        HttpResponseMessage response = await client.GetAsync($"/api/v1/orders?{queryString}");

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private Task SeedAsync(params object[] entities)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();

            db.AddRange(entities);

            await db.SaveChangesAsync();
        });
    }
}
