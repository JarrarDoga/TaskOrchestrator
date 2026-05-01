using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Persistence;

public sealed class ActivityRepository(IDbConnectionFactory db) : IActivityRepository
{
    public async Task<IEnumerable<ActivityEventDto>> GetByCardAsync(int cardId, int limit = 50)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.QueryAsync<ActivityRow>(
            """
            SELECT Id, CardId, BoardId, EventType, UserId, UserDisplayName, Description, OccurredAtUtc
            FROM CardActivity WHERE CardId = @CardId ORDER BY OccurredAtUtc DESC LIMIT @Limit
            """,
            new { CardId = cardId, Limit = limit });

        return rows.Select(r => r.ToDto());
    }

    public async Task<IEnumerable<ActivityEventDto>> GetByBoardAsync(int boardId, int limit = 100)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.QueryAsync<ActivityRow>(
            """
            SELECT Id, CardId, BoardId, EventType, UserId, UserDisplayName, Description, OccurredAtUtc
            FROM CardActivity WHERE BoardId = @BoardId ORDER BY OccurredAtUtc DESC LIMIT @Limit
            """,
            new { BoardId = boardId, Limit = limit });

        return rows.Select(r => r.ToDto());
    }

    public async Task AppendAsync(int cardId, int boardId, ActivityEventType type,
        string? userId, string? displayName, string description)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO CardActivity (CardId, BoardId, EventType, UserId, UserDisplayName, Description)
            VALUES (@CardId, @BoardId, @EventType, @UserId, @DisplayName, @Description)
            """,
            new { CardId = cardId, BoardId = boardId, EventType = (int)type,
                  UserId = userId, DisplayName = displayName, Description = description });
    }

    // PostgreSQL SMALLINT projects as short; convert explicitly to enum-backed DTO.
    private sealed record ActivityRow(
        int Id,
        int CardId,
        int BoardId,
        short EventType,
        string? UserId,
        string? UserDisplayName,
        string Description,
        DateTime OccurredAtUtc)
    {
        public ActivityEventDto ToDto() =>
            new(Id, CardId, BoardId, (ActivityEventType)(int)EventType, UserId, UserDisplayName, Description, OccurredAtUtc);
    }
}
