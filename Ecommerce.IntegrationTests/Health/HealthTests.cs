using System.Net;
using Ecommerce.IntegrationTests.Infrastructure;

namespace Ecommerce.IntegrationTests.Health;

public sealed class HealthTests
{
    private readonly IntegrationTestFactory _factory;

    public HealthTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetHealth_ShouldReturnHealthyStatusOnly_WhenDatabaseAndRedisAreUp()
    {
        // Arrange
        using HttpClient anonymousClient = _factory.CreateClient();

        // Act
        HttpResponseMessage response = await anonymousClient.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Healthy", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }
}
