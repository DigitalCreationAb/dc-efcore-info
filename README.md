# EF Core Essentials - Blazor .NET 10

A comprehensive demonstration of **Entity Framework Core best practices** for Blazor Server applications.

## Quick Start

```bash
dotnet run --project ef-core-essentials-blazor
```

The database is **automatically dropped and re-seeded on every DEBUG startup** - no manual migration steps needed during development.
In Release the initializer checks `Products.Any()` and skips if data already exists.

Navigate to `/products` to interact with all demos.

---

## Patterns demonstrated

| Pattern | Where |
|---|---|
| `.Select()` projection | `ProductService.GetProductsForListAsync` |
| `TagWith()` for query identification | Every query in `ProductService` |
| `AsSplitQuery` vs cartesian explosion | `ProductService.GetOrdersWithSplitQueryAsync` |
| `ToQueryString()` for SQL debugging | `ProductService.GetProductQuerySql` |
| `ExecuteUpdateAsync` / `ExecuteDeleteAsync` | `ProductService.IncreaseAllPricesAsync` |
| `AsAsyncEnumerable` live streaming | `ProductService.StreamProductsAsync` |
| Reusable `IQueryable<T>` extension methods | `QueryExtensions` |
| `ToPagingAsync<T>()` - reusable pagination | `QueryExtensions.ToPagingAsync` |
| JSON column filtering (OPENJSON / EXISTS) | `ProductService.FilterBySpecificationAsync` |
| Self-configuring value object (`[Owned]`) | `PriceWithCurrency` |
| Soft delete (automatic, interface-driven) | `ISoftDelete` + `AppDbContext.SaveChangesAsync` |
| Audit trail (automatic, interface-driven) | `IAuditable` + `AppDbContext.SaveChangesAsync` |
| Multi-tenancy (automatic, interface-driven) | `IHaveSiteId` + `AppDbContext.OnModelCreating` |

---

## Project structure

```
/Data
  /Configurations        - IEntityTypeConfiguration<T> implementations
  AppDbContext.cs        - DbContext with automatic soft delete / audit / site filters
  DbInitializer.cs       - Seed data (auto-reset in DEBUG)

/Models
  /ValueObjects          - PriceWithCurrency ([Owned] self-configuring value object)
  Product.cs             - ISoftDelete, IAuditable, IHaveSiteId
  Order.cs               - ISoftDelete, IAuditable, IHaveSiteId
  Category.cs            - Self-referencing hierarchy
  Site.cs                - Host-based multi-tenancy
  ISoftDelete.cs         - Marker interface -> automatic soft delete filter
  IAuditable.cs          - Marker interface -> automatic timestamp/user tracking
  IHaveSiteId.cs         - Marker interface -> automatic site isolation filter

/Services
  ICurrentUserService.cs - Reads SiteId from SiteContext
  SiteContext.cs         - Scoped POCO populated by middleware
  ProductService.cs      - All EF Core best-practice patterns

/Extensions
  QueryExtensions.cs     - WhereActive, ForCurrentSite, ToPagingAsync<T>, PagedResult<T>

/Components/Pages
  Products.razor         - Interactive demo page
```

---

## Global Query Filters - the right way

### The naive approach - one `HasQueryFilter` per entity

The most common way you will see global query filters is a `HasQueryFilter` call per entity in `OnModelCreating`:

```csharp
// Works, but does not scale and is easy to forget on new entities
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Product>().HasQueryFilter(p =>
        !p.IsDeleted && p.SiteId == _currentUserService.SiteId);

    modelBuilder.Entity<Order>().HasQueryFilter(o =>
        !o.IsDeleted && o.SiteId == _currentUserService.SiteId);

    // Every new entity = another manual call here.
    // Forget one -> deleted rows appear or cross-tenant data leaks silently.
}
```

Problems:

- Every developer adding a new entity must remember to wire the filter manually.
- Nothing in the compiler enforces it - a forgotten entity silently returns wrong data.
- The filter logic is duplicated across every entity.

---

### The better approach - interface as a contract

Use **interfaces as contracts**. Any entity that must be site-scoped implements `IHaveSiteId`.
`OnModelCreating` walks all entity types once and applies the appropriate filter automatically - no per-entity setup ever needed.

**Step 1 - define the interfaces**

```csharp
// Models/ISoftDelete.cs
public interface ISoftDelete
{
    bool IsDeleted { get; set; }
    DateTimeOffset? DeletedAt { get; set; }
}

// Models/IHaveSiteId.cs
// Implementing this interface is ALL that is needed to get automatic site isolation.
// No trip to AppDbContext required.
public interface IHaveSiteId
{
    int SiteId { get; }
}
```

**Step 2 - implement the interface on each entity**

```csharp
// Adding IHaveSiteId wires the site filter automatically - no AppDbContext changes.
public class Product : ISoftDelete, IAuditable, IHaveSiteId
{
    public int SiteId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    // ...
}

public class Order : ISoftDelete, IAuditable, IHaveSiteId
{
    public int SiteId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    // ...
}
```

**Step 3 - one loop in `OnModelCreating` handles everything**

```csharp
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

    // Walk every non-owned entity type and build its filter once.
    // New entities pick this up automatically just by implementing the interface.
    foreach (var clrType in modelBuilder.Model.GetEntityTypes()
        .Where(t => !t.IsOwned())
        .Select(t => t.ClrType))
    {
        var filter = BuildFilter(clrType);
        if (filter is not null)
            modelBuilder.Entity(clrType).HasQueryFilter(filter);
    }
}

// Builds a combined lambda: (!e.IsDeleted) && (siteId == null || e.SiteId == siteId)
// Only includes the parts relevant for each type.
private LambdaExpression? BuildFilter(Type clrType)
{
    var hasSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);
    var hasSiteId     = typeof(IHaveSiteId).IsAssignableFrom(clrType);

    if (!hasSoftDelete && !hasSiteId)
        return null;

    var param  = Expression.Parameter(clrType, "e");
    Expression? filter = null;

    if (hasSoftDelete)
    {
        // !e.IsDeleted
        filter = Expression.Not(Expression.Property(param, nameof(ISoftDelete.IsDeleted)));
    }

    if (hasSiteId)
    {
        // _currentUserService.SiteId == null || e.SiteId == _currentUserService.SiteId
        var service      = Expression.Constant(_currentUserService, typeof(ICurrentUserService));
        var currentSite  = Expression.Property(service, nameof(ICurrentUserService.SiteId));
        var entitySiteId = Expression.Convert(
                               Expression.Property(param, nameof(IHaveSiteId.SiteId)),
                               typeof(int?));

        var siteFilter = Expression.OrElse(
            Expression.Equal(currentSite, Expression.Constant(null, typeof(int?))),
            Expression.Equal(entitySiteId, currentSite));

        filter = filter is null ? siteFilter : Expression.AndAlso(filter, siteFilter);
    }

    return Expression.Lambda(filter!, param);
}
```

**Adding a new multi-tenant entity in the future:**

```csharp
// That's it. No changes to AppDbContext needed.
public class Invoice : ISoftDelete, IHaveSiteId
{
    public int SiteId { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    // ...
}
```

---

### Why the `siteId == null` guard matters

EF Core extracts closure values as query parameters **before** evaluating the expression tree.
Calling `.Value` on a null `int?` throws `InvalidOperationException: Nullable object must have a value`
even inside a `!HasValue ||` guard - because the parameter is pulled out first.

```csharp
// Throws at startup when SiteId is null (migrations, dotnet ef commands, tests)
p => p.SiteId == _currentUserService.SiteId!.Value

// Safe - null means "no site resolved yet, skip the filter"
p => _currentUserService.SiteId == null || p.SiteId == _currentUserService.SiteId
```

---

## Multi-tenancy via Host header

Each `Site` row has a `Host` column. Middleware in `Program.cs` reads the HTTP `Host` header,
queries the matching site, and stores it in a scoped `SiteContext`.
`ICurrentUserService.SiteId` reads from there.

```
Request: Host: store2.localhost
    -> middleware: SELECT * FROM Sites WHERE Host = 'store2.localhost'
    -> SiteContext.SiteId = 2
    -> every query on an IHaveSiteId entity gets WHERE SiteId = 2 appended automatically
```

To test locally, run a second launch profile pointing at `http://store2.localhost:PORT` -
the Products page will show only that store's products.

---

## Self-configuring value objects with `[Owned]`

`PriceWithCurrency` declares its own column names and constraints via data annotations.
Any entity with a `PriceWithCurrency` property picks up the configuration automatically -
no `OwnsOne` call needed anywhere.

```csharp
[Owned]
public record PriceWithCurrency
{
    [Column("Price")]
    [Precision(18, 2)]
    public decimal Amount { get; init; }

    [Column("Currency")]
    [MaxLength(3)]
    public string Currency { get; init; } = "SEK";
}
```

---

## Reusable pagination with `ToPagingAsync<T>()`

```csharp
var result = await _context.Products
    .Where(p => p.Stock > 0)
    .OrderBy(p => p.Name)
    .Select(p => new ProductListDto { ... })
    .ToPagingAsync(new PagingOptions { Page = 1, PageSize = 20 });

// result.Items       - the current page
// result.TotalCount  - total matching rows (for page controls)
// result.TotalPages  - calculated automatically
```

---

## `AsAsyncEnumerable` vs `ToListAsync`

| | `ToListAsync` | `AsAsyncEnumerable` |
|---|---|---|
| **When data arrives** | All rows buffered, then returned | Each row yielded as it arrives |
| **Memory** | Full result set in RAM | Constant - one row at a time |
| **First row latency** | High (waits for all rows) | Low (immediate) |
| **Cancellation** | Cancels the whole query | Propagates into the DB cursor via `.WithCancellation()` |
| **Best for** | Normal queries | Exports, reports, ETL, live dashboards |

```csharp
// [EnumeratorCancellation] propagates the token into .WithCancellation() on the DB cursor.
public async IAsyncEnumerable<ProductListDto> StreamProductsAsync(
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
{
    await foreach (var product in _context.Products
        .TagWith("StreamProducts - ProductService")
        .OrderBy(p => p.Id)
        .Select(p => new ProductListDto { ... })
        .AsAsyncEnumerable()
        .WithCancellation(cancellationToken))
    {
        yield return product;
    }
}
```

The Products page streaming demo lets you watch rows arrive live and cancel mid-stream -
the DB cursor aborts immediately.

---

## JSON column filtering

`Product.Metadata` is stored as a JSON column via `ToJson()`.
`Specifications` is a `List<Specification>` configured with `OwnsMany`, which lets EF Core
translate `.Any()` to a server-side `EXISTS + OPENJSON` - no client-side evaluation.

```csharp
// Translates to: WHERE EXISTS (SELECT 1 FROM OPENJSON(Metadata, '$.Specifications')
//                              WHERE Key = @key AND Value = @value)
var products = await _context.Products
    .Where(p => p.Metadata != null &&
                p.Metadata.Specifications.Any(s => s.Key == specKey && s.Value == specValue))
    .ToListAsync();
```

Use the **Show JSON Query SQL** button on the Products page to see the generated SQL.

---

## Always use `.Select()` - never load what you do not need

```csharp
// GOOD - only the 5 columns needed reach the application
var products = await context.Products
    .Select(p => new ProductListDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price.Amount,
        Currency = p.Price.Currency,
        CategoryName = p.Category.Name
        // Metadata JSON column NOT included - stays in the DB
    })
    .ToListAsync();

// BAD - loads every column including the potentially-large Metadata JSON blob
var products = await context.Products.ToListAsync();
```

---

## Extension methods over Repository pattern

`DbContext` already IS the repository and unit of work.
Use `IQueryable<T>` extension methods for reusable query logic instead.

```csharp
// Extensions/QueryExtensions.cs
public static IQueryable<Product> WhereActive(this IQueryable<Product> query)
    => query.Where(p => p.Stock > 0 && !p.IsDeleted);

public static IQueryable<Product> ForCurrentSite(this IQueryable<Product> query, int? siteId)
    => siteId.HasValue ? query.Where(p => p.SiteId == siteId.Value) : query;

// Still composable, still IQueryable, still server-side SQL
var products = await context.Products
    .WhereActive()
    .ForCurrentSite(siteId)
    .Select(p => new ProductListDto { ... })
    .ToListAsync();
```

---

## Soft delete

```csharp
// SaveChangesAsync intercepts Remove() and sets IsDeleted instead of deleting the row
context.Products.Remove(product);
await context.SaveChangesAsync(); // -> UPDATE Products SET IsDeleted=1, DeletedAt=...

// Global query filter hides deleted rows automatically - no manual WHERE IsDeleted=0 needed

// Access deleted items when needed
var deleted = await context.Products
    .IgnoreQueryFilters()
    .Where(p => p.IsDeleted)
    .ToListAsync();
```

> **Warning:** `ExecuteUpdateAsync`/`ExecuteDeleteAsync` bypass change tracking and therefore bypass
> soft delete. Use them only for bulk operations where hard deletes are intentional.

---

## Migrations reference

```bash
dotnet ef migrations add MigrationName
dotnet ef database update
dotnet ef database update PreviousMigrationName   # rollback
dotnet ef migrations script                        # generate SQL
dotnet ef migrations remove                        # remove last (if not applied)
```

---

## Learn more

- [EF Core documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Owned Entity Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [JSON Columns](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)
- [Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
