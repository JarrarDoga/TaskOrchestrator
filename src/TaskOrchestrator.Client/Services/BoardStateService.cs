using System.Net.Http.Json;
using System.Text.Json;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Client.Services;

// Scoped per board page lifetime. Handles local state, optimistic updates,
// and merging inbound SignalR events safely.
public sealed class BoardStateService(HttpClient http)
{
    public BoardDetailDto? Board { get; private set; }
    public List<UserPresenceDto> ActiveUsers { get; } = [];
    public List<ActivityEventDto> RecentActivity { get; } = [];
    public bool IsLoading { get; private set; }
    public string? LoadError { get; private set; }
    public ConflictInfo? PendingConflict { get; private set; }

    public event Action? OnChange;

    public async Task LoadBoardAsync(int boardId)
    {
        IsLoading = true;
        LoadError = null;
        NotifyChange();

        try
        {
            Board = await http.GetFromJsonAsync<BoardDetailDto>($"/api/boards/{boardId}");
            var activity = await http.GetFromJsonAsync<List<ActivityEventDto>>(
                $"/api/boards/{boardId}/activity") ?? [];
            RecentActivity.AddRange(activity.Take(50));
        }
        catch (Exception ex)
        {
            LoadError = ex.Message;
        }
        finally
        {
            IsLoading = false;
            NotifyChange();
        }
    }

    // Optimistically moves card in local state, returns the old position
    // so callers can revert if the API call fails.
    public (int OldColumnId, int OldPosition) OptimisticMove(int cardId, int targetColumnId, int targetPosition)
    {
        if (Board is null) return (-1, -1);

        var (card, oldColumn) = FindCard(cardId);
        if (card is null || oldColumn is null) return (-1, -1);

        int oldColumnId = card.ColumnId;
        int oldPos = card.Position;

        // Remove from source
        var srcCards = new List<CardDto>(oldColumn.Cards.Where(c => c.Id != cardId));
        // Re-number positions in source
        for (int i = 0; i < srcCards.Count; i++)
            srcCards[i] = srcCards[i] with { Position = i };

        // Insert into target
        var targetColumn = Board.Columns.First(c => c.Id == targetColumnId);
        var dstCards = new List<CardDto>(targetColumn.Cards);
        var updatedCard = card with { ColumnId = targetColumnId, Position = targetPosition };
        dstCards.Insert(Math.Clamp(targetPosition, 0, dstCards.Count), updatedCard);
        for (int i = 0; i < dstCards.Count; i++)
            dstCards[i] = dstCards[i] with { Position = i };

        RebuildBoard(oldColumnId, srcCards, targetColumnId, dstCards);
        NotifyChange();
        return (oldColumnId, oldPos);
    }

    public void RevertMove(int cardId, int oldColumnId, int oldPosition)
    {
        if (Board is null) return;

        var (card, currentColumn) = FindCard(cardId);
        if (card is null || currentColumn is null) return;

        // Remove from current location
        var srcCards = new List<CardDto>(currentColumn.Cards.Where(c => c.Id != cardId));

        // Put back in original column at original position
        var originalColumn = Board.Columns.First(c => c.Id == oldColumnId);
        var dstCards = new List<CardDto>(originalColumn.Cards);
        dstCards.Insert(Math.Clamp(oldPosition, 0, dstCards.Count), card with { ColumnId = oldColumnId, Position = oldPosition });

        RebuildBoard(currentColumn.Id, srcCards, oldColumnId, dstCards);
        NotifyChange();
    }

    public void ApplyCardCreated(CardDto card)
    {
        if (Board is null) return;
        var col = Board.Columns.FirstOrDefault(c => c.Id == card.ColumnId);
        if (col is null) return;

        var cards = new List<CardDto>(col.Cards) { card };
        RebuildBoard(card.ColumnId, cards, card.ColumnId, cards);
        NotifyChange();
    }

    public void ApplyCardUpdated(CardDto incoming)
    {
        if (Board is null) return;

        var (existing, col) = FindCard(incoming.Id);
        if (existing is null || col is null) return;

        // If the incoming card is for a different column, it's a move via SignalR
        if (existing.ColumnId != incoming.ColumnId)
        {
            var oldCards = col.Cards.Where(c => c.Id != incoming.Id).ToList();
            var newCol = Board.Columns.First(c => c.Id == incoming.ColumnId);
            var newCards = new List<CardDto>(newCol.Cards) { incoming };
            newCards = newCards.OrderBy(c => c.Position).ToList();
            RebuildBoard(col.Id, oldCards, incoming.ColumnId, newCards);
        }
        else
        {
            var updated = col.Cards.Select(c => c.Id == incoming.Id ? incoming : c).ToList();
            RebuildSingleColumn(incoming.ColumnId, updated);
        }

        NotifyChange();
    }

    public void ApplyCardDeleted(int cardId)
    {
        if (Board is null) return;
        var (_, col) = FindCard(cardId);
        if (col is null) return;

        var updated = col.Cards.Where(c => c.Id != cardId).ToList();
        RebuildSingleColumn(col.Id, updated);
        NotifyChange();
    }

    public void ApplyAttachmentAdded(int cardId, AttachmentDto attachment)
    {
        if (Board is null) return;
        var (card, col) = FindCard(cardId);
        if (card is null || col is null) return;

        var newAttachments = new List<AttachmentDto>(card.Attachments) { attachment };
        var updatedCard = card with { Attachments = newAttachments };
        var updated = col.Cards.Select(c => c.Id == cardId ? updatedCard : c).ToList();
        RebuildSingleColumn(col.Id, updated);
        NotifyChange();
    }

    public void ApplyPresence(BoardPresenceSnapshot snapshot)
    {
        ActiveUsers.Clear();
        ActiveUsers.AddRange(snapshot.Users);
        NotifyChange();
    }

    public void AppendActivity(ActivityEventDto evt)
    {
        RecentActivity.Insert(0, evt);
        if (RecentActivity.Count > 100) RecentActivity.RemoveAt(100);
        NotifyChange();
    }

    public void SetConflict(ConflictInfo conflict)
    {
        PendingConflict = conflict;
        NotifyChange();
    }

    public void ClearConflict()
    {
        PendingConflict = null;
        NotifyChange();
    }

    private (CardDto? card, ColumnWithCardsDto? col) FindCard(int cardId)
    {
        if (Board is null) return (null, null);
        foreach (var col in Board.Columns)
        {
            var card = col.Cards.FirstOrDefault(c => c.Id == cardId);
            if (card is not null) return (card, col);
        }
        return (null, null);
    }

    private void RebuildBoard(int colAId, List<CardDto> colACards, int colBId, List<CardDto> colBCards)
    {
        if (Board is null) return;
        Board = Board with
        {
            Columns = Board.Columns.Select(col =>
            {
                if (col.Id == colAId) return col with { Cards = colACards };
                if (col.Id == colBId) return col with { Cards = colBCards };
                return col;
            }).ToList()
        };
    }

    private void RebuildSingleColumn(int colId, List<CardDto> cards) =>
        RebuildBoard(colId, cards, colId, cards);

    private void NotifyChange() => OnChange?.Invoke();
}

public record ConflictInfo(
    string Message,
    CardDto? ServerSnapshot,
    CardDto? LocalVersion,
    ConflictKind Kind
);

public enum ConflictKind { Move, Edit }
