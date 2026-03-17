namespace ef_core_essentials_blazor.Services;

/// <summary>
/// Service for getting current user and site context
/// In production, this would read from JWT token or session
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    int? SiteId { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";
    
    // In production: read from JWT claims or similar
    public int? SiteId => 1; // Mock for demo - would come from token/session
}
