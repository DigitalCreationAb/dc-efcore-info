namespace ef_core_essentials_blazor.Services;

/// <summary>
/// Scoped container populated by site-resolution middleware on each HTTP request.
/// Holds the resolved site for the current Blazor circuit / request.
/// </summary>
public class SiteContext
{
    public int? SiteId { get; set; }
    public string? SiteName { get; set; }

    /// <summary>The Host header value that was used to resolve this site.</summary>
    public string? ResolvedFromHost { get; set; }

    public bool IsResolved => SiteId.HasValue;
}
