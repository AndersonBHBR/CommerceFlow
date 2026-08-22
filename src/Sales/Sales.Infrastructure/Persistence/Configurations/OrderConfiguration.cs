using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Sales.Domain.Orders;

namespace Sales.Infrastructure.Persistence.Configurations;

public sealed class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.ToTable("Orders", table =>
        {
            table.HasCheckConstraint("CK_Orders_Status", "[Status] BETWEEN 1 AND 4");
            table.HasCheckConstraint("CK_Orders_TotalAmount_Positive", "[TotalAmount] > 0");
        });
        builder.HasKey(order => order.Id);
        builder.Property(order => order.Id).ValueGeneratedNever();

        builder.Property(order => order.Number)
            .HasMaxLength(40)
            .IsRequired();
        builder.HasIndex(order => order.Number).IsUnique();

        builder.Property(order => order.CustomerId).IsRequired();
        builder.Property(order => order.ExternalReference)
            .HasMaxLength(100)
            .IsRequired();
        builder.HasIndex(order => order.ExternalReference).IsUnique();

        builder.Property(order => order.Status).IsRequired();
        builder.Property(order => order.StatusReason).HasMaxLength(500);
        builder.Property(order => order.TotalAmount)
            .HasPrecision(18, 2)
            .IsRequired();
        builder.Property(order => order.CreatedBy)
            .HasMaxLength(200)
            .IsRequired();
        builder.Property(order => order.CreatedAtUtc).IsRequired();
        builder.Property(order => order.CancelledBy).HasMaxLength(200);
        builder.Property(order => order.CancelledAtUtc);
        builder.Property(order => order.RowVersion)
            .IsRowVersion()
            .IsConcurrencyToken();

        builder.HasIndex(order => new { order.CustomerId, order.CreatedAtUtc });
        builder.HasIndex(order => new { order.Status, order.CreatedAtUtc });

        builder.HasMany(order => order.Items)
            .WithOne()
            .HasForeignKey(item => item.OrderId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.Navigation(order => order.Items)
            .UsePropertyAccessMode(PropertyAccessMode.Field);
    }
}
