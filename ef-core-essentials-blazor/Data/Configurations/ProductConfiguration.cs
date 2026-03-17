using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ef_core_essentials_blazor.Models;

namespace ef_core_essentials_blazor.Data.Configurations;

/// <summary>
/// IEntityTypeConfiguration for separating entity configuration from DbContext
/// </summary>
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products");
        
        builder.HasKey(p => p.Id);
        
        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);
        
        builder.Property(p => p.Description)
            .HasMaxLength(2000);

        // Complex type / Value object configuration
        builder.OwnsOne(p => p.Price, priceBuilder =>
        {
            priceBuilder.Property(p => p.Amount)
                .HasColumnName("Price")
                .HasPrecision(18, 2);
            
            priceBuilder.Property(p => p.Currency)
                .HasColumnName("Currency")
                .HasMaxLength(3);
        });

        // JSON column - useful for flexible metadata
        // WARNING: Can become large - always use .Select() to avoid loading unnecessary data
        builder.OwnsOne(p => p.Metadata, metadataBuilder =>
        {
            metadataBuilder.ToJson("Metadata");
        });

        // Relations
        builder.HasOne(p => p.Category)
            .WithMany(c => c.Products)
            .HasForeignKey(p => p.CategoryId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(p => p.Site)
            .WithMany(s => s.Products)
            .HasForeignKey(p => p.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter for soft delete
        builder.HasQueryFilter(p => !p.IsDeleted);

        // Indexes
        builder.HasIndex(p => p.SiteId);
        builder.HasIndex(p => p.CategoryId);
        builder.HasIndex(p => p.IsDeleted);
    }
}
