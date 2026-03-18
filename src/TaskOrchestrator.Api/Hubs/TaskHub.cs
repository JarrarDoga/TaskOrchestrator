using Microsoft.AspNetCore.SignalR;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Hubs;

// Keeps business logic out of the hub. Clients join board groups and receive
// strongly typed payloads. Method name constants prevent stringly-typed bugs.
public sealed class TaskHub : Hub
{
    public const string CardCreated       = nameof(CardCreated);
    public const string CardUpdated       = nameof(CardUpdated);
    public const string CardDeleted       = nameof(CardDeleted);
    public const string AttachmentAdded   = nameof(AttachmentAdded);
    public const string ActivityAppended  = nameof(ActivityAppended);
    public const string PresenceChanged   = nameof(PresenceChanged);
    public const string MemberKicked      = nameof(MemberKicked);
    public const string ColumnCreated     = nameof(ColumnCreated);
    public const string ColumnDeleted     = nameof(ColumnDeleted);
    public const string ColumnUpdated     = nameof(ColumnUpdated);

    // Presence: board-id → connection-id → UserPresenceDto
    // In-memory only; reset on app restart. Fine for a single-node deployment.
    private static readonly Dictionary<int, Dictionary<string, UserPresenceDto>> BoardPresence = [];
    private static readonly object PresenceLock = new();

    public async Task JoinBoard(int boardId, string userId, string displayName, string? avatarUrl)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, GroupName(boardId));
        UpdatePresence(boardId, new UserPresenceDto(userId, displayName, avatarUrl, null, DateTime.UtcNow));
        await Clients.Group(GroupName(boardId)).SendAsync(PresenceChanged, GetSnapshot(boardId));
    }

    public async Task LeaveBoard(int boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, GroupName(boardId));
        RemovePresence(boardId, Context.ConnectionId);

        await Clients.Group(GroupName(boardId))
            .SendAsync(PresenceChanged, GetSnapshot(boardId));
    }

    public async Task UpdateActiveCard(int boardId, string userId, string displayName, int? cardId)
    {
        string? avatarUrl = null;
        lock (PresenceLock)
        {
            if (BoardPresence.TryGetValue(boardId, out var board) && board.TryGetValue(userId, out var existing))
                avatarUrl = existing.AvatarUrl;
        }
        UpdatePresence(boardId, new UserPresenceDto(userId, displayName, avatarUrl, cardId, DateTime.UtcNow));
        await Clients.Group(GroupName(boardId)).SendAsync(PresenceChanged, GetSnapshot(boardId));
    }

    public static string GroupName(int boardId) => $"board:{boardId}";

    private static void UpdatePresence(int boardId, UserPresenceDto presence)
    {
        lock (PresenceLock)
        {
            if (!BoardPresence.TryGetValue(boardId, out var board))
                BoardPresence[boardId] = board = [];
            board[presence.UserId] = presence;
        }
    }

    private static void RemovePresence(int boardId, string connectionId)
    {
        lock (PresenceLock)
        {
            if (!BoardPresence.TryGetValue(boardId, out var board)) return;
            var key = board.Keys.FirstOrDefault();
            if (key is not null) board.Remove(key);
        }
    }

    private static BoardPresenceSnapshot GetSnapshot(int boardId)
    {
        lock (PresenceLock)
        {
            if (!BoardPresence.TryGetValue(boardId, out var board))
                return new BoardPresenceSnapshot([]);
            return new BoardPresenceSnapshot(board.Values.ToList());
        }
    }
}
