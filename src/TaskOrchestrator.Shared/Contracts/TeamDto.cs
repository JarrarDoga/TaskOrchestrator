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
    int MemberCount,
    int BoardCount,
    DateTime CreatedAt,
    string CreatedByUserId,
    IReadOnlyList<TeamMemberDto> Members
);

public record CreateTeamRequest(
    string Name,
    string? Description,
    string? Icon,
    IReadOnlyList<string>? MemberUserIds
);

public record UserSearchDto(
    string UserId,
    string DisplayName,
    string? AvatarUrl,
    string? Email
);

public record TeamBoardDto(
    int Id,
    string Name,
    string? Description,
    DateTime CreatedAt,
    int Version
);

public record TeamCreateBoardRequest(
    string Name,
    string? Description
);

public record TeamInviteInfoDto(
    string TeamName,
    string InviteeEmail,   // full email — frontend shows it so the user knows which account to sign in with
    bool IsExpired,
    bool IsUsed
);
