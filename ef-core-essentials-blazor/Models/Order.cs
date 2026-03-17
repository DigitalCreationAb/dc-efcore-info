namespace ef_core_essentials_blazor.Models;

public class Order : ISoftDelete, IAuditable
{
    public int Id { get; set; }
    public string OrderNumber { get; set; } = string.Empty;
    public DateTimeOffset OrderDate { get; set; }
    public OrderStatus Status { get; set; }
    
    // Multi-tenancy
    public int SiteId { get; set; }
    public Site Site { get; set; } = null!;
    
    // Owned entity for address
    public Address ShippingAddress { get; set; } = new();
    
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

public enum OrderStatus
{
    Pending,
    Processing,
    Shipped,
    Delivered,
    Cancelled
}

/// <summary>
/// Owned entity - stored in same table
/// </summary>
public class Address
{
    public string Street { get; set; } = string.Empty;
    public string City { get; set; } = string.Empty;
    public string PostalCode { get; set; } = string.Empty;
    public string Country { get; set; } = string.Empty;
}
