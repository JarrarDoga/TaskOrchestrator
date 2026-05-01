using Dapper;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public sealed class TeamInviteRepository(IDbConnectionFactory db) : ITeamInviteRepository
{
    public async Task<string> CreateAsync(int teamId, string inviteeEmail, string createdByUserId, DateTime expiresAt)
    {
        var token = Guid.NewGuid().ToString("N"); // 32-char hex, no dashes
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            """
            INSERT INTO TeamInvites (Token, TeamId, InviteeEmail, CreatedByUserId, ExpiresAt)
            VALUES (@Token, @TeamId, @InviteeEmail, @CreatedByUserId, @ExpiresAt)
            """,
            new { Token = token, TeamId = teamId, InviteeEmail = inviteeEmail, CreatedByUserId = createdByUserId, ExpiresAt = expiresAt });
        return token;
    }

    public async Task<TeamInviteRow?> GetByTokenAsync(string token)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<TeamInviteRow>(
            """
            SELECT Token, TeamId, InviteeEmail, CreatedByUserId, ExpiresAt, AcceptedAt
            FROM TeamInvites
            WHERE Token = @Token
            """,
            new { Token = token });
    }

    public async Task<bool> AcceptAsync(string token)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "UPDATE TeamInvites SET AcceptedAt = NOW() WHERE Token = @Token AND AcceptedAt IS NULL",
            new { Token = token });
        return rows > 0;
    }
}
