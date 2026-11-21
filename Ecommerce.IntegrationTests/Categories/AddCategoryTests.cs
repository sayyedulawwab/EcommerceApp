using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Categories;
using Ecommerce.Domain.Categories;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Categories;

public sealed class AddCategoryTests
{
    private static readonly AddCategoryRequest Request = new("Electronics", "ELEC");

    private readonly IntegrationTestFactory _factory;

    public AddCategoryTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddCategory_ShouldReturnConflictWithTheErrorCode_WhenTheNameAlreadyExists()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await SeedAsync(CategoryData.Create("Electronics", "ELEC"));

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/categories", Request with { Code = "OTHER" }, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal(CategoryErrors.NameAlreadyExists.Name, problem.Extensions["code"]?.ToString());
    }

    [Fact]
    public async Task AddCategory_ShouldCreateOnlyOneCategory_WhenRequestsWithTheSameNameArriveAtTheSameTime()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        // Act: the requests can all pass the duplicate check before any of them saves. The unique index decides.
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            _factory.Client.PostAsJsonAsync("/api/v1/categories", Request, cancellationToken: TestContext.Current.CancellationToken)));

        // Assert
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.Created);
        Assert.Equal(4, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));

        int count = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<Category>().CountAsync());

        Assert.Equal(1, count);
    }

    private Task SeedAsync(Category category)
    {
        return _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(category);
            await db.SaveChangesAsync();
        });
    }
}
