namespace ef_core_essentials_blazor.Services;

/// <summary>
/// Service for getting current user and site context.
/// SiteId is resolved automatically from the HTTP Host header via SiteContext.
/// </summary>
public interface ICurrentUserService
{
    string? UserId { get; }
    int? SiteId { get; }
}

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _httpContextAccessor;
    private readonly SiteContext _siteContext;

    public CurrentUserService(IHttpContextAccessor httpContextAccessor, SiteContext siteContext)
    {
        _httpContextAccessor = httpContextAccessor;
        _siteContext = siteContext;
    }

    public string? UserId => _httpContextAccessor.HttpContext?.User?.Identity?.Name ?? "system";

    // Resolved automatically by middleware from the HTTP Host header - no hardcoding needed
    public int? SiteId => _siteContext.SiteId;
}
