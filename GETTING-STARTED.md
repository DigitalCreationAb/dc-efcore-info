# ?? Getting Started - Step by Step

Follow these steps to get the EF Core demo running:

---

## Step 1: Install Required NuGet Packages

Open your terminal in the project directory and run:

```bash
dotnet add package Microsoft.EntityFrameworkCore.SqlServer --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Tools --version 10.0.0
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.0
```

**Optional:** For other database providers:

```bash
# PostgreSQL
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL --version 10.0.0

# SQLite
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.0
```

---

## Step 2: Verify Project Structure

Make sure all files have been created:

```
? /Data/AppDbContext.cs
? /Data/DbInitializer.cs
? /Data/Configurations/*.cs
? /Models/*.cs
? /Models/ValueObjects/PriceWithCurrency.cs
? /Services/ICurrentUserService.cs
? /Services/ProductService.cs
? /Extensions/QueryExtensions.cs
? /Components/Pages/Products.razor
? Program.cs (updated)
? appsettings.json (updated)
```

---

## Step 3: Build the Project

```bash
dotnet build
```

Fix any compilation errors before proceeding.

---

## Step 4: Create Database Migration

```bash
dotnet ef migrations add InitialCreate
```

This will create a `Migrations` folder with the initial migration files.

**What it does:**
- Analyzes your DbContext and entity configurations
- Generates migration code to create database schema
- Creates snapshot of current model

---

## Step 5: Apply Migration to Database

```bash
dotnet ef database update
```

**What it does:**
- Creates the database (if it doesn't exist)
- Runs the migration to create all tables
- Seeds initial test data via `DbInitializer`

**Expected tables:**
- Sites
- Categories
- Products
- Orders
- OrderItems

---

## Step 6: Run the Application

```bash
dotnet run
```

Or press **F5** in Visual Studio.

---

## Step 7: Test the Demo

1. **Navigate to home page** - `https://localhost:xxxx/`
   - See overview of features

2. **Go to Products page** - Click "View Products Demo" or navigate to `/products`
   - Click **"Load Products"** - See all products with .Select() projection
   - Click **"Load Paginated"** - See pagination in action
   - Click **"Show Generated SQL"** - See the actual SQL query EF Core generates

3. **Check the console output** - See SQL queries with `TagWith()` labels

---

## Step 8: Explore the Code

### Key Files to Study:

1. **Services/ProductService.cs**
   - See ALL best practices in action
   - Pagination, soft delete, bulk operations
   - ToQueryString(), AsSplitQuery, AsyncEnumerable

2. **Data/AppDbContext.cs**
   - Automatic soft delete in `SaveChangesAsync()`
   - Automatic audit trail
   - Configuration loading

3. **Data/Configurations/ProductConfiguration.cs**
   - IEntityTypeConfiguration example
   - Value object (PriceWithCurrency)
   - JSON column (Metadata)
   - Global query filters

4. **Extensions/QueryExtensions.cs**
   - Reusable query logic
   - Better than Repository pattern!

---

## Step 9: Experiment with Queries

Open `Products.razor` and try modifying queries:

### Example: Add Search Functionality

```csharp
private async Task SearchProducts(string searchTerm)
{
    products = await ProductService.GetProductsForListAsync()
        .Where(p => p.Name.Contains(searchTerm))
        .ToListAsync();
}
```

### Example: Add Sorting

```csharp
private async Task LoadProductsSorted(bool descending)
{
    var query = _context.Products
        .OrderBy(p => descending ? p.Price.Amount : -p.Price.Amount);
    
    products = await query.Select(...).ToListAsync();
}
```

---

## Step 10: View Generated SQL

In the console output, you'll see queries tagged with comments:

```sql
-- GetProductsForList - ProductService

SELECT [p].[Id], [p].[Name], [p].[Price], [p].[Currency], [c].[Name] AS [CategoryName]
FROM [Products] AS [p]
INNER JOIN [Categories] AS [c] ON [p].[CategoryId] = [c].[Id]
WHERE [p].[IsDeleted] = 0 AND [p].[Stock] > 0
```

---

## ?? What to Learn From This Demo

### 1. Always Use .Select()
- **Before:** Loading entire entities with large JSON columns
- **After:** Projecting only needed fields for performance

### 2. Extension Methods > Repository Pattern
- **Before:** Wrapping DbContext in repository classes
- **After:** Using extension methods for reusable query logic

### 3. Automatic Soft Delete
- **Before:** Manually checking IsDeleted everywhere
- **After:** Global query filters + SaveChanges interception

### 4. Multi-Tenancy
- **Before:** Adding WHERE SiteId = X to every query
- **After:** Extension method `.ForCurrentSite()` applies it consistently

### 5. Pagination
- **Before:** Loading all records with .ToList()
- **After:** Efficient Skip/Take with total count

### 6. Bulk Operations
- **Before:** Loading entities, modifying, saving
- **After:** ExecuteUpdateAsync for bulk updates

---

## ?? Troubleshooting

### "No database provider has been configured"
- Make sure you installed `Microsoft.EntityFrameworkCore.SqlServer`
- Check `appsettings.json` has `ConnectionStrings` section
- Verify `Program.cs` calls `AddDbContext`

### "Cannot connect to database"
- SQL Server LocalDB might not be installed
- Install SQL Server Express or use SQLite instead
- Change `DatabaseProvider` in `appsettings.json` to `"SQLite"`
- Update connection string: `"Data Source=efcore.db"`

### "The type or namespace name 'AppDbContext' could not be found"
- Run `dotnet build` to check for errors
- Make sure all files were created correctly
- Check namespace matches: `ef_core_essentials_blazor`

### Migration fails
- Delete `Migrations` folder
- Run `dotnet ef migrations add InitialCreate` again
- Make sure no syntax errors in entity configurations

---

## ?? Next Steps

1. **Add your own entity** - Create a Customer or Invoice entity
2. **Implement relationships** - Add Orders to Customer
3. **Try different queries** - Experiment with Include, ThenInclude, AsSplitQuery
4. **Add validation** - Use FluentValidation with your entities
5. **Implement authorization** - Add role-based filtering
6. **Add caching** - Use IMemoryCache for frequently accessed data

---

## ?? Advanced Topics to Explore

- **Change Tracking** - How EF Core tracks entity changes
- **AsNoTracking** - Read-only queries for better performance
- **Compiled Queries** - Pre-compile queries for reuse
- **Interceptors** - Custom logic before/after database operations
- **Value Converters** - Transform property values to/from database
- **Temporal Tables** - Built-in history tracking in SQL Server
- **Concurrency Tokens** - Optimistic concurrency control

---

**Happy coding! ??**
