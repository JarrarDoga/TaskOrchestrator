using System.Net.Http.Headers;
using System.Net.Http.Json;

namespace TaskOrchestrator.Api.Services.Invites;

public sealed class ResendInviteEmailService(
    HttpClient http,
    IConfiguration config,
    ILogger<ResendInviteEmailService> log) : IInviteEmailService
{
    public async Task<(int sent, List<string> failed)> SendBoardInviteAsync(
        string boardName,
        string inviteCode,
        IReadOnlyList<string> emails,
        CancellationToken ct = default)
    {
        var apiKey = config["Resend:ApiKey"];
        var fromEmail = config["Resend:FromEmail"];
        var fromName = config["Resend:FromName"] ?? "Task Orchestrator";
        var clientBaseUrl = config["InviteEmail:ClientBaseUrl"];

        if (string.IsNullOrWhiteSpace(apiKey) ||
            string.IsNullOrWhiteSpace(fromEmail) ||
            string.IsNullOrWhiteSpace(clientBaseUrl))
        {
            throw new InvalidOperationException(
                "Invite email is not configured. Set Resend:ApiKey, Resend:FromEmail, InviteEmail:ClientBaseUrl.");
        }

        var joinLink = $"{clientBaseUrl.TrimEnd('/')}/join/{inviteCode}";
        var subject = $"You are invited to join '{boardName}'";

        http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

        var sent = 0;
        var failed = new List<string>();

        foreach (var email in emails.Distinct(StringComparer.OrdinalIgnoreCase))
        {
            var html =
                $"<p>You were invited to join <strong>{System.Net.WebUtility.HtmlEncode(boardName)}</strong>.</p>" +
                $"<p>Use this secure invite link:</p>" +
                $"<p><a href=\"{joinLink}\">{joinLink}</a></p>" +
                "<p>If you do not have an account yet, sign in first and then open the link.</p>";

            var payload = new
            {
                from = $"{fromName} <{fromEmail}>",
                to = email,
                subject,
                html
            };

            try
            {
                var resp = await http.PostAsJsonAsync("https://api.resend.com/emails", payload, ct);
                if (resp.IsSuccessStatusCode) sent++;
                else failed.Add(email);
            }
            catch (Exception ex)
            {
                log.LogWarning(ex, "Failed to send invite email to {Email}", email);
                failed.Add(email);
            }
        }

        return (sent, failed);
    }
}
