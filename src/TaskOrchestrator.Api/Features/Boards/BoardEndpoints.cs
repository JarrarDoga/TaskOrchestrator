using Dapper;
using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Persistence;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Api.Features.Boards;

public static class BoardEndpoints
{
    public static IEndpointRouteBuilder MapBoardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/boards").WithTags("Boards");

        group.MapGet("/", GetAllBoards);
        group.MapGet("/{id:int}", GetBoardById);
        group.MapPost("/", CreateBoard);
        group.MapDelete("/{id:int}", DeleteBoard);

        return app;
    }

    private static async Task<IResult> GetAllBoards(IDbConnectionFactory db)
    {
        using var conn = db.CreateConnection();
        var boards = await conn.QueryAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards ORDER BY CreatedAt DESC");
        return Results.Ok(boards);
    }

    private static async Task<IResult> GetBoardById(int id, IDbConnectionFactory db)
    {
        using var conn = db.CreateConnection();
        var board = await conn.QuerySingleOrDefaultAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards WHERE Id = @Id",
            new { Id = id });
        return board is null ? Results.NotFound() : Results.Ok(board);
    }

    private static async Task<IResult> CreateBoard(
        [FromBody] CreateBoardRequest request,
        IDbConnectionFactory db)
    {
        using var conn = db.CreateConnection();
        var id = await conn.ExecuteScalarAsync<int>(
            """
            INSERT INTO Boards (Name, Description, CreatedAt, Version)
            VALUES (@Name, @Description, UTC_TIMESTAMP(), 1);
            SELECT LAST_INSERT_ID();
            """,
            new { request.Name, request.Description });

        var created = await conn.QuerySingleAsync<BoardDto>(
            "SELECT Id, Name, Description, CreatedAt, Version FROM Boards WHERE Id = @Id",
            new { Id = id });

        return Results.Created($"/api/boards/{id}", created);
    }

    private static async Task<IResult> DeleteBoard(int id, IDbConnectionFactory db)
    {
        using var conn = db.CreateConnection();
        var rows = await conn.ExecuteAsync(
            "DELETE FROM Boards WHERE Id = @Id", new { Id = id });
        return rows > 0 ? Results.NoContent() : Results.NotFound();
    }
}
