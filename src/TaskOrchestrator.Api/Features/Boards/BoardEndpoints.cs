using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Boards;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Boards");

        group.MapGet("/",         GetAll);
        group.MapGet("/{id:int}", GetDetail);
        group.MapPost("/",        Create);
        group.MapDelete("/{id:int}", Delete);

        return app;
    }

    static async Task<IResult> GetAll(IBoardRepository repo) =>
        Results.Ok(await repo.GetAllAsync());

    static async Task<IResult> GetDetail(int id, IBoardRepository repo)
    {
        var board = await repo.GetDetailAsync(id);
        return board is null ? Results.NotFound() : Results.Ok(board);
    }

    static async Task<IResult> Create([FromBody] CreateBoardRequest req, IBoardRepository repo)
    {
        if (string.IsNullOrWhiteSpace(req.Name))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Name"] = ["Name is required."] });

        var board = await repo.CreateAsync(req);
        return Results.Created($"/api/boards/{board.Id}", board);
    }

    static async Task<IResult> Delete(int id, IBoardRepository repo) =>
        await repo.DeleteAsync(id) ? Results.NoContent() : Results.NotFound();
}
