using Dapper;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public sealed class UserRepository(IDbConnectionFactory db) : IUserRepository
{
    public async Task UpsertAsync(string id, string? displayName, string? email, string? avatarUrl)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO Users (Id, DisplayName, Email, AvatarUrl, LastSeenAt)
            VALUES (@Id, @DisplayName, @Email, @AvatarUrl, NOW())
            ON CONFLICT (Id) DO UPDATE SET
                DisplayName = COALESCE(EXCLUDED.DisplayName, Users.DisplayName),
                Email       = COALESCE(EXCLUDED.Email, Users.Email),
                AvatarUrl   = COALESCE(EXCLUDED.AvatarUrl, Users.AvatarUrl),
                LastSeenAt  = NOW()
            """,
            new { Id = id, DisplayName = displayName ?? id, email, avatarUrl });
    }

    public async Task<IEnumerable<UserSearchDto>> SearchAsync(string query, int limit = 10)
    {
        using var conn = db.CreateConnection();
        var like = $"%{query}%";
        return await conn.QueryAsync<UserSearchDto>(
            """
            SELECT Id AS UserId, DisplayName, AvatarUrl, Email
            FROM Users
            WHERE DisplayName LIKE @Like OR Email LIKE @Like
            ORDER BY DisplayName
            LIMIT @Limit
            """,
            new { Like = like, Limit = limit });
    }

    public async Task<bool> ExistsAsync(string userId)
    {
        using var conn = db.CreateConnection();
        var count = await conn.ExecuteScalarAsync<int>(
            "SELECT COUNT(1) FROM Users WHERE Id = @UserId",
            new { UserId = userId });
        return count > 0;
    }

    public async Task<string?> GetEmailAsync(string userId)
    {
        using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT Email FROM Users WHERE Id = @UserId",
            new { UserId = userId });
    }
}
