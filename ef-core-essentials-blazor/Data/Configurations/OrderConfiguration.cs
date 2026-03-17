using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ef_core_essentials_blazor.Models;

namespace ef_core_essentials_blazor.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders");
        
        builder.HasKey(o => o.Id);
        
        builder.Property(o => o.OrderNumber)
            .IsRequired()
            .HasMaxLength(50);

        // Owned entity - stored in same table (default)
        builder.OwnsOne(o => o.ShippingAddress, addressBuilder =>
        {
            addressBuilder.Property(a => a.Street).HasColumnName("ShippingStreet");
            addressBuilder.Property(a => a.City).HasColumnName("ShippingCity");
            addressBuilder.Property(a => a.PostalCode).HasColumnName("ShippingPostalCode");
            addressBuilder.Property(a => a.Country).HasColumnName("ShippingCountry");
        });

        builder.HasOne(o => o.Site)
            .WithMany(s => s.Orders)
            .HasForeignKey(o => o.SiteId)
            .OnDelete(DeleteBehavior.Restrict);

        // Global query filter
        builder.HasQueryFilter(o => !o.IsDeleted);

        builder.HasIndex(o => o.OrderNumber).IsUnique();
        builder.HasIndex(o => o.SiteId);
    }
}
