using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IUserRepository
{
    Task UpsertAsync(string id, string? displayName, string? email, string? avatarUrl);
    Task<IEnumerable<UserSearchDto>> SearchAsync(string query, int limit = 10);
}
