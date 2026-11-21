using Ecommerce.Domain.Users;
using Ecommerce.Infrastructure;
using Ecommerce.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.API.Extensions;

internal static class DevelopmentDatabaseExtensions
{
    /// <summary>
    /// Development only: brings the local database to the latest migration and fills an empty one with sample data.
    /// Other environments migrate in the deploy pipeline, never at startup.
    /// </summary>
    public static async Task PrepareDevelopmentDatabaseAsync(this WebApplication app)
    {
        await using AsyncServiceScope scope = app.Services.CreateAsyncScope();
        ApplicationDbContext dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await dbContext.Database.MigrateAsync();

        // Seed only an empty database: the seeded accounts have fixed emails, and the unique email index would reject them twice.
        bool hasUsers = await dbContext.Set<User>().AnyAsync();
        if (!hasUsers)
        {
            DataSeeder.SeedData(dbContext);
        }
    }
}
