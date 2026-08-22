using Inventory.Domain.Stock;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Inventory.Infrastructure.Persistence.Configurations;

public sealed class StockReservationConfiguration : IEntityTypeConfiguration<StockReservation>
{
    public void Configure(EntityTypeBuilder<StockReservation> builder)
    {
        builder.ToTable("StockReservations", table =>
            table.HasCheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0"));
        builder.HasKey(reservation => reservation.Id);
        builder.Property(reservation => reservation.Id).ValueGeneratedNever();
        builder.HasIndex(reservation => new { reservation.OrderId, reservation.ProductId }).IsUnique();
        builder.HasIndex(reservation => new { reservation.OrderId, reservation.ReleasedAtUtc });
        builder.HasOne<Inventory.Domain.Products.Product>()
            .WithMany()
            .HasForeignKey(reservation => reservation.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
