using Inventory.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            table.HasCheckConstraint(
                "CK_Products_AvailableQuantity_NonNegative",
                "[AvailableQuantity] >= 0");
            table.HasCheckConstraint(
                "CK_Products_ReservedQuantity_NonNegative",
                "[ReservedQuantity] >= 0");
            table.HasCheckConstraint(
                "CK_Products_QuantityConsistency",
                "[AvailableQuantity] >= [ReservedQuantity]");
        });
        builder.HasKey(product => product.Id);
        builder.Property(product => product.Id).ValueGeneratedNever();

        builder.Property(product => product.Sku)
            .HasMaxLength(64)
            .IsRequired();
        builder.HasIndex(product => product.Sku).IsUnique();

        builder.Property(product => product.Name)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(product => product.IsActive).IsRequired();
        builder.Property(product => product.AvailableQuantity).IsRequired();
        builder.Property(product => product.ReservedQuantity).IsRequired();
        builder.Property(product => product.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.Ignore(product => product.FreeQuantity);
    }
}
