using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IBoardMemberRepository
{
    Task<bool> IsMemberAsync(int boardId, string userId);
    Task<string?> GetRoleAsync(int boardId, string userId);
    Task<IEnumerable<BoardMemberDto>> GetMembersAsync(int boardId);
    Task AddMemberAsync(int boardId, string userId, string role = "Member");
    Task RemoveMemberAsync(int boardId, string userId);
    Task SetRoleAsync(int boardId, string userId, string role);
}
