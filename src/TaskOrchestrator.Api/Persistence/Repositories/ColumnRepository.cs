using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence;

public sealed class ColumnRepository(IDbConnectionFactory db) : IColumnRepository
{
    public async Task<IEnumerable<ColumnDto>> GetByBoardAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<ColumnDto>(
            "SELECT Id, BoardId, Title, Color, Position FROM Columns WHERE BoardId = @BoardId ORDER BY Position",
            new { BoardId = boardId });
    }

    public async Task<ColumnDto> CreateAsync(CreateColumnRequest request)
    {
        using var conn = db.CreateConnection();
        var nextPos = await conn.ExecuteScalarAsync<int>(
            "SELECT COALESCE(MAX(Position) + 1, 0) FROM Columns WHERE BoardId = @BoardId",
            new { request.BoardId });

        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Columns (BoardId, Title, Color, Position)
            VALUES (@BoardId, @Title, @Color, @Position);
            SELECT LAST_INSERT_ID();
            """,
            new { request.BoardId, request.Title, request.Color, Position = nextPos });

        return await conn.QuerySingleAsync<ColumnDto>(
            "SELECT Id, BoardId, Title, Color, Position FROM Columns WHERE Id = @Id",
            new { Id = id });
    }

    public async Task<bool> DeleteAsync(int columnId)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync("DELETE FROM Columns WHERE Id = @Id", new { Id = columnId });
        return rows > 0;
    }

    public async Task<ColumnDto?> UpdateAsync(int columnId, string title, string color, int position)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE Columns SET Title = @Title, Color = @Color, Position = @Position WHERE Id = @Id",
            new { Id = columnId, Title = title, Color = color, Position = position });
        if (rows == 0) return null;
        return await conn.QuerySingleOrDefaultAsync<ColumnDto>(
            "SELECT Id, BoardId, Title, Color, Position FROM Columns WHERE Id = @Id",
            new { Id = columnId });
    }
}
