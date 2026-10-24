using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class RegisterTests
    : IClassFixture<IntegrationTestFactory>
{
    private readonly IntegrationTestFactory _factory;

    public RegisterTests(IntegrationTestFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task Register_ShouldCreateUser_WhenEmailIsUnique()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(1, await CountUsersAsync());
    }

    [Fact]
    public async Task Register_ShouldSendWelcomeEmail_WhenUserIsCreated()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        SentEmail email = Assert.Single(_factory.Emails.Sent);
        Assert.Equal("jane@example.com", email.To);
        Assert.Equal("Welcome to Ecommerce", email.Subject);
    }

    [Fact]
    public async Task Register_ShouldReturnConflict_WhenEmailIsNotUnique()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountUsersAsync());
    }

    private Task<int> CountUsersAsync()
    {
        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<User>().CountAsync());
    }
}
