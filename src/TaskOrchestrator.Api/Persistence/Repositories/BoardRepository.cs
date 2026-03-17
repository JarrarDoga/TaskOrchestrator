using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence;

public sealed class BoardRepository(IDbConnectionFactory db) : IBoardRepository
{
    public async Task<IEnumerable<BoardDto>> GetAllAsync()
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards ORDER BY CreatedAt DESC");
    }

    public async Task<BoardDetailDto?> GetDetailAsync(int boardId)
    {
        using var conn = db.CreateConnection();

        var board = await conn.QuerySingleOrDefaultAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards WHERE Id = @Id",
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
            board.CreatedAt, board.Version, columnsWithCards);
    }

    public async Task<BoardDto> CreateAsync(CreateBoardRequest request)
    {
        using var conn = db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Boards (Name, Description) VALUES (@Name, @Description);
            SELECT LAST_INSERT_ID();
            """,
            new { request.Name, request.Description });

        return await conn.QuerySingleAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<bool> DeleteAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Boards WHERE Id = @Id", new { Id = boardId });
        return rows > 0;
    }
}
