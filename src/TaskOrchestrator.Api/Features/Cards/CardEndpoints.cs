using Microsoft.AspNetCore.Mvc;
using TaskOrchestrator.Api.Features;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Features.Cards;

public static class CardEndpoints
{
    public static IEndpointRouteBuilder MapCardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Cards").RequireAuthorization();

        group.MapGet("/boards/{boardId:int}/cards",    GetByBoard);
        group.MapGet("/cards/{cardId:int}",            GetById);
        group.MapGet("/cards/{cardId:int}/activity",   GetActivity);
        group.MapPost("/cards",                        Create);
        group.MapPatch("/cards/{cardId:int}",          Update);
        group.MapPost("/cards/{cardId:int}/move",      Move);
        group.MapDelete("/cards/{cardId:int}",         Delete);

        return app;
    }

    static async Task<IResult> GetByBoard(
        int boardId, IUserContext user, IBoardMemberRepository members, ICardRepository cards)
    {
        if (await Guard.RequireMemberAsync(boardId, user, members) is { } err) return err;
        return Results.Ok(await cards.GetByBoardAsync(boardId));
    }

    static async Task<IResult> GetById(
        int cardId, IUserContext user, IBoardMemberRepository members, ICardRepository cards)
    {
        var card = await cards.GetByIdAsync(cardId);
        if (card is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(card.BoardId, user, members) is { } err) return err;
        return Results.Ok(card);
    }

    static async Task<IResult> GetActivity(
        int cardId, IUserContext user, IBoardMemberRepository members,
        ICardRepository cards, IActivityRepository activity)
    {
        var card = await cards.GetByIdAsync(cardId);
        if (card is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(card.BoardId, user, members) is { } err) return err;
        return Results.Ok(await activity.GetByCardAsync(cardId));
    }

    static async Task<IResult> Create(
        [FromBody] CreateCardRequest req,
        IUserContext user, IBoardMemberRepository members,
        ICardRepository cards, IActivityRepository activity, IBoardNotifier notifier)
    {
        if (string.IsNullOrWhiteSpace(req.Title))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["Title"] = ["Title is required."] });

        if (await Guard.RequireMemberAsync(req.BoardId, user, members) is { } err) return err;

        var card = await cards.CreateAsync(req, user.UserId);
        await activity.AppendAsync(card.Id, card.BoardId, ActivityEventType.CardCreated,
            user.UserId, user.DisplayName, $"Card \"{card.Title}\" created");

        var evt = new ActivityEventDto(0, card.Id, card.BoardId, ActivityEventType.CardCreated,
            user.UserId, user.DisplayName, $"Card \"{card.Title}\" created", DateTime.UtcNow);

        await notifier.CardCreatedAsync(card.BoardId, card);
        await notifier.ActivityAppendedAsync(card.BoardId, evt);

        return Results.Created($"/api/cards/{card.Id}", card);
    }

    static async Task<IResult> Update(
        int cardId, [FromBody] UpdateCardRequest req,
        IUserContext user, IBoardMemberRepository members,
        ICardRepository cards, IActivityRepository activity, IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(existing.BoardId, user, members) is { } err) return err;

        var updated = await cards.UpdateAsync(cardId, req, user.UserId);
        if (updated is null)
        {
            var snapshot = await cards.GetByIdAsync(cardId);
            return Results.Conflict(new ConflictResponse<CardDto?>(
                "Card was modified by someone else. Review the current version before retrying.",
                snapshot));
        }

        await activity.AppendAsync(updated.Id, updated.BoardId, ActivityEventType.CardUpdated,
            user.UserId, user.DisplayName, $"Card \"{updated.Title}\" updated");
        await notifier.CardUpdatedAsync(updated.BoardId, updated);
        return Results.Ok(updated);
    }

    static async Task<IResult> Move(
        int cardId, [FromBody] MoveCardRequest req,
        IUserContext user, IBoardMemberRepository members,
        ICardRepository cards, IActivityRepository activity, IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(existing.BoardId, user, members) is { } err) return err;

        var updated = await cards.MoveAsync(cardId, req, user.UserId);
        if (updated is null)
        {
            var snapshot = await cards.GetByIdAsync(cardId);
            return Results.Conflict(new ConflictResponse<CardDto?>(
                "Card was moved by someone else. Your move could not be applied.",
                snapshot));
        }

        await activity.AppendAsync(updated.Id, updated.BoardId, ActivityEventType.CardMoved,
            user.UserId, user.DisplayName, $"Card \"{updated.Title}\" moved");
        await notifier.CardUpdatedAsync(updated.BoardId, updated);
        return Results.Ok(updated);
    }

    static async Task<IResult> Delete(
        int cardId, IUserContext user, IBoardMemberRepository members,
        ICardRepository cards, IActivityRepository activity, IBoardNotifier notifier)
    {
        var existing = await cards.GetByIdAsync(cardId);
        if (existing is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(existing.BoardId, user, members) is { } err) return err;

        await cards.DeleteAsync(cardId);
        await activity.AppendAsync(existing.Id, existing.BoardId, ActivityEventType.CardDeleted,
            user.UserId, user.DisplayName, $"Card \"{existing.Title}\" deleted");
        await notifier.CardDeletedAsync(existing.BoardId, cardId);
        return Results.NoContent();
    }
}
