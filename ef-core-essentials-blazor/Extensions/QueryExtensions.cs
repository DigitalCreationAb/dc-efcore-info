using ef_core_essentials_blazor.Models;

namespace ef_core_essentials_blazor.Extensions;

/// <summary>
/// Extension methods for reusable query logic
/// This is BETTER than Repository pattern with EF Core!
/// </summary>
public static class QueryExtensions
{
    /// <summary>
    /// Filter products that are in stock and not deleted
    /// </summary>
    public static IQueryable<Product> WhereActive(this IQueryable<Product> query)
    {
        return query.Where(p => p.Stock > 0 && !p.IsDeleted);
    }

    /// <summary>
    /// Multi-tenancy filter - automatically scope to current site
    /// This is the "Boråsväst" example - filter by Site without explicit WHERE
    /// </summary>
    public static IQueryable<Product> ForCurrentSite(this IQueryable<Product> query, int? siteId)
    {
        if (siteId.HasValue)
        {
            return query.Where(p => p.SiteId == siteId.Value);
        }
        return query;
    }

    /// <summary>
    /// Generic pagination extension
    /// </summary>
    public static IQueryable<T> Paginate<T>(this IQueryable<T> query, int page, int pageSize)
    {
        return query
            .Skip((page - 1) * pageSize)
            .Take(pageSize);
    }

    /// <summary>
    /// Search by name
    /// </summary>
    public static IQueryable<Product> SearchByName(this IQueryable<Product> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(p => p.Name.Contains(searchTerm));
    }
}
