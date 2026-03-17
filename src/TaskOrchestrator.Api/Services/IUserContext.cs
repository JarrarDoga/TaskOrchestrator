namespace TaskOrchestrator.Api.Services;

public interface IUserContext
{
    bool IsAuthenticated { get; }
    string UserId { get; }      // throws if not authenticated
    string? DisplayName { get; }
    string? Email { get; }
    string? AvatarUrl { get; }
}
