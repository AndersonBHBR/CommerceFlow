using Inventory.Domain.Products;
using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockAdjustmentConfiguration : IEntityTypeConfiguration<StockAdjustment>
{
    public void Configure(EntityTypeBuilder<StockAdjustment> builder)
    {
        builder.ToTable("StockAdjustments", table =>
            table.HasCheckConstraint("CK_StockAdjustments_Quantity_NonZero", "[Quantity] <> 0"));
        builder.HasKey(adjustment => adjustment.Id);
        builder.Property(adjustment => adjustment.Id).ValueGeneratedNever();
        builder.Property(adjustment => adjustment.Quantity).IsRequired();
        builder.Property(adjustment => adjustment.Reason)
            .HasMaxLength(500)
            .IsRequired();
        builder.Property(adjustment => adjustment.PerformedBy)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(adjustment => adjustment.ExternalReference)
            .HasMaxLength(100);
        builder.Property(adjustment => adjustment.OccurredAtUtc).IsRequired();

        builder.HasIndex(adjustment => new { adjustment.ProductId, adjustment.OccurredAtUtc });
        builder.HasOne<Product>()
            .WithMany()
            .HasForeignKey(adjustment => adjustment.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
