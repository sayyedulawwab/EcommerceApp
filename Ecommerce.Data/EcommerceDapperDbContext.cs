using Microsoft.Data.SqlClient;
using System.Data;

namespace Ecommerce.Data
{
    public class EcommerceDapperDbContext
    {
        private readonly string _connectionString;
        public EcommerceDapperDbContext(string connectionString)
        {
            _connectionString = connectionString;
        }
        public IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }
    }
}
