using Microsoft.EntityFrameworkCore;
using System.Runtime.CompilerServices;
using ef_core_essentials_blazor.Data;
using ef_core_essentials_blazor.Models;
using ef_core_essentials_blazor.Extensions;

namespace ef_core_essentials_blazor.Services;

/// <summary>
/// Example service showing EF Core best practices
/// DON'T use Repository pattern with EF Core - DbContext IS the repository/unit of work!
/// </summary>
public class ProductService
{
    private readonly AppDbContext _context;
    private readonly ICurrentUserService _currentUserService;

    public ProductService(AppDbContext context, ICurrentUserService currentUserService)
    {
        _context = context;
        _currentUserService = currentUserService;
    }

    /// <summary>
    /// BEST PRACTICE: Always use .Select() to project only needed data
    /// This avoids loading large JSON columns, navigation properties, etc.
    /// </summary>
    public async Task<List<ProductListDto>> GetProductsForListAsync()
    {
        // WithTags helps identify where queries come from in logs
        var query = _context.Products
            .TagWith("GetProductsForList - ProductService")
            .Include(p => p.Category)
            .Where(p => p.Stock > 0)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                CategoryName = p.Category.Name
                // Note: Not including Metadata JSON column - keeps query lean
            });

        // IQueryable hasn't executed yet!
        // Query executes when we call ToListAsync()
        var products = await query.ToListAsync();

        return products;
    }

    /// <summary>
    /// Pagination example - efficient for large datasets
    /// </summary>
    public async Task<PagedResult<ProductListDto>> GetProductsPaginatedAsync(int page, int pageSize)
    {
        return await _context.Products
            .TagWith("GetProductsPaginated - ProductService")
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Name)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                CategoryName = p.Category.Name
            })
            .ToPagingAsync(new PagingOptions { Page = page, PageSize = pageSize });
    }

    /// <summary>
    /// AsSplitQuery vs AsSingleQuery
    /// AsSplitQuery: Multiple queries (one per Include) - better for 1:many to avoid cartesian explosion
    /// AsSingleQuery: Single query with JOINs - can cause cartesian explosion with multiple collections
    /// NOTE: AsSplitQuery is NOT default - must be explicitly called or set globally
    /// WARNING: AsSplitQuery with paging can give inconsistent results if data changes between queries
    /// </summary>
    public async Task<List<OrderWithItemsDto>> GetOrdersWithSplitQueryAsync()
    {
        var orders = await _context.Orders
            .TagWith("GetOrdersWithSplitQuery - ProductService")
            .Include(o => o.OrderItems)
                .ThenInclude(oi => oi.Product)
            .AsSplitQuery() // Executes as separate queries to avoid cartesian explosion
            .Select(o => new OrderWithItemsDto
            {
                Id = o.Id,
                OrderNumber = o.OrderNumber,
                Items = o.OrderItems.Select(oi => new OrderItemDto
                {
                    ProductName = oi.Product.Name,
                    Quantity = oi.Quantity,
                    UnitPrice = oi.UnitPrice
                }).ToList()
            })
            .ToListAsync();

        return orders;
    }

    /// <summary>
    /// Extension method pattern for reusable query logic
    /// </summary>
    public async Task<List<ProductListDto>> GetActiveProductsAsync()
    {
        var products = await _context.Products
            .TagWith("GetActiveProducts - ProductService")
            .WhereActive() // Extension method
            .ForCurrentSite(_currentUserService.SiteId) // Multi-tenancy filter
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return products;
    }

    /// <summary>
    /// ToQueryString() - useful for debugging generated SQL
    /// </summary>
    public string GetProductQuerySql()
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => p.Stock > 0);

        // Returns the SQL that would be generated
        return query.ToQueryString();
    }

    /// <summary>
    /// AsAsyncEnumerable — streams rows one-by-one from the database.
    ///
    /// KEY DIFFERENCE vs ToListAsync:
    ///   ToListAsync      → waits until ALL rows are loaded into memory, THEN returns the full list
    ///   AsAsyncEnumerable → yields each row the moment it arrives from the DB — caller can act immediately
    ///
    /// Why this matters:
    ///   - Memory stays constant regardless of result size (no giant List allocation)
    ///   - First row arrives faster — no waiting for full resultset
    ///   - CancellationToken propagates into the DB read loop via [EnumeratorCancellation]
    ///
    /// Ideal for: CSV/Excel exports, ETL pipelines, large reports, real-time dashboards.
    /// Note: Projects to DTO — avoids loading JSON blobs or navigation properties unnecessarily.
    /// </summary>
    public async IAsyncEnumerable<ProductListDto> StreamProductsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var product in _context.Products
            .TagWith("StreamProducts - ProductService")
            .OrderBy(p => p.Id)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                CategoryName = p.Category.Name
            })
            .AsAsyncEnumerable()
            .WithCancellation(cancellationToken))
        {
            yield return product;
        }
    }

    /// <summary>
    /// Compiled queries — EF Core compiles the expression tree ONCE at startup and reuses it.
    ///
    /// WHEN TO USE: Hot paths called thousands of times per second (e.g. GET /products/{id}).
    ///
    /// HOW IT WORKS:
    ///   - static field = compiled once for the application lifetime (shared across instances)
    ///   - DbContext is passed per-call → global filters (site, soft-delete) still applied
    ///   - Additional parameters are extra typed lambda arguments
    ///
    /// TRADE-OFF: Query shape is fixed at compile time.
    ///   Avoid for queries with dynamic filter combinations (use regular LINQ there).
    /// </summary>
    private static readonly Func<AppDbContext, int, Task<ProductListDto?>> _compiledGetById =
        EF.CompileAsyncQuery((AppDbContext ctx, int id) =>
            ctx.Products
                .Where(p => p.Id == id)
                .Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price.Amount,
                    Currency = p.Price.Currency,
                    CategoryName = p.Category.Name
                })
                .FirstOrDefault());

    private static readonly Func<AppDbContext, int, IAsyncEnumerable<ProductListDto>> _compiledGetByCategory =
        EF.CompileAsyncQuery((AppDbContext ctx, int categoryId) =>
            ctx.Products
                .Where(p => p.CategoryId == categoryId && p.Stock > 0)
                .OrderBy(p => p.Name)
                .Select(p => new ProductListDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    Price = p.Price.Amount,
                    Currency = p.Price.Currency,
                    CategoryName = p.Category.Name
                }));

    /// <summary>
    /// Global filters (site isolation, soft delete) are applied at execution time
    /// even though the query is precompiled — the DbContext carries the filter state.
    /// </summary>
    public Task<ProductListDto?> GetProductByIdAsync(int id)
        => _compiledGetById(_context, id);

    public IAsyncEnumerable<ProductListDto> GetProductsByCategoryAsync(int categoryId)
        => _compiledGetByCategory(_context, categoryId);

    /// <summary>
    /// Bulk update using ExecuteUpdateAsync (EF Core 7+)
    /// More efficient than loading, modifying, and saving entities
    /// </summary>
    public async Task<int> IncreaseAllPricesAsync(decimal percentage)
    {
        var rowsAffected = await _context.Products
            .TagWith("IncreaseAllPrices - ProductService")
            .Where(p => p.Stock > 0)
            .ExecuteUpdateAsync(setters => setters
                .SetProperty(p => p.Price.Amount, p => p.Price.Amount * (1 + percentage / 100)));

        return rowsAffected;
    }

    /// <summary>
    /// Bulk delete using ExecuteDeleteAsync (EF Core 7+)
    /// WARNING: Bypasses soft delete! Use with caution
    /// </summary>
    public async Task<int> HardDeleteOutOfStockProductsAsync()
    {
        // This is a hard delete - bypasses ISoftDelete
        var rowsAffected = await _context.Products
            .IgnoreQueryFilters() // Need this to get already soft-deleted items
            .Where(p => p.Stock == 0 && p.IsDeleted)
            .ExecuteDeleteAsync();

        return rowsAffected;
    }

    /// <summary>
    /// Proper soft delete - goes through change tracking
    /// </summary>
    public async Task SoftDeleteProductAsync(int productId)
    {
        var product = await _context.Products
            .FirstOrDefaultAsync(p => p.Id == productId);

        if (product != null)
        {
            _context.Products.Remove(product);
            // SaveChangesAsync will intercept and set IsDeleted = true instead
            await _context.SaveChangesAsync();
        }
    }

    /// <summary>
    /// Access soft-deleted items using IgnoreQueryFilters
    /// </summary>
    public async Task<List<ProductListDto>> GetDeletedProductsAsync()
    {
        return await _context.Products
            .IgnoreQueryFilters() // Bypasses global query filters
            .Where(p => p.IsDeleted)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency
            })
            .ToListAsync();
    }

    /// <summary>
    /// Filter by JSON column - ProductMetadata.Specifications.
    /// Specifications is now a List&lt;Specification&gt; (owned entity collection) so EF Core
    /// translates .Any() to an EXISTS + OPENJSON query - no client-side evaluation needed.
    /// </summary>
    public async Task<List<ProductWithMetadataDto>> FilterBySpecificationAsync(string specKey, string specValue)
    {
        // Full server-side translation: generates EXISTS (SELECT 1 FROM OPENJSON(...))
        var products = await _context.Products
            .TagWith($"FilterBySpecification - {specKey}={specValue}")
            .Where(p => p.Metadata != null &&
                       p.Metadata.Specifications.Any(s => s.Key == specKey && s.Value == specValue))
            .AsNoTracking()
            .ToListAsync();

        return products.Select(p => new ProductWithMetadataDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price.Amount,
            Currency = p.Price.Currency,
            Brand = p.Metadata!.Brand,
            Manufacturer = p.Metadata.Manufacturer,
            Specifications = p.Metadata.Specifications,
            Tags = p.Metadata.Tags
        }).ToList();
    }

    /// <summary>
    /// Filter by JSON Brand property - server-side WHERE on a mapped JSON scalar property.
    /// </summary>
    public async Task<List<ProductWithMetadataDto>> FilterByBrandAsync(string brand)
    {
        var products = await _context.Products
            .TagWith($"FilterByBrand - {brand}")
            .Where(p => p.Metadata != null && p.Metadata.Brand == brand)
            .AsNoTracking()
            .ToListAsync();

        return products.Select(p => new ProductWithMetadataDto
        {
            Id = p.Id,
            Name = p.Name,
            Price = p.Price.Amount,
            Currency = p.Price.Currency,
            Brand = p.Metadata!.Brand,
            Manufacturer = p.Metadata.Manufacturer,
            Specifications = p.Metadata.Specifications,
            Tags = p.Metadata.Tags
        }).ToList();
    }

    /// <summary>
    /// Returns the actual SQL generated for the specification filter.
    /// With List&lt;Specification&gt; (owned entity collection) EF Core produces a real
    /// EXISTS + OPENJSON query - the full WHERE is server-side.
    /// </summary>
    public string GetFilterBySpecificationQuerySql(string specKey, string specValue)
    {
        var query = _context.Products
            .Where(p => p.Metadata != null &&
                       p.Metadata.Specifications.Any(s => s.Key == specKey && s.Value == specValue));

        return query.ToQueryString();
    }
}

// DTOs - Data Transfer Objects
public class ProductListDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductWithMetadataDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public string Currency { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public List<Specification> Specifications { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

public class OrderWithItemsDto
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public string ProductName { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
