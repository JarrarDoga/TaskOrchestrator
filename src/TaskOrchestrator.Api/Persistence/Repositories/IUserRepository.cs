namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IUserRepository
{
    Task UpsertAsync(string id, string? displayName, string? email, string? avatarUrl);
}
