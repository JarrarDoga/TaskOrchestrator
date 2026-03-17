using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Members;

public static class MemberEndpoints
{
    public static IEndpointRouteBuilder MapMemberEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards/{boardId:int}").WithTags("Members").RequireAuthorization();

        group.MapGet("/members",                     GetMembers);
        group.MapDelete("/members/{targetUserId}",   KickMember);
        group.MapPost("/transfer",                   TransferOwnership);

        // Invite code management
        group.MapGet("/invite",    GetInvite);
        group.MapPost("/invite",   GenerateInvite);
        group.MapDelete("/invite", RevokeInvite);

        return app;
    }

    // Anyone calls the global join endpoint — no boardId in the path
    public static IEndpointRouteBuilder MapJoinEndpoint(this IEndpointRouteBuilder app)
    {
        app.MapPost("/api/join/{code}", JoinBoard)
           .WithTags("Members")
           .RequireAuthorization();
        return app;
    }

    static async Task<IResult> GetMembers(
        int boardId, IUserContext user, IBoardMemberRepository members)
    {
        if (await Guard.RequireMemberAsync(boardId, user, members) is { } err) return err;
        return Results.Ok(await members.GetMembersAsync(boardId));
    }

    static async Task<IResult> KickMember(
        int boardId, string targetUserId,
        IUserContext user, IBoardMemberRepository members,
        IBoardNotifier notifier)
    {
        if (await Guard.RequireOwnerAsync(boardId, user, members) is { } err) return err;

        if (targetUserId == user.UserId)
            return Results.BadRequest("You can't kick yourself. Transfer ownership first.");

        var role = await members.GetRoleAsync(boardId, targetUserId);
        if (role is null) return Results.NotFound("User is not a member of this board.");

        await members.RemoveMemberAsync(boardId, targetUserId);

        // Tell the kicked user's SignalR connection to leave
        await notifier.MemberKickedAsync(boardId, targetUserId);

        return Results.NoContent();
    }

    static async Task<IResult> TransferOwnership(
        int boardId,
        [FromBody] TransferOwnershipRequest req,
        IUserContext user, IBoardMemberRepository members)
    {
        if (await Guard.RequireOwnerAsync(boardId, user, members) is { } err) return err;

        var targetRole = await members.GetRoleAsync(boardId, req.NewOwnerUserId);
        if (targetRole is null)
            return Results.BadRequest("Target user is not a member of this board.");

        // Swap roles atomically enough for a single-user tool
        await members.SetRoleAsync(boardId, user.UserId,         "Member");
        await members.SetRoleAsync(boardId, req.NewOwnerUserId,  "Owner");

        return Results.NoContent();
    }

    static async Task<IResult> GetInvite(
        int boardId, IUserContext user, IBoardMemberRepository members, IInviteRepository invites)
    {
        if (await Guard.RequireOwnerAsync(boardId, user, members) is { } err) return err;
        var invite = await invites.GetActiveAsync(boardId);
        return invite is null ? Results.NotFound() : Results.Ok(invite);
    }

    static async Task<IResult> GenerateInvite(
        int boardId,
        [FromBody] GenerateInviteRequest req,
        IUserContext user, IBoardMemberRepository members, IInviteRepository invites)
    {
        if (await Guard.RequireOwnerAsync(boardId, user, members) is { } err) return err;
        var invite = await invites.GenerateAsync(boardId, user.UserId, req.ExpiresAt, req.MaxUses);
        return Results.Ok(invite);
    }

    static async Task<IResult> RevokeInvite(
        int boardId, IUserContext user, IBoardMemberRepository members, IInviteRepository invites)
    {
        if (await Guard.RequireOwnerAsync(boardId, user, members) is { } err) return err;
        await invites.DeactivateAsync(boardId);
        return Results.NoContent();
    }

    static async Task<IResult> JoinBoard(
        string code, IUserContext user,
        IUserRepository users, IBoardMemberRepository members, IInviteRepository invites)
    {
        var resolved = await invites.ResolveCodeAsync(code);
        if (resolved is null)
            return Results.NotFound("Invite code is invalid, expired, or exhausted.");

        var (boardId, invite) = resolved.Value;

        // Already a member? Just return success — idempotent
        if (await members.IsMemberAsync(boardId, user.UserId))
            return Results.Ok(new { boardId, alreadyMember = true });

        await members.AddMemberAsync(boardId, user.UserId, "Member");
        await invites.IncrementUsesAsync(code);

        return Results.Ok(new { boardId, alreadyMember = false });
    }
}
