using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Hubs;

// Injected into endpoints so they can trigger hub broadcasts without
// taking a direct dependency on IHubContext everywhere.
public interface IBoardNotifier
{
    Task CardCreatedAsync(int boardId, CardDto card);
    Task CardUpdatedAsync(int boardId, CardDto card);
    Task CardDeletedAsync(int boardId, int cardId);
    Task AttachmentAddedAsync(int boardId, int cardId, AttachmentDto attachment);
    Task ActivityAppendedAsync(int boardId, ActivityEventDto activity);
    Task PresenceChangedAsync(int boardId, BoardPresenceSnapshot snapshot);
    Task MemberKickedAsync(int boardId, string kickedUserId);
    Task ColumnCreatedAsync(int boardId, ColumnDto column);
    Task ColumnDeletedAsync(int boardId, int columnId);
}
