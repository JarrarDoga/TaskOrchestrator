namespace TaskOrchestrator.Shared.Contracts;

public record TeamMemberDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string Role,
    DateTime JoinedAt
);

public record TeamDto(
    int Id,
    string Name,
    string? Description,
    string Slug,
    string Icon,
    bool IsPublic,
    int MemberCount,
    DateTime CreatedAt,
    string CreatedByUserId,
    IReadOnlyList<TeamMemberDto> Members
);

public record CreateTeamRequest(
    string Name,
    string? Description,
    string? Icon,
    bool IsPublic,
    IReadOnlyList<string>? MemberUserIds
);

public record UserSearchDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Email
);
