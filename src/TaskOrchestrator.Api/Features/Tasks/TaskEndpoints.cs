using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Features.Tasks;

public static class TaskEndpoints
{
    public static IEndpointRouteBuilder MapTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards/{boardId:int}/tasks").WithTags("Tasks");

        group.MapGet("/", GetTasksByBoard);
        group.MapPost("/", CreateTask);
        group.MapPut("/{taskId:int}", UpdateTask);
        group.MapPatch("/{taskId:int}/move", MoveTask);
        group.MapDelete("/{taskId:int}", DeleteTask);

        return app;
    }

    private static async Task<IResult> GetTasksByBoard(int boardId, IDbConnectionFactory db)
    {
        using var conn = db.CreateConnection();
        var tasks = await conn.QueryAsync<TaskItemDto>(
            """
            SELECT Id, BoardId, Title, Description, Status, Priority,
                   AssignedToUserId, Position, CreatedAt, UpdatedAt, Version
            FROM TaskItems
            WHERE BoardId = @BoardId
            ORDER BY Status, Position
            """,
            new { BoardId = boardId });
        return Results.Ok(tasks);
    }

    private static async Task<IResult> CreateTask(
        int boardId,
        [FromBody] CreateTaskItemRequest request,
        IDbConnectionFactory db,
        IHubContext<TaskHub> hub)
    {
        using var conn = db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO TaskItems (BoardId, Title, Description, Status, Priority, Position, CreatedAt, UpdatedAt, Version)
            VALUES (@BoardId, @Title, @Description, @Status, @Priority,
                    (SELECT COALESCE(MAX(Position), 0) + 1 FROM TaskItems t WHERE t.BoardId = @BoardId AND t.Status = @Status),
                    UTC_TIMESTAMP(), UTC_TIMESTAMP(), 1);
            SELECT LAST_INSERT_ID();
            """,
            new
            {
                BoardId = boardId,
                request.Title,
                request.Description,
                Status = TaskItemStatus.Backlog,
                Priority = request.Priority
            });

        var created = await conn.QuerySingleAsync<TaskItemDto>(
            "SELECT Id, BoardId, Title, Description, Status, Priority, AssignedToUserId, Position, CreatedAt, UpdatedAt, Version FROM TaskItems WHERE Id = @Id",
            new { Id = id });

        await hub.Clients.Group(TaskHub.BoardGroup(boardId))
            .SendAsync(TaskHub.TaskCreated, created);

        return Results.Created($"/api/boards/{boardId}/tasks/{id}", created);
    }

    private static async Task<IResult> UpdateTask(
        int boardId,
        int taskId,
        [FromBody] UpdateTaskItemRequest request,
        IDbConnectionFactory db,
        IHubContext<TaskHub> hub)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            """
            UPDATE TaskItems
            SET Title = @Title, Description = @Description, Status = @Status,
                Priority = @Priority, AssignedToUserId = @AssignedToUserId,
                Position = @Position, UpdatedAt = UTC_TIMESTAMP(), Version = Version + 1
            WHERE Id = @Id AND BoardId = @BoardId AND Version = @Version
            """,
            new { Id = taskId, BoardId = boardId, request.Title, request.Description,
                  request.Status, request.Priority, request.AssignedToUserId,
                  request.Position, request.Version });

        if (rows == 0)
            return Results.Conflict("Task was modified by another user. Refresh and try again.");

        var updated = await conn.QuerySingleAsync<TaskItemDto>(
            "SELECT Id, BoardId, Title, Description, Status, Priority, AssignedToUserId, Position, CreatedAt, UpdatedAt, Version FROM TaskItems WHERE Id = @Id",
            new { Id = taskId });

        await hub.Clients.Group(TaskHub.BoardGroup(boardId))
            .SendAsync(TaskHub.TaskUpdated, updated);

        return Results.Ok(updated);
    }

    private static async Task<IResult> MoveTask(
        int boardId,
        int taskId,
        [FromBody] MoveTaskItemRequest request,
        IDbConnectionFactory db,
        IHubContext<TaskHub> hub)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            """
            UPDATE TaskItems
            SET Status = @NewStatus, Position = @NewPosition,
                UpdatedAt = UTC_TIMESTAMP(), Version = Version + 1
            WHERE Id = @Id AND BoardId = @BoardId AND Version = @Version
            """,
            new { Id = taskId, BoardId = boardId, request.NewStatus, request.NewPosition, request.Version });

        if (rows == 0)
            return Results.Conflict("Task was modified by another user. Refresh and try again.");

        var updated = await conn.QuerySingleAsync<TaskItemDto>(
            "SELECT Id, BoardId, Title, Description, Status, Priority, AssignedToUserId, Position, CreatedAt, UpdatedAt, Version FROM TaskItems WHERE Id = @Id",
            new { Id = taskId });

        await hub.Clients.Group(TaskHub.BoardGroup(boardId))
            .SendAsync(TaskHub.TaskUpdated, updated);

        return Results.Ok(updated);
    }

    private static async Task<IResult> DeleteTask(
        int boardId,
        int taskId,
        IDbConnectionFactory db,
        IHubContext<TaskHub> hub)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM TaskItems WHERE Id = @Id AND BoardId = @BoardId",
            new { Id = taskId, BoardId = boardId });

        if (rows == 0) return Results.NotFound();

        await hub.Clients.Group(TaskHub.BoardGroup(boardId))
            .SendAsync(TaskHub.TaskDeleted, new { taskId, boardId });

        return Results.NoContent();
    }
}
