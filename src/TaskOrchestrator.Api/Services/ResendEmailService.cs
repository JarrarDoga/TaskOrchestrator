using System.Net.Http.Headers;

namespace TaskOrchestrator.Api.Services;

public sealed class ResendEmailService(IHttpClientFactory httpClientFactory, IConfiguration config) : IEmailService
{
    public async Task SendTeamInviteAsync(string toEmail, string teamName, string clientBaseUrl)
    {
        var apiKey    = config["Resend:ApiKey"]      ?? throw new InvalidOperationException("Resend:ApiKey is not configured.");
        var fromEmail = config["Resend:FromEmail"]   ?? throw new InvalidOperationException("Resend:FromEmail is not configured.");

        using var client = httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var payload = new
        {
            from    = fromEmail,
            to      = new[] { toEmail },
            subject = $"You've been invited to join {teamName} on TaskOrchestrator",
            html    = $"""
                       <div style="font-family:sans-serif;max-width:480px;margin:auto">
                         <h2>You're invited!</h2>
                         <p>Someone invited you to join the <strong>{teamName}</strong> team on TaskOrchestrator.</p>
                         <p>
                           <a href="{clientBaseUrl}/teams"
                              style="background:#6366f1;color:#fff;padding:12px 24px;border-radius:8px;text-decoration:none;display:inline-block">
                             View team
                           </a>
                         </p>
                         <p style="color:#888;font-size:12px">If you don't have an account yet, you'll be asked to sign up first.</p>
                       </div>
                       """
        };

        var response = await client.PostAsJsonAsync("https://api.resend.com/emails", payload);
        response.EnsureSuccessStatusCode();
    }
}
