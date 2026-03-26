namespace TaskOrchestrator.Api.Persistence.Repositories;

public interface ITeamInviteRepository
{
    Task<string> CreateAsync(int teamId, string inviteeEmail, string createdByUserId, DateTime expiresAt);
    Task<TeamInviteRow?> GetByTokenAsync(string token);
    Task<bool> AcceptAsync(string token);
}

public sealed record TeamInviteRow(
    string Token,
    int TeamId,
    string InviteeEmail,
    string CreatedByUserId,
    DateTime ExpiresAt,
    DateTime? AcceptedAt
);
