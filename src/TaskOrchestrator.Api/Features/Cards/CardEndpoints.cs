using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Features.Cards;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Cards");

        group.MapGet("/boards/{boardId:int}/cards", GetByBoard);
        group.MapGet("/cards/{cardId:int}",         GetById);
        group.MapPost("/cards",                     Create);
        group.MapPatch("/cards/{cardId:int}",       Update);
        group.MapPost("/cards/{cardId:int}/move",   Move);
        group.MapDelete("/cards/{cardId:int}",      Delete);

        return app;
    }

    static async Task<IResult> GetByBoard(int boardId, ICardRepository repo) =>
        Results.Ok(await repo.GetByBoardAsync(boardId));

    static async Task<IResult> GetById(int cardId, ICardRepository repo)
    {
        var card = await repo.GetByIdAsync(cardId);
        return card is null ? Results.NotFound() : Results.Ok(card);
    }

    static async Task<IResult> Create(
        [FromBody] CreateCardRequest req,
        ICardRepository cards,
        IActivityRepository activity,
        IBoardNotifier notifier)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Title"] = ["Title is required."] });

        // userId would come from HttpContext.User once auth is wired up
        var card = await cards.CreateAsync(req, userId: null);

        await activity.AppendAsync(card.Id, card.BoardId, ActivityEventType.CardCreated,
            null, null, $"Card \"{card.Title}\" created");

        var evt = new ActivityEventDto(0, card.Id, card.BoardId, ActivityEventType.CardCreated,
            null, null, $"Card \"{card.Title}\" created", DateTime.UtcNow);

        await notifier.CardCreatedAsync(card.BoardId, card);
        await notifier.ActivityAppendedAsync(card.BoardId, evt);

        return Results.Created($"/api/cards/{card.Id}", card);
    }

    static async Task<IResult> Update(
        int cardId,
        [FromBody] UpdateCardRequest req,
        ICardRepository cards,
        IActivityRepository activity,
        IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();

        var updated = await cards.UpdateAsync(cardId, req, userId: null);
        if (updated is null)
        {
            var snapshot = await cards.GetByIdAsync(cardId);
            return Results.Conflict(new ConflictResponse<CardDto?>(
                "Card was modified by someone else. Review the current version before retrying.",
                snapshot));
        }

        await activity.AppendAsync(updated.Id, updated.BoardId, ActivityEventType.CardUpdated,
            null, null, $"Card \"{updated.Title}\" updated");

        await notifier.CardUpdatedAsync(updated.BoardId, updated);
        return Results.Ok(updated);
    }

    static async Task<IResult> Move(
        int cardId,
        [FromBody] MoveCardRequest req,
        ICardRepository cards,
        IActivityRepository activity,
        IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();

        var updated = await cards.MoveAsync(cardId, req, userId: null);
        if (updated is null)
        {
            var snapshot = await cards.GetByIdAsync(cardId);
            return Results.Conflict(new ConflictResponse<CardDto?>(
                "Card was moved by someone else. Your move could not be applied.",
                snapshot));
        }

        await activity.AppendAsync(updated.Id, updated.BoardId, ActivityEventType.CardMoved,
            null, null, $"Card \"{updated.Title}\" moved to column {updated.ColumnId}");

        await notifier.CardUpdatedAsync(updated.BoardId, updated);
        return Results.Ok(updated);
    }

    static async Task<IResult> Delete(
        int cardId,
        ICardRepository cards,
        IActivityRepository activity,
        IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();

        await cards.DeleteAsync(cardId);

        await activity.AppendAsync(existing.Id, existing.BoardId, ActivityEventType.CardDeleted,
            null, null, $"Card \"{existing.Title}\" deleted");

        await notifier.CardDeletedAsync(existing.BoardId, cardId);
        return Results.NoContent();
    }
}
