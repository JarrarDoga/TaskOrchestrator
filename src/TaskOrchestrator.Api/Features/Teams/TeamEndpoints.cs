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
        group.MapPost("/",                             Create);
        group.MapDelete("/{id:int}",                  Delete);
        group.MapPost("/{id:int}/members",             AddMember);
        group.MapDelete("/{id:int}/members/{userId}", RemoveMember);
        group.MapPost("/{id:int}/invite-email",        InviteByEmail);

        // User search (used by the invite input in the modal)
        app.MapGet("/api/users/search", SearchUsers).RequireAuthorization();

        return app;
    }

    static async Task<IResult> GetAll(IUserContext user, ITeamRepository teams) =>
        Results.Ok(await teams.GetAllAsync(user.UserId));

    static async Task<IResult> GetById(int id, ITeamRepository teams)
    {
        var team = await teams.GetByIdAsync(id);
        return team is null ? Results.NotFound() : Results.Ok(team);
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
        IUserContext user, ITeamRepository teams, IEmailService email,
        IConfiguration config)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();

        var team = await teams.GetByIdAsync(id);
        if (team is null) return Results.NotFound();

        if (string.IsNullOrWhiteSpace(req.Email) || !req.Email.Contains('@'))
            return Results.BadRequest("A valid email address is required.");

        var clientBaseUrl = config["InviteEmail:ClientBaseUrl"] ?? "https://task-orchestrator-phi.vercel.app";

        try
        {
            await email.SendTeamInviteAsync(req.Email.Trim(), team.Name, clientBaseUrl);
            return Results.Ok();
        }
        catch (Exception ex)
        {
            return Results.Problem($"Failed to send invite email: {ex.Message}");
        }
    }

    static async Task<IResult> SearchUsers(
        [FromQuery] string q, IUserRepository users)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return Results.Ok(Array.Empty<UserSearchDto>());

        return Results.Ok(await users.SearchAsync(q));
    }
}

public record AddTeamMemberRequest(string UserId);
public record InviteByEmailRequest(string Email);
