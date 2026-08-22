using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderItemConfiguration : IEntityTypeConfiguration<OrderItem>
{
    public void Configure(EntityTypeBuilder<OrderItem> builder)
    {
        builder.ToTable("OrderItems", table =>
        {
            table.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
            table.HasCheckConstraint("CK_OrderItems_UnitPrice_Positive", "[UnitPrice] > 0");
            table.HasCheckConstraint("CK_OrderItems_TotalAmount_Positive", "[TotalAmount] > 0");
        });
        builder.HasKey(item => item.Id);
        builder.Property(item => item.Id).ValueGeneratedNever();
        builder.Property(item => item.OrderId).IsRequired();
        builder.Property(item => item.ProductId).IsRequired();
        builder.Property(item => item.Sku)
            .HasMaxLength(64)
            .IsRequired();
        builder.Property(item => item.ProductName)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(item => item.Quantity).IsRequired();
        builder.Property(item => item.UnitPrice)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(item => item.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();

        builder.HasIndex(item => item.ProductId);
        builder.HasIndex(item => new { item.OrderId, item.ProductId }).IsUnique();
    }
}
