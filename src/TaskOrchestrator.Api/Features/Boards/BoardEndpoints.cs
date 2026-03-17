using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Features;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Boards;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Boards").RequireAuthorization();

        group.MapGet("/",                  GetMyBoards);   // redundant with /api/me/boards but convenient
        group.MapGet("/{id:int}",          GetDetail);
        group.MapGet("/{id:int}/activity", GetActivity);
        group.MapPost("/",                 Create);
        group.MapDelete("/{id:int}",       Delete);

        return app;
    }

    static async Task<IResult> GetMyBoards(IUserContext user, IBoardRepository boards) =>
        Results.Ok(await boards.GetByUserAsync(user.UserId));

    static async Task<IResult> GetDetail(
        int id, IUserContext user, IBoardMemberRepository members, IBoardRepository boards)
    {
        if (await Guard.RequireMemberAsync(id, user, members) is { } err) return err;
        var board = await boards.GetDetailAsync(id);
        return board is null ? Results.NotFound() : Results.Ok(board);
    }

    static async Task<IResult> GetActivity(
        int id, IUserContext user, IBoardMemberRepository members, IActivityRepository activity)
    {
        if (await Guard.RequireMemberAsync(id, user, members) is { } err) return err;
        return Results.Ok(await activity.GetByBoardAsync(id));
    }

    static async Task<IResult> Create(
        [FromBody] CreateBoardRequest req,
        IUserContext user, IBoardRepository boards)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Name"] = ["Name is required."] });

        var board = await boards.CreateAsync(req, user.UserId);
        return Results.Created($"/api/boards/{board.Id}", board);
    }

    static async Task<IResult> Delete(
        int id, IUserContext user, IBoardMemberRepository members, IBoardRepository boards)
    {
        if (await Guard.RequireOwnerAsync(id, user, members) is { } err) return err;
        return await boards.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
    }
}
