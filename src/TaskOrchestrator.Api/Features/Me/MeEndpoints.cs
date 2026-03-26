using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;

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

    static async Task<IResult> Sync(IUserContext user, IUserRepository users, SyncProfileRequest? body)
    {
        // Access tokens with a custom audience don't include profile claims (email, name, picture).
        // The client reads those from the OIDC ID token and sends them in the request body as a fallback.
        var displayName = user.DisplayName ?? body?.DisplayName;
        var email       = user.Email       ?? body?.Email;
        var avatarUrl   = user.AvatarUrl   ?? body?.AvatarUrl;

        await users.UpsertAsync(user.UserId, displayName, email, avatarUrl);
        return Results.Ok(new { user.UserId, displayName, email });
    }

    static async Task<IResult> GetMyBoards(IUserContext user, IBoardRepository boards)
    {
        return Results.Ok(await boards.GetByUserAsync(user.UserId));
    }
}
