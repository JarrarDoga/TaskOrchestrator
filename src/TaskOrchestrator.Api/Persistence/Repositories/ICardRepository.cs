using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface ICardRepository
{
    Task<CardDto?> GetByIdAsync(int cardId);
    Task<IEnumerable<CardDto>> GetByBoardAsync(int boardId);
    Task<CardDto> CreateAsync(CreateCardRequest request, string? userId);
    // Returns null on version conflict
    Task<CardDto?> UpdateAsync(int cardId, UpdateCardRequest request, string? userId);
    Task<CardDto?> MoveAsync(int cardId, MoveCardRequest request, string? userId);
    Task<bool> DeleteAsync(int cardId);
}
