using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Shared.Contracts;

public record TaskItemDto(
    int Id,
    int BoardId,
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedToUserId,
    int Position,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    int Version
);

public record CreateTaskItemRequest(
    int BoardId,
    string Title,
    string? Description,
    TaskPriority Priority
);

public record UpdateTaskItemRequest(
    string Title,
    string? Description,
    TaskItemStatus Status,
    TaskPriority Priority,
    string? AssignedToUserId,
    int Position,
    int Version
);

public record MoveTaskItemRequest(
    TaskItemStatus NewStatus,
    int NewPosition,
    int Version
);
