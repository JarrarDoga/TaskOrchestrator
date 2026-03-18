namespace TaskOrchestrator.Shared.Contracts;

public record UserPresenceDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    int? ActiveCardId,
    DateTime LastSeenAtUtc
);

public record BoardPresenceSnapshot(IReadOnlyList<UserPresenceDto> Users);
