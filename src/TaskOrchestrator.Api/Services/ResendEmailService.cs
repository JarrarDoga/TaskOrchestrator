using System.Net.Http.Headers;

namespace TaskOrchestrator.Api.Services;

public sealed class ResendEmailService(IHttpClientFactory httpClientFactory, IConfiguration config) : IEmailService
{
    public async Task SendTeamInviteAsync(string toEmail, string teamName, string inviteToken, string clientBaseUrl)
    {
        var apiKey    = config["Resend:ApiKey"]    ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");
        var fromEmail = config["Resend:FromEmail"] ?? throw new InvalidOperationException("Resend:FromEmail is not configured.");

        var joinLink = $"{clientBaseUrl.TrimEnd('/')}/join-team/{inviteToken}";

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            from    = fromEmail,
            to      = new[] { toEmail },
            subject = $"You've been invited to join {teamName} on TaskOrchestrator",
            html    = $"""
                       <div style="font-family:sans-serif;max-width:480px;margin:auto;padding:24px">
                         <h2 style="margin-bottom:8px">You're invited!</h2>
                         <p>You've been invited to join the <strong>{teamName}</strong> team on TaskOrchestrator.</p>
                         <p>This link is personal — it only works when you sign in with <strong>{toEmail}</strong>.</p>
                         <p style="margin:24px 0">
                           <a href="{joinLink}"
                              style="background:#6366f1;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block;font-weight:bold">
                             Accept invitation
                           </a>
                         </p>
                         <p style="color:#888;font-size:12px">
                           If you don't have an account yet, sign up with this email address first, then click the link again.
                           This invite expires in 7 days.
                         </p>
                       </div>
                       """
        };

        var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload);
        response.EnsureSuccessStatusCode();
    }
}
