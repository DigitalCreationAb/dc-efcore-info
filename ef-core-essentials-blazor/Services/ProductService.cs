using Microsoft.EntityFrameworkCore;
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
        var query = _context.Products
            .TagWith("GetProductsPaginated - ProductService")
            .Where(p => p.Stock > 0)
            .OrderBy(p => p.Name);

        var totalCount = await query.CountAsync();

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new ProductListDto
            {
                Id = p.Id,
                Name = p.Name,
                Price = p.Price.Amount,
                Currency = p.Price.Currency,
                CategoryName = p.Category.Name
            })
            .ToListAsync();

        return new PagedResult<ProductListDto>
        {
            Items = items,
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        };
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
    /// AsyncEnumerable - stream results for very large datasets
    /// Useful when processing millions of rows without loading all into memory
    /// </summary>
    public async IAsyncEnumerable<Product> StreamAllProductsAsync()
    {
        await foreach (var product in _context.Products
            .TagWith("StreamAllProducts - ProductService")
            .AsAsyncEnumerable())
        {
            yield return product;
        }
    }

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

public class PagedResult<T>
{
    public List<T> Items { get; set; } = new();
    public int TotalCount { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
}
