using Npgsql;
using System.Data;

namespace Luminae.API.Data;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}

public class NpgsqlConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection()
    {
        var conn = new NpgsqlConnection(connectionString);
        conn.Open();
        return conn;
    }
}