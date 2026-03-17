using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;

namespace TaskOrchestrator.Api.Features.Me;

public static class MeEndpoints
{
    public static IEndpointRouteBuilder MapMeEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/me").WithTags("Me").RequireAuthorization();

        // Called by the client immediately after login to sync the Auth0 profile
        // into the Users table so display names are available for member lists.
        group.MapPost("/sync", Sync);

        group.MapGet("/boards", GetMyBoards);

        return app;
    }

    static async Task<IResult> Sync(IUserContext user, IUserRepository users)
    {
        await users.UpsertAsync(user.UserId, user.DisplayName, user.Email, user.AvatarUrl);
        return Results.Ok(new { user.UserId, user.DisplayName, user.Email });
    }

    static async Task<IResult> GetMyBoards(IUserContext user, IBoardRepository boards)
    {
        return Results.Ok(await boards.GetByUserAsync(user.UserId));
    }
}
