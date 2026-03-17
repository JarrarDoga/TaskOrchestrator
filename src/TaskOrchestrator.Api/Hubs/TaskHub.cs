using Microsoft.AspNetCore.SignalR;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Hubs;

/// <summary>
/// Real-time hub. Clients join a board group to receive scoped updates.
/// Server-to-client method names are defined as constants to avoid stringly-typed bugs.
/// </summary>
public sealed class TaskHub : Hub
{
    public const string TaskCreated   = nameof(TaskCreated);
    public const string TaskUpdated   = nameof(TaskUpdated);
    public const string TaskDeleted   = nameof(TaskDeleted);
    public const string BoardUpdated  = nameof(BoardUpdated);

    public async Task JoinBoard(int boardId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, BoardGroup(boardId));
    }

    public async Task LeaveBoard(int boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, BoardGroup(boardId));
    }

    public static string BoardGroup(int boardId) => $"board-{boardId}";
}
