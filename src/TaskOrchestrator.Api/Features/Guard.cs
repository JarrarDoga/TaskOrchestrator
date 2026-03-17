using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;

namespace TaskOrchestrator.Api.Features;

// Shared helpers for auth checks across endpoint files.
// Return non-null IResult means the check failed — callers return it immediately.
internal static class Guard
{
    internal static async Task<IResult?> RequireMemberAsync(
        int boardId, IUserContext user, IBoardMemberRepository members)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        if (!await members.IsMemberAsync(boardId, user.UserId)) return Results.Forbid();
        return null;
    }

    internal static async Task<IResult?> RequireOwnerAsync(
        int boardId, IUserContext user, IBoardMemberRepository members)
    {
        if (!user.IsAuthenticated) return Results.Unauthorized();
        var role = await members.GetRoleAsync(boardId, user.UserId);
        if (role is null) return Results.Forbid();
        if (role != "Owner") return Results.Forbid();
        return null;
    }
}
