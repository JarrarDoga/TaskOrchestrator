using Microsoft.AspNetCore.SignalR;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Hubs;

public sealed class BoardNotifier(IHubContext<TaskHub> hub) : IBoardNotifier
{
    private IClientProxy Group(int boardId) => hub.Clients.Group(TaskHub.GroupName(boardId));

    public Task CardCreatedAsync(int boardId, CardDto card) =>
        Group(boardId).SendAsync(TaskHub.CardCreated, card);

    public Task CardUpdatedAsync(int boardId, CardDto card) =>
        Group(boardId).SendAsync(TaskHub.CardUpdated, card);

    public Task CardDeletedAsync(int boardId, int cardId) =>
        Group(boardId).SendAsync(TaskHub.CardDeleted, new { boardId, cardId });

    public Task AttachmentAddedAsync(int boardId, int cardId, AttachmentDto attachment) =>
        Group(boardId).SendAsync(TaskHub.AttachmentAdded, new { boardId, cardId, attachment });

    public Task ActivityAppendedAsync(int boardId, ActivityEventDto activity) =>
        Group(boardId).SendAsync(TaskHub.ActivityAppended, activity);

    public Task PresenceChangedAsync(int boardId, BoardPresenceSnapshot snapshot) =>
        Group(boardId).SendAsync(TaskHub.PresenceChanged, snapshot);

    public Task MemberKickedAsync(int boardId, string kickedUserId) =>
        Group(boardId).SendAsync(TaskHub.MemberKicked, new { boardId, kickedUserId });

    public Task ColumnCreatedAsync(int boardId, ColumnDto column) =>
        Group(boardId).SendAsync(TaskHub.ColumnCreated, column);

    public Task ColumnDeletedAsync(int boardId, int columnId) =>
        Group(boardId).SendAsync(TaskHub.ColumnDeleted, new { boardId, columnId });

    public Task ColumnUpdatedAsync(int boardId, ColumnDto column) =>
        Group(boardId).SendAsync(TaskHub.ColumnUpdated, column);
}
