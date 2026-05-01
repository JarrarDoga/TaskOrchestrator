using System.Data;
using Npgsql;

namespace TaskOrchestrator.Api.Persistence;

public sealed class PostgresConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new NpgsqlConnection(connectionString);
}
