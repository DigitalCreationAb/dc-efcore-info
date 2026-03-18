using ef_core_essentials_blazor.Models.ValueObjects;

namespace ef_core_essentials_blazor.Models;

public class Product : ISoftDelete, IAuditable, IHaveSiteId
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    // Custom value object - configured as owned type
    public PriceWithCurrency Price { get; set; } = new();
    
    public int Stock { get; set; }
    
    // Multi-tenancy
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
    
    // JSON column for flexible metadata
    public ProductMetadata? Metadata { get; set; }
    
    // Relations
    public int CategoryId { get; set; }
    public Category Category { get; set; } = null!;
    
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();

    // Soft delete
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }

    // Audit
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
}

/// <summary>
/// JSON metadata - watch out: this column can become large with .ToList()
/// Always use .Select() to project only needed fields
/// </summary>
public class ProductMetadata
{
    public string? Brand { get; set; }
    public string? Manufacturer { get; set; }
    public List<Specification> Specifications { get; set; } = new();
    public List<string> Tags { get; set; } = new();
}

/// <summary>
/// Key/value pair stored as a JSON array element within ProductMetadata.Specifications.
/// Using a class instead of Dictionary&lt;string,string&gt; lets EF Core map it as an owned
/// entity collection and translate queries to OPENJSON SQL.
/// </summary>
public class Specification
{
    public string Key { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
}
