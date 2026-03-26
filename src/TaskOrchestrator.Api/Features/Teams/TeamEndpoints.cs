using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Teams;

public static class TeamEndpoints
{
    public static IEndpointRouteBuilder MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams").WithTags("Teams").RequireAuthorization();

        group.MapGet("/",                              GetAll);
        group.MapGet("/{id:int}",                     GetById);
        group.MapGet("/{id:int}/boards",              GetBoards);
        group.MapPost("/",                             Create);
        group.MapPost("/{id:int}/boards",             CreateBoard);
        group.MapDelete("/{id:int}",                  Delete);
        group.MapPost("/{id:int}/members",             AddMember);
        group.MapDelete("/{id:int}/members/{userId}", RemoveMember);
        group.MapPost("/{id:int}/invite-email",        InviteByEmail);

        // Invite acceptance (token is public-readable but accept requires auth + email match)
        app.MapGet("/api/teams/invites/{token}",        GetInviteInfo).AllowAnonymous();
        app.MapPost("/api/teams/invites/{token}/accept", AcceptInvite).RequireAuthorization();

        // User search
        app.MapGet("/api/users/search", SearchUsers).RequireAuthorization();

        return app;
    }

    static async Task<IResult> GetAll(IUserContext user, ITeamRepository teams) =>
        Results.Ok(await teams.GetAllAsync(user.UserId));

    static async Task<IResult> GetById(int id, IUserContext user, ITeamRepository teams)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        var team = await teams.GetByIdAsync(id);
        if (team is null) return Results.NotFound();
        if (!await teams.IsMemberAsync(id, user.UserId)) return Results.Forbid();
        return Results.Ok(team);
    }

    static async Task<IResult> GetBoards(int id, IUserContext user, ITeamRepository teams)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (!await teams.IsMemberAsync(id, user.UserId)) return Results.Forbid();
        return Results.Ok(await teams.GetBoardsAsync(id, user.UserId));
    }

    static async Task<IResult> Create(
        [FromBody] CreateTeamRequest req,
        IUserContext user, ITeamRepository teams)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Name"] = ["Name is required."] });

        var team = await teams.CreateAsync(req, user.UserId);
        return Results.Created($"/api/teams/{team.Id}", team);
    }

    static async Task<IResult> Delete(int id, IUserContext user, ITeamRepository teams)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (!await teams.IsOwnerAsync(id, user.UserId)) return Results.Forbid();
        return await teams.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
    }

    static async Task<IResult> CreateBoard(
        int id,
        [FromBody] TeamCreateBoardRequest req,
        IUserContext user,
        ITeamRepository teams,
        IBoardRepository boards)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Name"] = ["Name is required."] });

        if (!await teams.IsMemberAsync(id, user.UserId)) return Results.Forbid();

        var board = await boards.CreateAsync(
            new CreateBoardRequest(req.Name.Trim(), req.Description?.Trim(), id),
            user.UserId);
        return Results.Created($"/api/boards/{board.Id}", board);
    }

    static async Task<IResult> AddMember(
        int id, [FromBody] AddTeamMemberRequest req,
        IUserContext user, ITeamRepository teams)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (!await teams.IsOwnerAsync(id, user.UserId)) return Results.Forbid();
        await teams.AddMemberAsync(id, req.UserId);
        return Results.NoContent();
    }

    static async Task<IResult> RemoveMember(
        int id, string userId,
        IUserContext user, ITeamRepository teams)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (!await teams.IsOwnerAsync(id, user.UserId)) return Results.Forbid();
        await teams.RemoveMemberAsync(id, userId);
        return Results.NoContent();
    }

    static async Task<IResult> InviteByEmail(
        int id,
        [FromBody] InviteByEmailRequest req,
        IUserContext user,
        ITeamRepository teams,
        ITeamInviteRepository invites,
        IEmailService email,
        IConfiguration config)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();

        var team = await teams.GetByIdAsync(id);
        if (team is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return Results.BadRequest("A valid email address is required.");

        var clientBaseUrl = config["InviteEmail:ClientBaseUrl"] ?? "https://task-orchestrator-phi.vercel.app";
        var expiresAt     = DateTime.UtcNow.AddDays(7);
        var token         = await invites.CreateAsync(id, req.Email.Trim().ToLowerInvariant(), user.UserId, expiresAt);

        try
        {
            await email.SendTeamInviteAsync(req.Email.Trim(), team.Name, token, clientBaseUrl);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to send invite email: {ex.Message}");
        }
    }

    // Public peek — lets the JoinTeam page show team name / status before the user decides to sign in
    static async Task<IResult> GetInviteInfo(string token, ITeamInviteRepository invites, ITeamRepository teams)
    {
        var invite = await invites.GetByTokenAsync(token);
        if (invite is null) return Results.NotFound();

        var team = await teams.GetByIdAsync(invite.TeamId);
        var teamName = team?.Name ?? "Unknown Team";

        return Results.Ok(new TeamInviteInfoDto(
            teamName,
            invite.InviteeEmail,
            IsExpired: invite.ExpiresAt < DateTime.UtcNow,
            IsUsed:    invite.AcceptedAt.HasValue
        ));
    }

    // Requires auth — token possession is proof of identity (32-char random hex, one-time use)
    static async Task<IResult> AcceptInvite(
        string token,
        IUserContext user,
        ITeamInviteRepository invites,
        ITeamRepository teams)
    {
        var invite = await invites.GetByTokenAsync(token);
        if (invite is null)                     return Results.NotFound("Invite not found.");
        if (invite.AcceptedAt.HasValue)         return Results.Conflict("This invite has already been used.");
        if (invite.ExpiresAt < DateTime.UtcNow) return Results.Problem(title: "Expired", detail: "This invite has expired.", statusCode: 410);

        // Idempotent: already a member is fine
        if (!await teams.IsMemberAsync(invite.TeamId, user.UserId))
            await teams.AddMemberAsync(invite.TeamId, user.UserId);

        await invites.AcceptAsync(token);

        return Results.Ok(new { teamId = invite.TeamId });
    }

    static async Task<IResult> SearchUsers([FromQuery] string q, IUserRepository users)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.Ok(Array.Empty<UserSearchDto>());

        return Results.Ok(await users.SearchAsync(q));
    }
}

public record AddTeamMemberRequest(string UserId);
public record InviteByEmailRequest(string Email);
