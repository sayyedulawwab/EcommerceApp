using Ecommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.API.Extensions;

internal static class DevelopmentDatabaseExtensions
{
    /// <summary>
    /// Development only: brings the local database to the latest migration.
    /// Other environments migrate in the deploy pipeline, never at startup.
    /// </summary>
    public static async Task PrepareDevelopmentDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();
    }
}
