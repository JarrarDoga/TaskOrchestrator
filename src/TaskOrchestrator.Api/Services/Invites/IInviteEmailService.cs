namespace TaskOrchestrator.Api.Services.Invites;

public interface IInviteEmailService
{
    Task<(int sent, List<string> failed)> SendBoardInviteAsync(
        string boardName,
        string inviteCode,
        IReadOnlyList<string> emails,
        CancellationToken ct = default);
}
