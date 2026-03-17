using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Shared.Contracts;

public record ActivityEventDto(
    int Id,
    int CardId,
    int BoardId,
    ActivityEventType EventType,
    string? UserId,
    string? UserDisplayName,
    string Description,
    DateTime OccurredAtUtc
);
