namespace ef_core_essentials_blazor.Models;

/// <summary>
/// Marker interface for multi-tenancy. Any entity implementing this will automatically
/// receive a global query filter scoped to the current site in AppDbContext.
/// </summary>
public interface IHaveSiteId
{
    int SiteId { get; }
}
