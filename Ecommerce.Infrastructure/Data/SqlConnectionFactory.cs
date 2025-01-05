using System.Data;
using Ecommerce.Application.Abstractions.Data;
using Npgsql;

namespace Ecommerce.Infrastructure.Data;

internal sealed class SqlConnectionFactory(string connectionString) : ISqlConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var connection = new NpgsqlConnection(connectionString);
        connection.Open();

        return connection;
    }
}
