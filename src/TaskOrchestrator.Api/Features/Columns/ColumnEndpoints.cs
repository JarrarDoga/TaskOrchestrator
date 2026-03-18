using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Columns;

public static class ColumnEndpoints
{
    public static IEndpointRouteBuilder MapColumnEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards/{boardId:int}/columns")
            .WithTags("Columns")
            .RequireAuthorization();

        group.MapPost("/",              Create);
        group.MapDelete("/{columnId:int}", Delete);

        return app;
    }

    static async Task<IResult> Create(
        int boardId,
        [FromBody] CreateColumnRequest req,
        IUserContext user, IBoardMemberRepository members,
        IColumnRepository columns, IBoardNotifier notifier)
    {
        if (await Guard.RequireMemberAsync(boardId, user, members) is { } err) return err;

        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Title"] = ["Title is required."] });

        var column = await columns.CreateAsync(req with { BoardId = boardId });
        await notifier.ColumnCreatedAsync(boardId, column);
        return Results.Created($"/api/boards/{boardId}/columns/{column.Id}", column);
    }

    static async Task<IResult> Delete(
        int boardId, int columnId,
        IUserContext user, IBoardMemberRepository members,
        IColumnRepository columns, IBoardNotifier notifier)
    {
        if (await Guard.RequireMemberAsync(boardId, user, members) is { } err) return err;

        var deleted = await columns.DeleteAsync(columnId);
        if (!deleted) return Results.NotFound();

        await notifier.ColumnDeletedAsync(boardId, columnId);
        return Results.NoContent();
    }
}
