using Dapper;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public sealed class BoardMemberRepository(IDbConnectionFactory db) : IBoardMemberRepository
{
    public async Task<bool> IsMemberAsync(int boardId, string userId)
    {
        using var conn = db.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM BoardMembers WHERE BoardId = @BoardId AND UserId = @UserId",
            new { BoardId = boardId, UserId = userId });
        return count > 0;
    }

    public async Task<string?> GetRoleAsync(int boardId, string userId)
    {
        using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT Role FROM BoardMembers WHERE BoardId = @BoardId AND UserId = @UserId",
            new { BoardId = boardId, UserId = userId });
    }

    public async Task<IEnumerable<BoardMemberDto>> GetMembersAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<BoardMemberDto>(
            """
            SELECT bm.UserId, u.DisplayName, u.AvatarUrl, bm.Role, bm.JoinedAt, u.LastSeenAt
            FROM BoardMembers bm
            INNER JOIN Users u ON u.Id = bm.UserId
            WHERE bm.BoardId = @BoardId
            ORDER BY bm.Role DESC, bm.JoinedAt ASC
            """,
            new { BoardId = boardId });
    }

    public async Task AddMemberAsync(int boardId, string userId, string role = "Member")
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT IGNORE INTO BoardMembers (BoardId, UserId, Role)
            VALUES (@BoardId, @UserId, @Role)
            """,
            new { BoardId = boardId, UserId = userId, Role = role });
    }

    public async Task RemoveMemberAsync(int boardId, string userId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "DELETE FROM BoardMembers WHERE BoardId = @BoardId AND UserId = @UserId",
            new { BoardId = boardId, UserId = userId });
    }

    public async Task SetRoleAsync(int boardId, string userId, string role)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE BoardMembers SET Role = @Role WHERE BoardId = @BoardId AND UserId = @UserId",
            new { BoardId = boardId, UserId = userId, Role = role });
    }
}
