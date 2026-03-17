using System.Data;

namespace TaskOrchestrator.Api.Persistence;

public interface IDbConnectionFactory
{
    IDbConnection CreateConnection();
}
