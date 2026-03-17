using System.Security.Claims;

namespace TaskOrchestrator.Api.Services;

public sealed class UserContext(IHttpContextAccessor http) : IUserContext
{
    private ClaimsPrincipal? Principal => http.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    // Auth0 puts the user ID in the 'sub' claim
    public string UserId =>
        Principal?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? Principal?.FindFirst("sub")?.Value
        ?? throw new InvalidOperationException("User is not authenticated.");

    public string? DisplayName =>
        Principal?.FindFirst("name")?.Value
        ?? Principal?.FindFirst(ClaimTypes.Name)?.Value;

    public string? Email =>
        Principal?.FindFirst(ClaimTypes.Email)?.Value
        ?? Principal?.FindFirst("email")?.Value;

    public string? AvatarUrl =>
        Principal?.FindFirst("picture")?.Value;
}
