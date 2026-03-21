using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface ITeamRepository
{
    Task<IEnumerable<TeamDto>> GetAllAsync(string userId);
    Task<TeamDto?> GetByIdAsync(int teamId);
    Task<IEnumerable<TeamBoardDto>> GetBoardsAsync(int teamId, string userId);
    Task<TeamDto> CreateAsync(CreateTeamRequest request, string ownerUserId);
    Task<bool> DeleteAsync(int teamId);
    Task<bool> IsOwnerAsync(int teamId, string userId);
    Task<bool> IsMemberAsync(int teamId, string userId);
    Task AddMemberAsync(int teamId, string userId);
    Task RemoveMemberAsync(int teamId, string userId);
}
