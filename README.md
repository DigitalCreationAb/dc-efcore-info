# ?? EF Core Essentials - Blazor .NET 10

A comprehensive demonstration of **Entity Framework Core best practices** for Blazor Server applications.

## ?? Quick Start

### 1. Install EF Core NuGet Packages

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer
dotnet add package Microsoft.EntityFrameworkCore.Tools
dotnet add package Microsoft.EntityFrameworkCore.Design
```

### 2. Create Initial Migration

```bash
dotnet ef migrations add InitialCreate
```

### 3. Update Database

```bash
dotnet ef database update
```

### 4. Run the Application

```bash
dotnet run
```

Navigate to `/products` to see the demo in action!

---

## ?? What's Included

### ? Configuration Patterns
- **IEntityTypeConfiguration<>** - Separate entity configurations from DbContext
- **Multi-database support** - SQL Server, PostgreSQL, SQLite
- **Value Objects** - `PriceWithCurrency` for domain modeling
- **Owned Entities** - `Address` embedded in `Order`
- **JSON Columns** - Flexible `ProductMetadata`

### ? Query Best Practices
- **Always use .Select()** - Project only needed fields to avoid loading large JSON columns
- **IQueryable<>** - Understand when queries actually execute
- **ToQueryString()** - Debug generated SQL
- **TagWith()** - Identify query origins in logs
- **Pagination** - Efficient `Skip()`/`Take()` with total count

### ? Performance Patterns
- **AsSplitQuery vs AsSingleQuery** - Avoid cartesian explosion
- **AsyncEnumerable** - Stream millions of rows without memory overhead
- **ExecuteUpdateAsync/DeleteAsync** - Bulk operations without loading entities
- **Extension Methods** - Reusable query logic (NO Repository pattern!)

### ? Soft Delete & Auditing
- **ISoftDelete** - Automatic soft delete in `SaveChangesAsync()`
- **IAuditable** - Automatic `CreatedBy`/`UpdatedBy` tracking
- **Global Query Filters** - Automatically hide soft-deleted items
- **IgnoreQueryFilters()** - Access deleted items when needed

### ? Multi-Tenancy
- **Site-based filtering** - Automatic scoping by `SiteId`
- **Extension methods** - `.ForCurrentSite()` for consistent filtering
- **Token-based context** - Read from JWT claims in production

### ? Relations & Mappings
- **HasOne/HasMany** - Define relationships
- **Self-referencing** - Category hierarchy
- **OwnsOne** - Embedded entities like `Address`
- **Complex types** - Value objects like `PriceWithCurrency`

---

## ??? Project Structure

```
/Data
  /Configurations        - IEntityTypeConfiguration<> implementations
  AppDbContext.cs        - Main DbContext with automatic soft delete/audit
  DbInitializer.cs       - Seed data for testing

/Models
  /ValueObjects          - Custom types like PriceWithCurrency
  Product.cs             - Entity with JSON metadata, soft delete, audit
  Category.cs            - Self-referencing hierarchy
  Order.cs               - Owned entity (Address)
  OrderItem.cs           - Many-to-many relationship
  Site.cs                - Multi-tenancy support
  ISoftDelete.cs         - Soft delete interface
  IAuditable.cs          - Audit trail interface

/Services
  ICurrentUserService.cs - User context (reads from JWT in production)
  ProductService.cs      - Example service with ALL best practices

/Extensions
  QueryExtensions.cs     - Reusable query logic (BETTER than Repository!)

/Components/Pages
  Products.razor         - Interactive demo page
  Home.razor             - Landing page with documentation
```

---

## ?? Key Concepts Demonstrated

### 1. Always Use .Select()

```csharp
// ? GOOD - Project only needed data
var products = await context.Products
    .Select(p => new ProductListDto
    {
        Id = p.Id,
        Name = p.Name,
        Price = p.Price.Amount
    })
    .ToListAsync();

// ? BAD - Loads everything including large JSON columns
var products = await context.Products.ToListAsync();
```

### 2. Extension Methods Over Repository Pattern

```csharp
// ? GOOD - Extension methods for reusable logic
public static IQueryable<Product> WhereActive(this IQueryable<Product> query)
{
    return query.Where(p => p.Stock > 0 && !p.IsDeleted);
}

// Usage
var products = await context.Products
    .WhereActive()
    .ForCurrentSite(siteId)
    .ToListAsync();

// ? BAD - Don't wrap DbContext in Repository
// DbContext IS already a repository and unit of work!
```

### 3. AsSplitQuery for Multiple Collections

```csharp
// ? GOOD - Separate queries to avoid cartesian explosion
var orders = await context.Orders
    .Include(o => o.OrderItems)
    .Include(o => o.Site)
    .AsSplitQuery() // Multiple queries
    .ToListAsync();

// ?? WARNING: AsSplitQuery with paging can give inconsistent results
```

### 4. Automatic Soft Delete

```csharp
// In SaveChangesAsync() - automatic interception
foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
{
    if (entry.State == EntityState.Deleted)
    {
        entry.State = EntityState.Modified;
        entry.Entity.IsDeleted = true;
        entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
    }
}

// Usage - just call Remove(), soft delete is automatic!
context.Products.Remove(product);
await context.SaveChangesAsync(); // IsDeleted = true, not physically deleted
```

### 5. Bulk Operations

```csharp
// ? Efficient - No loading entities
await context.Products
    .Where(p => p.Stock > 0)
    .ExecuteUpdateAsync(setters => setters
        .SetProperty(p => p.Price.Amount, p => p.Price.Amount * 1.10m));

// ?? WARNING: Bypasses soft delete and audit tracking!
```

### 6. Pagination

```csharp
var query = context.Products
    .Where(p => p.Stock > 0)
    .OrderBy(p => p.Name);

var totalCount = await query.CountAsync();

var items = await query
    .Skip((page - 1) * pageSize)
    .Take(pageSize)
    .Select(p => new ProductListDto { ... })
    .ToListAsync();
```

### 7. JSON Columns

```csharp
// Configuration
builder.OwnsOne(p => p.Metadata, metadataBuilder =>
{
    metadataBuilder.ToJson("Metadata");
});

// ?? WARNING: Can become large - always use .Select() to avoid loading
```

---

## ??? Database Migrations

```bash
# Add a new migration
dotnet ef migrations add MigrationName

# Update database to latest migration
dotnet ef database update

# Rollback to specific migration
dotnet ef database update PreviousMigrationName

# Generate SQL script
dotnet ef migrations script

# Remove last migration (if not applied)
dotnet ef migrations remove
```

---

## ?? Configuration

### Connection Strings (appsettings.json)

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=(localdb)\\mssqllocaldb;Database=EFCoreEssentialsDB;Trusted_Connection=true;MultipleActiveResultSets=true;TrustServerCertificate=true"
  },
  "DatabaseProvider": "SqlServer"
}
```

### Switch Database Provider

Change `DatabaseProvider` in `appsettings.json`:
- `"SqlServer"` - Microsoft SQL Server
- `"PostgreSQL"` - PostgreSQL
- `"SQLite"` - SQLite

---

## ?? Important Notes

### DON'T Use Repository Pattern with EF Core
- DbContext IS already a repository and unit of work
- Adding a repository layer adds unnecessary abstraction
- Use extension methods for reusable query logic instead

### Always Use .Select()
- Prevents loading large JSON columns
- Reduces memory usage
- Improves query performance
- Projects only the data you need

### Soft Delete Considerations
- `ExecuteUpdateAsync`/`ExecuteDeleteAsync` bypass soft delete
- Use regular `Remove()` + `SaveChangesAsync()` for soft delete
- Use `IgnoreQueryFilters()` to access deleted items

### AsSplitQuery Warning
- Using with pagination can give inconsistent results
- Data might change between queries
- Consider `AsSingleQuery` for small datasets

### DateTime vs DateTimeOffset
- Use `DateTimeOffset` for timezone-aware dates
- Stores UTC offset with the date
- Better for multi-timezone applications

---

## ?? Learn More

- [EF Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [Query Performance Best Practices](https://learn.microsoft.com/en-us/ef/core/performance/)
- [Global Query Filters](https://learn.microsoft.com/en-us/ef/core/querying/filters)
- [Owned Entity Types](https://learn.microsoft.com/en-us/ef/core/modeling/owned-entities)
- [JSON Columns](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)

---

## ?? Example Queries

Check `Services/ProductService.cs` for comprehensive examples of:
- Pagination
- Soft delete
- Bulk operations
- AsSplitQuery
- ToQueryString()
- AsyncEnumerable
- Extension methods
- Multi-tenancy filtering

---

## ?? License

This is a demo project for educational purposes.

---

## ?? Contributing

This is a demonstration project showcasing EF Core best practices for .NET 10 Blazor applications.

---

**Built with ?? using .NET 10 and Entity Framework Core**
