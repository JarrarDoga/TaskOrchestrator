using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IActivityRepository
{
    Task<IEnumerable<ActivityEventDto>> GetByCardAsync(int cardId, int limit = 50);
    Task<IEnumerable<ActivityEventDto>> GetByBoardAsync(int boardId, int limit = 100);
    Task AppendAsync(int cardId, int boardId, ActivityEventType type, string? userId, string? displayName, string description);
}
