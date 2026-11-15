using System.Diagnostics;
using System.Net;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;

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

    [Fact]
    public async Task GetHealth_ShouldReturnDegradedWith200_WhenRedisIsDown()
    {
        // Arrange
        // Starting the extra host seeds development data when it finds no users. Start it on an empty
        // database (leftovers from other tests can collide with the seed), then reset to remove the seed.
        await _factory.ResetDatabaseAsync();

        await using WebApplicationFactory<Program> factoryWithoutRedis = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.UseRedisThatIsDown()));

        using HttpClient anonymousClient = factoryWithoutRedis.CreateClient();

        await _factory.ResetDatabaseAsync();

        // Act
        HttpResponseMessage response = await anonymousClient.GetAsync("/health", TestContext.Current.CancellationToken);

        // Assert: 200 keeps the container behind Traefik, which serves from the database meanwhile.
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Degraded", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetHealth_ShouldAnswerDegradedWithinTheProbeTimeout_WhenRedisNeverAnswers()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await using WebApplicationFactory<Program> factoryWithSilentRedis = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureServices(services => services.UseRedisThatNeverAnswers()));

        using HttpClient anonymousClient = factoryWithSilentRedis.CreateClient();

        await _factory.ResetDatabaseAsync();

        // Act
        var stopwatch = Stopwatch.StartNew();

        HttpResponseMessage response = await anonymousClient.GetAsync("/health", TestContext.Current.CancellationToken);

        stopwatch.Stop();

        // Assert: the compose healthcheck gives up after 5 s and would mark the container unhealthy.
        Assert.Equal("Degraded", await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(5), $"Took {stopwatch.Elapsed}");
    }
}
