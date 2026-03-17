using Microsoft.AspNetCore.SignalR.Client;
using TaskOrchestrator.Shared.Contracts;

namespace TaskOrchestrator.Client.Services;

public sealed class SignalRService : IAsyncDisposable
{
    private HubConnection? _hub;
    private readonly IConfiguration _config;

    public HubConnectionState State => _hub?.State ?? HubConnectionState.Disconnected;
    public event Action? OnStateChanged;

    public SignalRService(IConfiguration config) => _config = config;

    public async Task ConnectAsync(int boardId, BoardStateService state,
        string userId = "anonymous", string displayName = "Guest")
    {
        var baseUrl = _config["ApiBaseUrl"] ?? "http://localhost:5150";

        _hub = new HubConnectionBuilder()
            .WithUrl($"{baseUrl}/hubs/board")
            .WithAutomaticReconnect()
            .Build();

        _hub.Reconnecting += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };
        _hub.Reconnected += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };
        _hub.Closed += _ => { OnStateChanged?.Invoke(); return Task.CompletedTask; };

        _hub.On<CardDto>("CardCreated",     state.ApplyCardCreated);
        _hub.On<CardDto>("CardUpdated",     state.ApplyCardUpdated);
        _hub.On<object>("CardDeleted",      raw => HandleCardDeleted(raw, state));
        _hub.On<object>("AttachmentAdded",  raw => HandleAttachmentAdded(raw, state));
        _hub.On<ActivityEventDto>("ActivityAppended", state.AppendActivity);
        _hub.On<BoardPresenceSnapshot>("PresenceChanged", state.ApplyPresence);

        await _hub.StartAsync();
        await _hub.SendAsync("JoinBoard", boardId, userId, displayName);
        OnStateChanged?.Invoke();
    }

    public async Task DisconnectAsync(int boardId)
    {
        if (_hub is null) return;
        if (_hub.State == HubConnectionState.Connected)
            await _hub.SendAsync("LeaveBoard", boardId);
        await _hub.StopAsync();
        OnStateChanged?.Invoke();
    }

    private static void HandleCardDeleted(object raw, BoardStateService state)
    {
        // SignalR sends { boardId, cardId } as anonymous object
        if (raw is System.Text.Json.JsonElement el && el.TryGetProperty("cardId", out var idEl))
            state.ApplyCardDeleted(idEl.GetInt32());
    }

    private static void HandleAttachmentAdded(object raw, BoardStateService state)
    {
        if (raw is not System.Text.Json.JsonElement el) return;
        if (!el.TryGetProperty("cardId", out var cardIdEl)) return;
        if (!el.TryGetProperty("attachment", out var attachEl)) return;

        var attachment = System.Text.Json.JsonSerializer.Deserialize<AttachmentDto>(attachEl.GetRawText());
        if (attachment is not null)
            state.ApplyAttachmentAdded(cardIdEl.GetInt32(), attachment);
    }

    public async ValueTask DisposeAsync()
    {
        if (_hub is not null) await _hub.DisposeAsync();
    }
}
