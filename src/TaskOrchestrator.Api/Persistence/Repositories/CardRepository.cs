using System.Text.Json;
using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Persistence;

public sealed class CardRepository(IDbConnectionFactory db) : ICardRepository
{
    public async Task<CardDto?> GetByIdAsync(int cardId)
    {
        using var conn = db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync<CardRow>(
            "SELECT Id, BoardId, ColumnId, Title, Description, Position, Version, Priority, AssignedToUserId, Metadata, UpdatedAtUtc, UpdatedByUserId FROM Cards WHERE Id = @Id",
            new { Id = cardId });

        if (row is null) return null;

        var attachments = await LoadAttachments(conn, cardId);
        return row.ToDto(attachments);
    }

    public async Task<IEnumerable<CardDto>> GetByBoardAsync(int boardId)
    {
        using var conn = db.CreateConnection();

        var rows = (await conn.QueryAsync<CardRow>(
            "SELECT Id, BoardId, ColumnId, Title, Description, Position, Version, Priority, AssignedToUserId, Metadata, UpdatedAtUtc, UpdatedByUserId FROM Cards WHERE BoardId = @BoardId ORDER BY ColumnId, Position",
            new { BoardId = boardId })).ToList();

        var attachmentsByCard = (await conn.QueryAsync<AttachmentDto>(
            """
            SELECT a.Id, a.CardId, a.FileName, a.ContentType, a.FileSizeBytes,
                   CONCAT('/api/attachments/', a.Id, '/download') AS Url,
                   a.UploadedAtUtc, a.UploadedByUserId
            FROM Attachments a WHERE a.CardId IN (SELECT Id FROM Cards WHERE BoardId = @BoardId)
            """,
            new { BoardId = boardId }))
            .GroupBy(a => a.CardId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AttachmentDto>)g.ToList());

        return rows.Select(r => r.ToDto(attachmentsByCard.GetValueOrDefault(r.Id, [])));
    }

    public async Task<CardDto> CreateAsync(CreateCardRequest request, string? userId)
    {
        using var conn = db.CreateConnection();

        var position = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(Position), -1) + 1 FROM Cards WHERE ColumnId = @ColumnId",
            new { request.ColumnId });

        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Cards (BoardId, ColumnId, Title, Description, Position, Priority, Metadata, UpdatedAtUtc, UpdatedByUserId)
            VALUES (@BoardId, @ColumnId, @Title, @Description, @Position, @Priority, '{}', UTC_TIMESTAMP(3), @UserId);
            SELECT LAST_INSERT_ID();
            """,
            new { request.BoardId, request.ColumnId, request.Title, request.Description,
                  Position = position, Priority = (int)request.Priority, UserId = userId });

        return (await GetByIdAsync(id))!;
    }

    public async Task<CardDto?> UpdateAsync(int cardId, UpdateCardRequest request, string? userId)
    {
        using var conn = db.CreateConnection();

        var metadataJson = request.Metadata is not null
            ? JsonSerializer.Serialize(request.Metadata)
            : null;

        // Version in WHERE clause enforces optimistic concurrency.
        // If another writer incremented Version first, affected rows = 0.
        var rows = await conn.ExecuteAsync(
            """
            UPDATE Cards
            SET Title           = @Title,
                Description     = @Description,
                Priority        = @Priority,
                AssignedToUserId = @AssignedToUserId,
                Metadata        = COALESCE(@Metadata, Metadata),
                UpdatedAtUtc    = UTC_TIMESTAMP(3),
                UpdatedByUserId = @UserId,
                Version         = Version + 1
            WHERE Id = @Id AND Version = @Version
            """,
            new { Id = cardId, request.Title, request.Description,
                  Priority = (int)request.Priority, request.AssignedToUserId,
                  Metadata = metadataJson, UserId = userId, request.Version });

        return rows == 0 ? null : await GetByIdAsync(cardId);
    }

    public async Task<CardDto?> MoveAsync(int cardId, MoveCardRequest request, string? userId)
    {
        using var conn = db.CreateConnection();

        // Moving within or across columns. We accept the client's position
        // and let the client manage gap reordering. Future improvement:
        // shift adjacent cards server-side for gap-free ordering.
        var rows = await conn.ExecuteAsync(
            """
            UPDATE Cards
            SET ColumnId        = @TargetColumnId,
                Position        = @TargetPosition,
                UpdatedAtUtc    = UTC_TIMESTAMP(3),
                UpdatedByUserId = @UserId,
                Version         = Version + 1
            WHERE Id = @Id AND Version = @Version
            """,
            new { Id = cardId, request.TargetColumnId, request.TargetPosition,
                  UserId = userId, request.Version });

        return rows == 0 ? null : await GetByIdAsync(cardId);
    }

    public async Task<bool> DeleteAsync(int cardId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Cards WHERE Id = @Id", new { Id = cardId });
        return rows > 0;
    }

    private static async Task<IReadOnlyList<AttachmentDto>> LoadAttachments(
        System.Data.IDbConnection conn, int cardId)
    {
        var results = await conn.QueryAsync<AttachmentDto>(
            """
            SELECT Id, CardId, FileName, ContentType, FileSizeBytes,
                   CONCAT('/api/attachments/', Id, '/download') AS Url,
                   UploadedAtUtc, UploadedByUserId
            FROM Attachments WHERE CardId = @CardId
            """,
            new { CardId = cardId });
        return results.ToList();
    }
}
