namespace TaskOrchestrator.Shared.Contracts;

public record UserPresenceDto(
    string UserId,
    string DisplayName,
    int? ActiveCardId,
    DateTime LastSeenAtUtc
);

public record BoardPresenceSnapshot(IReadOnlyList<UserPresenceDto> Users);
