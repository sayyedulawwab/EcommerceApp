using System.Data.Common;
using Ecommerce.Application.Abstractions.Emails;
using Ecommerce.Infrastructure;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Npgsql;
using Testcontainers.PostgreSql;

namespace Ecommerce.IntegrationTests.Infrastructure;

public sealed class IntegrationTestFactory
    : WebApplicationFactory<Program>,
      IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres;

    private DatabaseReset _databaseReset = default!;

    private DbConnection _connection = default!;

    public HttpClient Client { get; private set; } = default!;

    public FakeEmailService Emails { get; } = new();

    public string ConnectionString => _postgres.GetConnectionString();

    public IntegrationTestFactory()
    {
        _postgres = new PostgreSqlBuilder("postgres:18.6")
            .WithDatabase("ecommerce")
            .WithUsername("postgres")
            .WithPassword("postgres")
            .Build();
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        Client = CreateClient();

        await using AsyncServiceScope scope = Services.CreateAsyncScope();

        ApplicationDbContext db = scope.ServiceProvider
            .GetRequiredService<ApplicationDbContext>();

        await db.Database.MigrateAsync();

        _connection = new NpgsqlConnection(ConnectionString);

        await _connection.OpenAsync();

        _databaseReset = await DatabaseReset.CreateAsync(_connection);
    }

    public async Task ResetDatabaseAsync()
    {
        Emails.Clear();

        await _databaseReset.ResetAsync(_connection);
    }

    public async Task ExecuteScopeAsync(
        Func<IServiceProvider, Task> action)
    {
        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        await action(scope.ServiceProvider);
    }

    public async Task<TResult> ExecuteScopeAsync<TResult>(
        Func<IServiceProvider, Task<TResult>> action)
    {
        await using AsyncServiceScope scope =
            Services.CreateAsyncScope();

        return await action(scope.ServiceProvider);
    }

    public override async ValueTask DisposeAsync()
    {
        if (_connection is not null)
        {
            await _connection.DisposeAsync();
        }

        await _postgres.DisposeAsync();

        await base.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");

        builder.ConfigureAppConfiguration((_, config) => config.AddInMemoryCollection(
                new Dictionary<string, string?>
                {
                    ["ConnectionStrings:Database"] = ConnectionString
                }));

        builder.ConfigureServices(services =>
        {
            services.RemoveAll<DbContextOptions<ApplicationDbContext>>();

            services.AddDbContext<ApplicationDbContext>(options => options.UseNpgsql(ConnectionString)
                       .UseSnakeCaseNamingConvention());

            services.RemoveAll<NpgsqlDataSource>();

            services.AddSingleton(_ => NpgsqlDataSource.Create(ConnectionString));

            services.RemoveAll<IEmailService>();
            services.AddSingleton<IEmailService>(Emails);
        });
    }
}
