using Ecommerce.Domain.Users;
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

        dbContext.Database.Migrate();

        // Seed once. A restart keeps the data, and the fixed accounts would break the unique email index.
        if (!dbContext.Set<User>().Any())
        {
            DataSeeder.SeedData(dbContext);
        }
    }
}
