using ef_core_essentials_blazor.Models;
using Microsoft.EntityFrameworkCore;

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
    /// Search by name
    /// </summary>
    public static IQueryable<Product> SearchByName(this IQueryable<Product> query, string? searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
            return query;

        return query.Where(p => p.Name.Contains(searchTerm));
    }

    /// <summary>
    /// Executes the query as a paginated result.
    /// Apply ordering and .Select() projection before calling this.
    /// </summary>
    public static async Task<PagedResult<T>> ToPagingAsync<T>(
        this IQueryable<T> query,
        PagingOptions options,
        CancellationToken cancellationToken = default)
    {
        var pageSize = Math.Min(options.PageSize, PagingOptions.MaxPageSize);

        var totalCount = await query.CountAsync(cancellationToken);

        var items = await query
            .Skip((options.Page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return new PagedResult<T>
        {
            Items = items,
            TotalCount = totalCount,
            Page = options.Page,
            PageSize = pageSize
        };
    }
}

public class PagingOptions
{
    public const int MaxPageSize = 100;

    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 20;
}

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
