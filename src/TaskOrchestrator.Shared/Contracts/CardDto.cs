using System.Text.Json;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Shared.Contracts;

// Relational fields: everything you'd filter, sort, or join on.
// Metadata: labels, custom fields, per-board UI settings — no schema migration needed.
public record CardDto(
    int Id,
    int BoardId,
    int ColumnId,
    string Title,
    string? Description,
    int Position,
    int Version,
    TaskPriority Priority,
    string? AssignedToUserId,
    IReadOnlyList<AttachmentDto> Attachments,
    IReadOnlyDictionary<string, JsonElement> Metadata,
    DateTime UpdatedAtUtc,
    string? UpdatedByUserId
);

public record CreateCardRequest(
    int BoardId,
    int ColumnId,
    string Title,
    string? Description,
    TaskPriority Priority
);

public record UpdateCardRequest(
    string Title,
    string? Description,
    TaskPriority Priority,
    string? AssignedToUserId,
    IReadOnlyDictionary<string, JsonElement>? Metadata,
    int Version
);

public record MoveCardRequest(
    int TargetColumnId,
    int TargetPosition,
    int Version
);
