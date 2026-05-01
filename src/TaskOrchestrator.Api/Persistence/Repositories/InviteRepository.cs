using System.Security.Cryptography;
using System.Text;
using Dapper;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence.Repositories;

public sealed class InviteRepository(IDbConnectionFactory db) : IInviteRepository
{
    public async Task<BoardInviteDto?> GetActiveAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<BoardInviteDto>(
            """
            SELECT Id, Code, CreatedAt, ExpiresAt, MaxUses, TimesUsed
            FROM BoardInvites
            WHERE BoardId = @BoardId AND IsActive = TRUE
              AND (ExpiresAt IS NULL OR ExpiresAt > NOW())
            LIMIT 1
            """,
            new { BoardId = boardId });
    }

    public async Task<(int BoardId, BoardInviteDto Invite)?> ResolveCodeAsync(string code)
    {
        using var conn = db.CreateConnection();
        var row = await conn.QuerySingleOrDefaultAsync(
            """
            SELECT Id, BoardId, Code, CreatedAt, ExpiresAt, MaxUses, TimesUsed
            FROM BoardInvites
            WHERE Code = @Code AND IsActive = TRUE
              AND (ExpiresAt IS NULL OR ExpiresAt > NOW())
              AND (MaxUses IS NULL OR TimesUsed < MaxUses)
            """,
            new { Code = code });

        if (row is null) return null;

        var dto = new BoardInviteDto((int)row.Id, (string)row.Code,
            (DateTime)row.CreatedAt, (DateTime?)row.ExpiresAt,
            (int?)row.MaxUses, (int)row.TimesUsed);

        return ((int)row.BoardId, dto);
    }

    public async Task<BoardInviteDto> GenerateAsync(int boardId, string createdByUserId,
        DateTime? expiresAt, int? maxUses)
    {
        using var conn = db.CreateConnection();

        // Deactivate any existing code for this board
        await conn.ExecuteAsync(
            "UPDATE BoardInvites SET IsActive = FALSE WHERE BoardId = @BoardId",
            new { BoardId = boardId });

        var code = NewCode();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO BoardInvites (BoardId, Code, CreatedByUserId, ExpiresAt, MaxUses)
            VALUES (@BoardId, @Code, @CreatedBy, @ExpiresAt, @MaxUses)
            RETURNING Id
            """,
            new { BoardId = boardId, Code = code, CreatedBy = createdByUserId, expiresAt, maxUses });

        return new BoardInviteDto(id, code, DateTime.UtcNow, expiresAt, maxUses, 0);
    }

    public async Task DeactivateAsync(int boardId)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE BoardInvites SET IsActive = FALSE WHERE BoardId = @BoardId",
            new { BoardId = boardId });
    }

    public async Task IncrementUsesAsync(string code)
    {
        using var conn = db.CreateConnection();
        await conn.ExecuteAsync(
            "UPDATE BoardInvites SET TimesUsed = TimesUsed + 1 WHERE Code = @Code",
            new { Code = code });
    }

    // Format: XXXX-XXXX using unambiguous characters
    private static string NewCode()
    {
        const string chars = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";
        var bytes = RandomNumberGenerator.GetBytes(8);
        var sb = new StringBuilder(9);
        for (int i = 0; i < 8; i++)
        {
            if (i == 4) sb.Append('-');
            sb.Append(chars[bytes[i] % chars.Length]);
        }
        return sb.ToString();
    }
}
