using ef_core_essentials_blazor.Models;
using ef_core_essentials_blazor.Models.ValueObjects;

namespace ef_core_essentials_blazor.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        context.Database.EnsureCreated();

        // Check if already seeded
        if (context.Products.Any())
        {
            return;
        }

        // Seed Sites
        var site = new Site
        {
            Name = "Main Store",
            Code = "MAIN",
            IsActive = true
        };
        context.Sites.Add(site);
        context.SaveChanges();

        // Seed Categories
        var electronics = new Category { Name = "Electronics", Description = "Electronic products" };
        var computers = new Category { Name = "Computers", ParentCategory = electronics };
        context.Categories.AddRange(electronics, computers);
        context.SaveChanges();

        // Seed Products
        var products = new[]
        {
            new Product
            {
                Name = "Laptop",
                Description = "High-performance laptop",
                Price = new PriceWithCurrency(15000, "SEK"),
                Stock = 10,
                SiteId = site.Id,
                CategoryId = computers.Id,
                Metadata = new ProductMetadata
                {
                    Brand = "Dell",
                    Manufacturer = "Dell Inc.",
                    Specifications = new Dictionary<string, string>
                    {
                        { "CPU", "Intel i7" },
                        { "RAM", "16GB" },
                        { "Storage", "512GB SSD" }
                    },
                    Tags = new List<string> { "laptop", "computer", "portable" }
                }
            },
            new Product
            {
                Name = "Mouse",
                Description = "Wireless mouse",
                Price = new PriceWithCurrency(299, "SEK"),
                Stock = 50,
                SiteId = site.Id,
                CategoryId = computers.Id
            },
            new Product
            {
                Name = "Keyboard",
                Description = "Mechanical keyboard",
                Price = new PriceWithCurrency(899, "SEK"),
                Stock = 25,
                SiteId = site.Id,
                CategoryId = computers.Id
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();
    }
}
