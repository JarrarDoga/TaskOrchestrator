namespace TaskOrchestrator.Shared.Contracts;

public record BoardMemberDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt,
    DateTime LastSeenAt
);

public record BoardInviteDto(
    int Id,
    string Code,
    DateTime CreatedAt,
    DateTime? ExpiresAt,
    int? MaxUses,
    int TimesUsed
);

public record GenerateInviteRequest(DateTime? ExpiresAt, int? MaxUses);

public record TransferOwnershipRequest(string NewOwnerUserId);
