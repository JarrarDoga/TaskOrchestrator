namespace TaskOrchestrator.Api.Services;

public interface IEmailService
{
    Task SendTeamInviteAsync(string toEmail, string teamName, string inviteToken, string clientBaseUrl);
}
