using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class JwtStartupTests
{
    private readonly IntegrationTestFactory _factory;

    public JwtStartupTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Theory]
    [InlineData("")]
    [InlineData("too-short")]
    [InlineData("0123456789012345678901234567890")] // 31 bytes
    public void Startup_ShouldFail_WhenTheJwtSecretKeyIsMissingOrShorterThan32Bytes(string secretKey)
    {
        // Arrange
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:SecretKey"] = secretKey })));

        // Act
        OptionsValidationException exception = Assert.Throws<OptionsValidationException>(() => factory.CreateClient());

        // Assert
        Assert.Contains("Jwt:SecretKey", exception.Message, StringComparison.Ordinal);

        if (secretKey.Length > 0)
        {
            Assert.DoesNotContain(secretKey, exception.Message, StringComparison.Ordinal);
        }
    }

    [Fact]
    public void Startup_ShouldSucceed_WhenTheJwtSecretKeyIs32Bytes()
    {
        // Arrange
        using WebApplicationFactory<Program> factory = _factory.WithWebHostBuilder(builder =>
            builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["Jwt:SecretKey"] = "01234567890123456789012345678901" })));

        // Act
        using HttpClient client = factory.CreateClient();

        // Assert
        Assert.NotNull(client);
    }
}
