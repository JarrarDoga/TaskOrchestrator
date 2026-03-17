using Dapper;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Persistence;

public sealed class AttachmentRepository(IDbConnectionFactory db) : IAttachmentRepository
{
    private const string SelectSql =
        """
        SELECT Id, CardId, FileName, ContentType, FileSizeBytes,
               CONCAT('/api/attachments/', Id, '/download') AS Url,
               UploadedAtUtc, UploadedByUserId
        FROM Attachments
        """;

    public async Task<AttachmentDto?> GetByIdAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.QuerySingleOrDefaultAsync<AttachmentDto>(
            SelectSql + " WHERE Id = @Id", new { Id = id });
    }

    public async Task<string?> GetStoragePathAsync(int id)
    {
        using var conn = db.CreateConnection();
        return await conn.ExecuteScalarAsync<string?>(
            "SELECT StoragePath FROM Attachments WHERE Id = @Id", new { Id = id });
    }

    public async Task<IEnumerable<AttachmentDto>> GetByCardAsync(int cardId)
    {
        using var conn = db.CreateConnection();
        return await conn.QueryAsync<AttachmentDto>(
            SelectSql + " WHERE CardId = @CardId", new { CardId = cardId });
    }

    public async Task<AttachmentDto> CreateAsync(RegisterAttachmentRequest request, string? userId)
    {
        using var conn = db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Attachments (CardId, FileName, ContentType, FileSizeBytes, StoragePath, UploadedByUserId)
            VALUES (@CardId, @FileName, @ContentType, @FileSizeBytes, @StoragePath, @UserId);
            SELECT LAST_INSERT_ID();
            """,
            new { request.CardId, request.FileName, request.ContentType,
                  request.FileSizeBytes, request.StoragePath, UserId = userId });

        return await conn.QuerySingleAsync<AttachmentDto>(
            SelectSql + " WHERE Id = @Id", new { Id = id });
    }
}
