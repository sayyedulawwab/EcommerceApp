using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Categories;
using Ecommerce.Domain.Products;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Products;

public sealed class ProductConcurrencyTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public ProductConcurrencyTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowConcurrencyException_WhenTheProductChangedAfterItWasLoaded()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        Category category = CategoryData.Create();
        Product keyboard = ProductData.CreateProduct(category.Id, 100m);

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.AddRange(category, keyboard);
            await db.SaveChangesAsync();
        });

        // Two requests read the same stock, like two orders placed at the same time.
        using IServiceScope first = _factory.CreateScope();
        using IServiceScope second = _factory.CreateScope();
        ApplicationDbContext firstDb = first.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        ApplicationDbContext secondDb = second.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        Product firstCopy = await firstDb.Set<Product>().FirstAsync(p => p.Id == keyboard.Id, cancellationToken: TestContext.Current.CancellationToken);
        Product secondCopy = await secondDb.Set<Product>().FirstAsync(p => p.Id == keyboard.Id, cancellationToken: TestContext.Current.CancellationToken);

        firstCopy.RemoveStock(10, DateTime.UtcNow);
        secondCopy.RemoveStock(10, DateTime.UtcNow);

        await firstDb.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);

        // Act and assert
        await Assert.ThrowsAsync<ConcurrencyException>(() => secondDb.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken));
    }
}
