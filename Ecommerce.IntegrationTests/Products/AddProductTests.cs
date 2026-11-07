using Ecommerce.API.Controllers.Products;
using Ecommerce.Application.Abstractions.Auth;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Json;

namespace Ecommerce.IntegrationTests.Products;

public sealed class AddProductTests
{
    private readonly IntegrationTestFactory _factory;

    public AddProductTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddProduct_ShouldCreateProduct_WhenRequestIsValid()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db =
                sp.GetRequiredService<ApplicationDbContext>();

            db.Add(category);

            await db.SaveChangesAsync();
        });

        AddProductRequest request =
            ProductData.Create(category.Id.Value);

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response =
            await adminClient.PostAsJsonAsync(
                "/api/v1/products",
                request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        Product? product =
            await _factory.ExecuteScopeAsync(async sp =>
            {
                ApplicationDbContext db =
                    sp.GetRequiredService<ApplicationDbContext>();

                ProductName productName = ProductName.Create(request.Name).Value;

                return await db.Set<Product>()
                    .FirstOrDefaultAsync(p =>
                        p.Name == productName);
            });

        Assert.NotNull(product);

        Assert.Equal(request.Name, product.Name.Value);
        Assert.Equal(request.Description, product.Description.Value);
        Assert.Equal(request.PriceAmount, product.Price.Amount);
        Assert.Equal(request.PriceCurrency, product.Price.Currency.Code);
        Assert.Equal(request.Quantity, product.Quantity);
        Assert.Equal(category.Id, product.CategoryId);
    }

    [Fact]
    public async Task AddProduct_ShouldReturnForbidden_WhenTheUserIsNotAnAdmin()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient customerClient = _factory.CreateClientForUser(Guid.NewGuid());

        // Act
        HttpResponseMessage response = await customerClient.PostAsJsonAsync("/api/v1/products", ProductData.Create(), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddProduct_ShouldReturnBadRequest_WhenTheNameIsEmpty()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        using HttpClient adminClient = _factory.CreateAdminClient();

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/products", ProductData.Create() with { Name = "" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Validation.General", problem.Extensions["code"]?.ToString());

        bool anyProduct = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Product>().AnyAsync());

        Assert.False(anyProduct);
    }

    [Fact]
    public async Task AddProduct_ShouldCreateProductAndLogWarning_WhenTheCacheCannotBeCleared()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(category);
            await db.SaveChangesAsync();
        });

        using var logs = new CapturingLoggerProvider();

        await using WebApplicationFactory<Program> factoryWithBrokenCache = _factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureLogging(logging => logging.AddProvider(logs));
            builder.ConfigureServices(services =>
            {
                services.RemoveAll<HybridCache>();
                services.AddSingleton<HybridCache, FailingHybridCache>();
            });
        });

        using HttpClient adminClient = factoryWithBrokenCache.CreateClient();

        adminClient.DefaultRequestHeaders.Authorization = new("Test");
        adminClient.DefaultRequestHeaders.Add(TestAuthenticationHandler.RoleHeaderName, Roles.Admin);

        // Act
        HttpResponseMessage response = await adminClient.PostAsJsonAsync("/api/v1/products", ProductData.Create(category.Id.Value), cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);

        bool productSaved = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Product>().AnyAsync());

        Assert.True(productSaved);
        Assert.Contains(logs.Entries, entry => entry.Level == LogLevel.Warning && entry.Message.Contains("could not clear", StringComparison.Ordinal));
    }
}
