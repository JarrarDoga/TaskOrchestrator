using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface IAttachmentRepository
{
    Task<AttachmentDto?> GetByIdAsync(int id);
    Task<string?> GetStoragePathAsync(int id);
    Task<IEnumerable<AttachmentDto>> GetByCardAsync(int cardId);
    Task<AttachmentDto> CreateAsync(RegisterAttachmentRequest request, string? userId);
}
