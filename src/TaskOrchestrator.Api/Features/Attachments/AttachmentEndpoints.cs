using TaskOrchestrator.Api.Features;
using TaskOrchestrator.Api.Hubs;
using TaskOrchestrator.Api.Persistence.Repositories;
using TaskOrchestrator.Api.Services;
using TaskOrchestrator.Api.Services.FileStorage;
using TaskOrchestrator.Shared.Contracts;
using TaskOrchestrator.Shared.Enums;

namespace TaskOrchestrator.Api.Features.Attachments;

public static class AttachmentEndpoints
{
    private const long MaxFileSizeBytes = 20 * 1024 * 1024; // 20 MB

    private static readonly HashSet<string> AllowedTypes =
    [
        "image/png", "image/jpeg", "image/gif", "image/webp",
        "application/pdf", "text/plain",
        "application/zip", "application/octet-stream"
    ];

    public static IEndpointRouteBuilder MapAttachmentEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api").WithTags("Attachments").RequireAuthorization();

        group.MapPost("/cards/{cardId:int}/attachments", Upload).DisableAntiforgery();
        group.MapGet("/attachments/{id:int}/download",   Download);

        return app;
    }

    static async Task<IResult> Upload(
        int cardId,
        IFormFile file,
        IUserContext user,
        IBoardMemberRepository members,
        ICardRepository cards,
        IAttachmentRepository attachments,
        IActivityRepository activity,
        IFileStorageService storage,
        IBoardNotifier notifier)
    {
        var card = await cards.GetByIdAsync(cardId);
        if (card is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(card.BoardId, user, members) is { } err) return err;

        if (file.Length > MaxFileSizeBytes)
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["file"] = [$"File exceeds maximum size of {MaxFileSizeBytes / 1024 / 1024} MB."] });

        var contentType = file.ContentType.ToLowerInvariant();
        if (!AllowedTypes.Contains(contentType))
            return Results.ValidationProblem(new Dictionary<string, string[]>
                { ["file"] = [$"File type '{contentType}' is not allowed."] });

        await using var stream = file.OpenReadStream();
        var storagePath = await storage.SaveAsync(stream, file.FileName, contentType);

        var request = new RegisterAttachmentRequest(
            cardId, file.FileName, contentType, file.Length, storagePath);

        var attachment = await attachments.CreateAsync(request, user.UserId);

        await activity.AppendAsync(cardId, card.BoardId, ActivityEventType.AttachmentAdded,
            user.UserId, user.DisplayName, $"Attached \"{file.FileName}\"");

        await notifier.AttachmentAddedAsync(card.BoardId, cardId, attachment);

        return Results.Created($"/api/attachments/{attachment.Id}/download",
            new UploadAttachmentResponse(attachment));
    }

    static async Task<IResult> Download(
        int id,
        IUserContext user,
        IBoardMemberRepository members,
        IAttachmentRepository attachments,
        ICardRepository cards,
        IFileStorageService storage)
    {
        var meta = await attachments.GetByIdAsync(id);
        if (meta is null) return Results.NotFound();

        // Verify the requesting user is a member of the board this attachment belongs to
        var card = await cards.GetByIdAsync(meta.CardId);
        if (card is null) return Results.NotFound();
        if (await Guard.RequireMemberAsync(card.BoardId, user, members) is { } err) return err;

        var storagePath = await attachments.GetStoragePathAsync(id);
        if (storagePath is null) return Results.NotFound();

        var (stream, _) = await storage.ReadAsync(storagePath);
        return Results.File(stream, meta.ContentType, meta.FileName);
    }
}
