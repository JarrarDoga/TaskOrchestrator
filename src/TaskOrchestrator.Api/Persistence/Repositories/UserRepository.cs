using Dapper;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public sealed class UserRepository(IDbConnectionFactory db) : IUserRepository
{
    public async Task UpsertAsync(string id, string? displayName, string? email, string? avatarUrl)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO Users (Id, DisplayName, Email, AvatarUrl, LastSeenAt)
            VALUES (@Id, @DisplayName, @Email, @AvatarUrl, UTC_TIMESTAMP(3))
            ON DUPLICATE KEY UPDATE
                DisplayName = VALUES(DisplayName),
                Email       = VALUES(Email),
                AvatarUrl   = VALUES(AvatarUrl),
                LastSeenAt  = UTC_TIMESTAMP(3)
            """,
            new { Id = id, DisplayName = displayName ?? id, email, avatarUrl });
    }
}
