using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IBoardRepository
{
    Task<IEnumerable<BoardDto>> GetByUserAsync(string userId);
    Task<BoardDetailDto?> GetDetailAsync(int boardId);
    Task<BoardDto> CreateAsync(CreateBoardRequest request, string ownerUserId);
    Task<bool> DeleteAsync(int boardId);
}
