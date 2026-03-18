# JSON Column Filtering Example - ProductMetadata.Specifications

## Overview
This example demonstrates how to filter products using JSON columns in EF Core. The `ProductMetadata.Specifications` dictionary is stored as a JSON column in the database and can be queried efficiently using EF Core's JSON support.

## What Was Added

### 1. ProductService Methods

#### `FilterBySpecificationAsync(string specKey, string specValue)`
Filters products by a specific key-value pair in the JSON Specifications dictionary.

```csharp
var products = await ProductService.FilterBySpecificationAsync("Color", "Red");
```

**Generated SQL:**
```sql
SELECT ... FROM Products
WHERE JSON_VALUE([Metadata], '$.Specifications.Color') = 'Red'
```

#### `FilterByBrandAsync(string brand)`
Filters products by the Brand property in the JSON Metadata column.

```csharp
var products = await ProductService.FilterByBrandAsync("Apple");
```

#### `GetFilterBySpecificationQuerySql(string specKey, string specValue)`
Returns the generated SQL for debugging purposes without executing the query.

### 2. Products.razor Page Updates

Added a new JSON filtering section with:
- Input fields for specification key and value
- Buttons to filter by specification or brand
- Button to view generated SQL
- Table displaying filtered products with full metadata (Brand, Specifications, Tags)

### 3. Enhanced Sample Data

Updated `DbInitializer.cs` with products containing rich metadata:
- **Dell Laptop** - Color: Silver, Storage: 512GB SSD
- **iPhone 15 Pro** - Color: Red, Storage: 512GB, Brand: Apple
- **MacBook Pro** - Color: Space Gray, Storage: 1TB SSD, Brand: Apple
- **Gaming Mouse** - Color: Red, DPI: 25600, Brand: Logitech

## How to Use

1. **Run the application** and navigate to the Products page
2. **Enter a specification key and value**:
   - Key: `Color`, Value: `Red` (finds iPhone and Gaming Mouse)
   - Key: `Storage`, Value: `512GB` (finds Dell Laptop and iPhone)
   - Key: `Brand`, Value: `Apple` (use the "Filter by Brand" button)
3. **View generated SQL** by clicking "Show JSON Query SQL"
4. **Observe the results** in the filtered products table with metadata

## Key Points

### ✅ EF Core JSON Support
- Works with SQL Server, PostgreSQL, and SQLite
- Translates dictionary queries to `JSON_VALUE()` or equivalent
- Supports nested property access

### ✅ Configuration Required
```csharp
// In ProductConfiguration.cs
builder.OwnsOne(p => p.Metadata, metadataBuilder =>
{
    metadataBuilder.ToJson("Metadata");
    
    // Explicitly ignore collections to prevent navigation property discovery
    metadataBuilder.Ignore(m => m.Specifications);
    metadataBuilder.Ignore(m => m.Tags);
});
```

### ⚠️ Performance Considerations
- JSON column queries may not use indexes as efficiently as regular columns
- Always use `.Select()` to project only needed fields when not filtering
- Consider extracting frequently-queried properties to regular columns

### ⚠️ Limitations
- Dictionary queries are translated but may have database-specific limitations
- Complex LINQ expressions on JSON may not translate to SQL
- Ordering by JSON properties has limited support

## Example Queries

```csharp
// Filter by specification
var redProducts = await context.Products
    .Where(p => p.Metadata != null && 
               p.Metadata.Specifications.ContainsKey("Color") &&
               p.Metadata.Specifications["Color"] == "Red")
    .ToListAsync();

// Filter by brand
var appleProducts = await context.Products
    .Where(p => p.Metadata != null && p.Metadata.Brand == "Apple")
    .ToListAsync();

// Project specific metadata fields
var productData = await context.Products
    .Select(p => new 
    { 
        p.Name, 
        Brand = p.Metadata!.Brand,
        Color = p.Metadata.Specifications["Color"]
    })
    .ToListAsync();
```

## Testing

To reset the database and reseed with new data:
1. Delete the existing database
2. Run a new migration or let `EnsureCreated()` recreate it
3. The new sample products will be added automatically

## Resources
- [EF Core JSON Columns Documentation](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-7.0/whatsnew#json-columns)
- [JSON Mapping in EF Core](https://learn.microsoft.com/en-us/ef/core/modeling/json)
