using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IColumnRepository
{
    Task<IEnumerable<ColumnDto>> GetByBoardAsync(int boardId);
    Task<ColumnDto> CreateAsync(CreateColumnRequest request);
    Task<bool> DeleteAsync(int columnId);
}
