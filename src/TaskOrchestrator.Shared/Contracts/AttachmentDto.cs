namespace TaskOrchestrator.Shared.Contracts;

public record AttachmentDto(
    int Id,
    int CardId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string Url,
    DateTime UploadedAtUtc,
    string? UploadedByUserId
);

public record RegisterAttachmentRequest(
    int CardId,
    string FileName,
    string ContentType,
    long FileSizeBytes,
    string StoragePath
);

public record UploadAttachmentResponse(AttachmentDto Attachment);
