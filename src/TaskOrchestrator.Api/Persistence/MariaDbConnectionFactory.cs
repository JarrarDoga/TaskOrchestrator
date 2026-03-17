using System.Data;
using MySqlConnector;

namespace TaskOrchestrator.Api.Persistence;

public sealed class MariaDbConnectionFactory(string connectionString) : IDbConnectionFactory
{
    public IDbConnection CreateConnection() => new MySqlConnection(connectionString);
}
