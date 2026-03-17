using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IInviteRepository
{
    Task<BoardInviteDto?> GetActiveAsync(int boardId);
    // Returns null if the code doesn't exist or is expired/exhausted
    Task<(int BoardId, BoardInviteDto Invite)?> ResolveCodeAsync(string code);
    Task<BoardInviteDto> GenerateAsync(int boardId, string createdByUserId, DateTime? expiresAt, int? maxUses);
    Task DeactivateAsync(int boardId);
    Task IncrementUsesAsync(string code);
}
