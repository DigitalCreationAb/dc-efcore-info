namespace ef_core_essentials_blazor.Models;

/// <summary>
/// Multi-tenancy site entity
/// </summary>
public class Site
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// HTTP Host header value used to auto-resolve this site (e.g. "store1.localhost").
    /// Matched by middleware on every request to set SiteContext.
    /// </summary>
    public string? Host { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<Product> Products { get; set; } = new List<Product>();
    public ICollection<Order> Orders { get; set; } = new List<Order>();
}
