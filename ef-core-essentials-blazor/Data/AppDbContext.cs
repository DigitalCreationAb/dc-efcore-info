using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using ef_core_essentials_blazor.Models;
using ef_core_essentials_blazor.Services;

namespace ef_core_essentials_blazor.Data;

public class AppDbContext : DbContext
{
    private readonly ICurrentUserService _currentUserService;

    public AppDbContext(DbContextOptions<AppDbContext> options, ICurrentUserService currentUserService) 
        : base(options)
    {
        _currentUserService = currentUserService;
    }

    public DbSet<Product> Products => Set<Product>();
    public DbSet<Category> Categories => Set<Category>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<Site> Sites => Set<Site>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Apply all configurations from assembly - automatically finds all IEntityTypeConfiguration<>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Auto-apply global query filters to any entity implementing ISoftDelete or IHaveSiteId.
        // Adding the interface to a new entity is all that's needed - no manual HasQueryFilter calls.
        foreach (var clrType in modelBuilder.Model.GetEntityTypes()
            .Where(t => !t.IsOwned())
            .Select(t => t.ClrType))
        {
            var filter = BuildFilter(clrType);
            if (filter is not null)
                modelBuilder.Entity(clrType).HasQueryFilter(filter);
        }

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Builds a combined query filter expression for a given entity type.
    /// ISoftDelete  → !e.IsDeleted
    /// IHaveSiteId  → siteId == null || e.SiteId == siteId
    /// Both         → combined with &&
    /// </summary>
    private LambdaExpression? BuildFilter(Type clrType)
    {
        var hasSoftDelete = typeof(ISoftDelete).IsAssignableFrom(clrType);
        var hasSiteId    = typeof(IHaveSiteId).IsAssignableFrom(clrType);

        if (!hasSoftDelete && !hasSiteId)
            return null;

        var param   = Expression.Parameter(clrType, "e");
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

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Automatic soft delete handling - intercepts deletes and marks as deleted instead
        foreach (var entry in ChangeTracker.Entries<ISoftDelete>())
        {
            if (entry.State == EntityState.Deleted)
            {
                entry.State = EntityState.Modified;
                entry.Entity.IsDeleted = true;
                entry.Entity.DeletedAt = DateTimeOffset.UtcNow;
            }
        }

        // Automatic audit trail - sets created/updated timestamps and user
        foreach (var entry in ChangeTracker.Entries<IAuditable>())
        {
            switch (entry.State)
            {
                case EntityState.Added:
                    entry.Entity.CreatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.CreatedBy = _currentUserService.UserId;
                    break;
                case EntityState.Modified:
                    entry.Entity.UpdatedAt = DateTimeOffset.UtcNow;
                    entry.Entity.UpdatedBy = _currentUserService.UserId;
                    break;
            }
        }

        return await base.SaveChangesAsync(cancellationToken);
    }
}
