using Respawn;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Text;

namespace Ecommerce.IntegrationTests.Infrastructure;

internal sealed class DatabaseReset
{
    private readonly Respawner _respawner;

    private DatabaseReset(Respawner respawner)
    {
        _respawner = respawner;
    }

    public static async Task<DatabaseReset> CreateAsync(DbConnection connection)
    {
        Respawner respawner = await Respawner.CreateAsync(
            connection,
            new RespawnerOptions
            {
                DbAdapter = DbAdapter.Postgres,

                TablesToIgnore =
                [
                    "__EFMigrationsHistory"
                ]
            });

        return new DatabaseReset(respawner);
    }

    public Task ResetAsync(DbConnection connection)
    {
        return _respawner.ResetAsync(connection);
    }
}
