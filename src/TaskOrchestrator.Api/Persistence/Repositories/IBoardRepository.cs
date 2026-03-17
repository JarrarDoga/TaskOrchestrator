using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IBoardRepository
{
    Task<IEnumerable<BoardDto>> GetAllAsync();
    Task<BoardDetailDto?> GetDetailAsync(int boardId);
    Task<BoardDto> CreateAsync(CreateBoardRequest request);
    Task<bool> DeleteAsync(int boardId);
}
