using System.Text.Json;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Persistence.Repositories;

// Internal Dapper projection. JSON columns come back as strings;
// ToDto() deserialises metadata and attaches pre-loaded attachments.
internal sealed record CardRow(
    int Id,
    int BoardId,
    int ColumnId,
    string Title,
    string? Description,
    int Position,
    int Version,
    int Priority,
    string? AssignedToUserId,
    string? Metadata,
    DateTime UpdatedAtUtc,
    string? UpdatedByUserId
)
{
    public CardDto ToDto(IReadOnlyList<AttachmentDto> attachments)
    {
        var metadata = Metadata is { Length: > 0 }
            ? JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(Metadata)
              ?? new Dictionary<string, JsonElement>()
            : new Dictionary<string, JsonElement>();

        return new CardDto(
            Id, BoardId, ColumnId, Title, Description,
            Position, Version, (TaskPriority)Priority,
            AssignedToUserId, attachments, metadata,
            UpdatedAtUtc, UpdatedByUserId);
    }
}
