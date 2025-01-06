using Ecommerce.Infrastructure.Data;
using Ecommerce.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace Ecommerce.API.Extensions;

public static class MigrationExtensions
{
    public static void ApplyMigrations(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();

        using ApplicationDbContext dbContext =
            scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        // I have manually removed the migration files.
        // So I needed this to delete the previous test database.
        dbContext.Database.EnsureDeleted();

        dbContext.Database.Migrate();
        DataSeeder.SeedData(dbContext);
    }
}
