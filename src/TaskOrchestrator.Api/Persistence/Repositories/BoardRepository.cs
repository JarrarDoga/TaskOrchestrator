using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence;

public sealed class BoardRepository(IDbConnectionFactory db) : IBoardRepository
{
    public async Task<IEnumerable<BoardDto>> GetByUserAsync(string userId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<BoardDto>(
            """
            SELECT b.Id, b.Name, b.Description,
                   b.TeamId,
                   t.Name AS TeamName,
                   b.CreatedAt, b.Version
            FROM Boards b
            LEFT JOIN Teams t ON t.Id = b.TeamId
            INNER JOIN BoardMembers bm ON bm.BoardId = b.Id
            WHERE bm.UserId = @UserId
            ORDER BY bm.JoinedAt DESC
            """,
            new { UserId = userId });
    }

    public async Task<IEnumerable<BoardDto>> GetByTeamAsync(int teamId, string userId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<BoardDto>(
            """
            SELECT b.Id, b.Name, b.Description,
                   b.TeamId,
                   t.Name AS TeamName,
                   b.CreatedAt, b.Version
            FROM Boards b
            INNER JOIN Teams t ON t.Id = b.TeamId
            INNER JOIN BoardMembers bm ON bm.BoardId = b.Id
            WHERE b.TeamId = @TeamId AND bm.UserId = @UserId
            ORDER BY b.CreatedAt DESC
            """,
            new { TeamId = teamId, UserId = userId });
    }

    public async Task<BoardDetailDto?> GetDetailAsync(int boardId)
    {
        using var conn = db.CreateConnection();

        var board = await conn.QuerySingleOrDefaultAsync<BoardDto>(
            """
            SELECT b.Id, b.Name, b.Description,
                   b.TeamId,
                   t.Name AS TeamName,
                   b.CreatedAt, b.Version
            FROM Boards b
            LEFT JOIN Teams t ON t.Id = b.TeamId
            WHERE b.Id = @Id
            """,
            new { Id = boardId });

        if (board is null) return null;

        var columns = (await conn.QueryAsync<ColumnDto>(
            "SELECT Id, BoardId, Title, Color, Position FROM Columns WHERE BoardId = @BoardId ORDER BY Position",
            new { BoardId = boardId })).ToList();

        // One query for all cards, group in memory — avoids N+1
        var cardRows = (await conn.QueryAsync<CardRow>(
            """
            SELECT c.Id, c.BoardId, c.ColumnId, c.Title, c.Description,
                   c.Position, c.Version, c.Priority, c.AssignedToUserId,
                   c.Metadata, c.UpdatedAtUtc, c.UpdatedByUserId
            FROM Cards c
            WHERE c.BoardId = @BoardId
            ORDER BY c.ColumnId, c.Position
            """,
            new { BoardId = boardId })).ToList();

        var attachmentsByCard = (await conn.QueryAsync<AttachmentDto>(
            """
            SELECT a.Id, a.CardId, a.FileName, a.ContentType, a.FileSizeBytes,
                   CONCAT('/api/attachments/', a.Id, '/download') AS Url,
                   a.UploadedAtUtc, a.UploadedByUserId
            FROM Attachments a
            INNER JOIN Cards c ON c.Id = a.CardId
            WHERE c.BoardId = @BoardId
            """,
            new { BoardId = boardId }))
            .GroupBy(a => a.CardId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<AttachmentDto>)g.ToList());

        var cardsByColumn = cardRows
            .Select(r => r.ToDto(attachmentsByCard.GetValueOrDefault(r.Id, [])))
            .GroupBy(c => c.ColumnId)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<CardDto>)g.ToList());

        var columnsWithCards = columns
            .Select(col => new ColumnWithCardsDto(
                col.Id, col.BoardId, col.Title, col.Color, col.Position,
                cardsByColumn.GetValueOrDefault(col.Id, [])))
            .ToList();

        return new BoardDetailDto(board.Id, board.Name, board.Description,
            board.TeamId, board.TeamName, board.CreatedAt, board.Version, columnsWithCards);
    }

    public async Task<BoardDto> CreateAsync(CreateBoardRequest request, string ownerUserId)
    {
        using var conn = db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Boards (Name, Description, TeamId) VALUES (@Name, @Description, @TeamId)
            RETURNING Id
            """,
            new { request.Name, request.Description, request.TeamId });

        // Guard against a missed /api/me/sync: boardmembers has FK → users, so the user must exist first.
        await conn.ExecuteAsync(
            "INSERT INTO Users (Id, DisplayName) VALUES (@Id, @Id) ON CONFLICT (Id) DO NOTHING",
            new { Id = ownerUserId });

        await conn.ExecuteAsync(
            "INSERT INTO BoardMembers (BoardId, UserId, Role) VALUES (@BoardId, @UserId, 'Owner')",
            new { BoardId = id, UserId = ownerUserId });

        if (request.TeamId.HasValue)
        {
            await conn.ExecuteAsync(
                """
                INSERT INTO BoardMembers (BoardId, UserId, Role)
                SELECT @BoardId, tm.UserId, 'Member'
                FROM TeamMembers tm
                WHERE tm.TeamId = @TeamId
                  AND tm.UserId <> @OwnerUserId
                ON CONFLICT DO NOTHING
                """,
                new { BoardId = id, TeamId = request.TeamId.Value, OwnerUserId = ownerUserId });
        }

        return await conn.QuerySingleAsync<BoardDto>(
            """
            SELECT b.Id, b.Name, b.Description,
                   b.TeamId,
                   t.Name AS TeamName,
                   b.CreatedAt, b.Version
            FROM Boards b
            LEFT JOIN Teams t ON t.Id = b.TeamId
            WHERE b.Id = @Id
            """,
            new { Id = id });
    }

    public async Task<bool> DeleteAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Boards WHERE Id = @Id", new { Id = boardId });
        return rows > 0;
    }
}
