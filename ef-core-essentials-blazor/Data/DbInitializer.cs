using ef_core_essentials_blazor.Models;
using ef_core_essentials_blazor.Models.ValueObjects;

namespace ef_core_essentials_blazor.Data;

public static class DbInitializer
{
    public static void Initialize(AppDbContext context)
    {
        // Check if already seeded
        if (context.Products.Any())
        {
            return;
        }

        // Seed Sites - each has a Host that the middleware matches against the HTTP Host header.
        // In dev, run a second profile on a different port to test store2.localhost isolation.
        var site = new Site
        {
            Name = "Main Store",
            Code = "MAIN",
            Host = "localhost",
            IsActive = true
        };
        var site2 = new Site
        {
            Name = "Second Store",
            Code = "SECOND",
            Host = "store2.localhost",
            IsActive = true
        };
        context.Sites.AddRange(site, site2);
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
                    Specifications =
                    [
                        new() { Key = "CPU", Value = "Intel i7" },
                        new() { Key = "RAM", Value = "16GB" },
                        new() { Key = "Storage", Value = "512GB SSD" },
                        new() { Key = "Color", Value = "Silver" }
                    ],
                    Tags = ["laptop", "computer", "portable"]
                }
            },
            new Product
            {
                Name = "iPhone 15 Pro",
                Description = "Latest iPhone with advanced features",
                Price = new PriceWithCurrency(12999, "SEK"),
                Stock = 15,
                SiteId = site.Id,
                CategoryId = electronics.Id,
                Metadata = new ProductMetadata
                {
                    Brand = "Apple",
                    Manufacturer = "Apple Inc.",
                    Specifications =
                    [
                        new() { Key = "Storage", Value = "512GB" },
                        new() { Key = "Color", Value = "Red" },
                        new() { Key = "Screen", Value = "6.1 inch" },
                        new() { Key = "5G", Value = "Yes" }
                    ],
                    Tags = ["smartphone", "premium", "5g"]
                }
            },
            new Product
            {
                Name = "MacBook Pro",
                Description = "Professional laptop for creators",
                Price = new PriceWithCurrency(24999, "SEK"),
                Stock = 8,
                SiteId = site.Id,
                CategoryId = computers.Id,
                Metadata = new ProductMetadata
                {
                    Brand = "Apple",
                    Manufacturer = "Apple Inc.",
                    Specifications =
                    [
                        new() { Key = "CPU", Value = "M3 Pro" },
                        new() { Key = "RAM", Value = "32GB" },
                        new() { Key = "Storage", Value = "1TB SSD" },
                        new() { Key = "Color", Value = "Space Gray" },
                        new() { Key = "Screen", Value = "14 inch" }
                    ],
                    Tags = ["laptop", "professional", "creator"]
                }
            },
            new Product
            {
                Name = "Gaming Mouse",
                Description = "RGB wireless gaming mouse",
                Price = new PriceWithCurrency(899, "SEK"),
                Stock = 30,
                SiteId = site.Id,
                CategoryId = computers.Id,
                Metadata = new ProductMetadata
                {
                    Brand = "Logitech",
                    Manufacturer = "Logitech International",
                    Specifications =
                    [
                        new() { Key = "DPI", Value = "25600" },
                        new() { Key = "Color", Value = "Red" },
                        new() { Key = "Wireless", Value = "Yes" },
                        new() { Key = "RGB", Value = "Yes" }
                    ],
                    Tags = ["gaming", "mouse", "rgb"]
                }
            },
            new Product
            {
                Name = "Mouse",
                Description = "Standard wireless mouse",
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
            },
            // Second store products - only visible when Host header matches "store2.localhost"
            new Product
            {
                Name = "Monitor",
                Description = "4K display",
                Price = new PriceWithCurrency(5999, "SEK"),
                Stock = 12,
                SiteId = site2.Id,
                CategoryId = electronics.Id,
                Metadata = new ProductMetadata
                {
                    Brand = "Samsung",
                    Manufacturer = "Samsung Electronics",
                    Specifications =
                    [
                        new() { Key = "Resolution", Value = "4K" },
                        new() { Key = "Size", Value = "27 inch" },
                        new() { Key = "RefreshRate", Value = "144Hz" }
                    ],
                    Tags = ["monitor", "4k", "gaming"]
                }
            },
            new Product
            {
                Name = "Headphones",
                Description = "Noise-cancelling headphones",
                Price = new PriceWithCurrency(2499, "SEK"),
                Stock = 20,
                SiteId = site2.Id,
                CategoryId = electronics.Id
            }
        };

        context.Products.AddRange(products);
        context.SaveChanges();

        // Bulk products so AsAsyncEnumerable streaming demo has enough rows to show
        var bulkProducts = Enumerable.Range(1, 5500).Select(i => new Product
        {
            Name        = $"Bulk Product {i:D3}",
            Description = "Auto-generated for streaming demo",
            Price       = new PriceWithCurrency(100 + i * 15, "SEK"),
            Stock       = i % 7 == 0 ? 0 : 5 + (i % 20),
            SiteId      = site.Id,
            CategoryId  = i % 2 == 0 ? electronics.Id : computers.Id
        });

        context.Products.AddRange(bulkProducts);
        context.SaveChanges();
    }
}
