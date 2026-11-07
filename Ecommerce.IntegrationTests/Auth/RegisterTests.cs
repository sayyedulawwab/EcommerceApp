using System.Net;
using System.Net.Http.Json;
using Ecommerce.API.Controllers.Auth;
using Ecommerce.Application.Exceptions;
using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.IntegrationTests.Helpers;
using Ecommerce.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Ecommerce.IntegrationTests.Auth;

public sealed class RegisterTests
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
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

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
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

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

        await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal(1, await CountUsersAsync());
    }

    [Fact]
    public async Task Register_ShouldCreateOneUserAndReturnConflictForTheRest_WhenTheSameEmailIsRegisteredAtTheSameTime()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        // Act
        HttpResponseMessage[] responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ =>
            _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken)));

        // Assert
        Assert.Single(responses, response => response.StatusCode == HttpStatusCode.OK);
        Assert.Equal(4, responses.Count(response => response.StatusCode == HttpStatusCode.Conflict));
        Assert.Equal(1, await CountUsersAsync());
    }

    [Fact]
    public async Task SaveChanges_ShouldThrowUniqueConstraintException_WhenTheEmailIsAlreadyStored()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        await _factory.ExecuteScopeAsync(async sp =>
        {
            ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
            db.Add(UserData.Create("jane@example.com"));
            await db.SaveChangesAsync();
        });

        // Act
        UniqueConstraintException exception = await Assert.ThrowsAsync<UniqueConstraintException>(() =>
            _factory.ExecuteScopeAsync(async sp =>
            {
                ApplicationDbContext db = sp.GetRequiredService<ApplicationDbContext>();
                db.Add(UserData.Create("jane@example.com"));
                await db.SaveChangesAsync();
            }));

        // Assert
        Assert.Equal("ix_users_email", exception.ConstraintName);
    }

    [Fact]
    public async Task Register_ShouldStoreABcryptHash_WhenUserIsCreated()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "Password123!");

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        string hash = await _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<User>().Select(user => user.PasswordHash).SingleAsync());

        Assert.StartsWith("$2a$10$", hash, StringComparison.Ordinal);
        Assert.NotEqual(request.Password, hash);
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenThePasswordIsLongerThan72Bytes()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", new string('a', 73));

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Equal(0, await CountUsersAsync());
    }

    [Fact]
    public async Task Register_ShouldReturnBadRequest_WhenThePasswordIsTooShort()
    {
        // Arrange
        await _factory.ResetDatabaseAsync();

        var request = new RegisterUserRequest("Jane", "Doe", "jane@example.com", "short");

        // Act
        HttpResponseMessage response = await _factory.Client.PostAsJsonAsync("/api/v1/auth/register", request, cancellationToken: TestContext.Current.CancellationToken);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

        ProblemDetails? problem = await response.Content.ReadFromJsonAsync<ProblemDetails>(cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotNull(problem);
        Assert.Equal("Validation.General", problem.Extensions["code"]?.ToString());
        Assert.Equal(0, await CountUsersAsync());
    }

    private Task<int> CountUsersAsync()
    {
        return _factory.ExecuteScopeAsync(sp =>
            sp.GetRequiredService<ApplicationDbContext>().Set<User>().CountAsync());
    }
}
