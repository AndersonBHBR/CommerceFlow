using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Inventory.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InventoryDbContext))]
public sealed class InventoryDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("inventory")
            .HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Inventory.Infrastructure.Persistence.Messaging.InboxMessage", entity =>
        {
            entity.Property<Guid>("MessageId").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("ProcessedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("ReceivedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Type").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.HasKey("MessageId");
            entity.HasIndex("ProcessedAtUtc");
            entity.ToTable("InboxMessages", "inventory");
        });

        modelBuilder.Entity("Inventory.Infrastructure.Persistence.Messaging.OutboxMessage", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<int>("Attempts").HasColumnType("int");
            entity.Property<string>("CorrelationId").IsRequired().HasMaxLength(128).HasColumnType("nvarchar(128)");
            entity.Property<string>("LastError").HasMaxLength(2000).HasColumnType("nvarchar(2000)");
            entity.Property<DateTimeOffset>("NextAttemptAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("OccurredAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Payload").IsRequired().HasColumnType("nvarchar(max)");
            entity.Property<DateTimeOffset?>("PublishedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("RoutingKey").IsRequired().HasMaxLength(200).HasColumnType("nvarchar(200)");
            entity.Property<string>("Type").IsRequired().HasMaxLength(500).HasColumnType("nvarchar(500)");
            entity.HasKey("Id");
            entity.HasIndex("PublishedAtUtc", "NextAttemptAtUtc");
            entity.ToTable("OutboxMessages", "inventory");
        });

        modelBuilder.Entity("Inventory.Domain.Products.Product", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");

            entity.Property<int>("AvailableQuantity")
                .HasColumnType("int");

            entity.Property<bool>("IsActive")
                .HasColumnType("bit");

            entity.Property<string>("Name")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            entity.Property<int>("ReservedQuantity")
                .HasColumnType("int");

            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            entity.Property<string>("Sku")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("nvarchar(64)");

            entity.HasKey("Id");
            entity.HasIndex("Sku").IsUnique();

            entity.ToTable("Products", "inventory", table =>
            {
                table.HasCheckConstraint(
                    "CK_Products_AvailableQuantity_NonNegative",
                    "[AvailableQuantity] >= 0");
                table.HasCheckConstraint(
                    "CK_Products_QuantityConsistency",
                    "[AvailableQuantity] >= [ReservedQuantity]");
                table.HasCheckConstraint(
                    "CK_Products_ReservedQuantity_NonNegative",
                    "[ReservedQuantity] >= 0");
            });
        });

        modelBuilder.Entity("Inventory.Domain.Stock.StockAdjustment", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");

            entity.Property<string>("ExternalReference")
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            entity.Property<DateTimeOffset>("OccurredAtUtc")
                .HasColumnType("datetimeoffset");

            entity.Property<string>("PerformedBy")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            entity.Property<Guid>("ProductId")
                .HasColumnType("uniqueidentifier");

            entity.Property<int>("Quantity")
                .HasColumnType("int");

            entity.Property<string>("Reason")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            entity.HasKey("Id");
            entity.HasIndex("ProductId", "OccurredAtUtc");

            entity.ToTable("StockAdjustments", "inventory", table =>
                table.HasCheckConstraint("CK_StockAdjustments_Quantity_NonZero", "[Quantity] <> 0"));
        });

        modelBuilder.Entity("Inventory.Domain.Stock.StockReservation", entity =>
        {
            entity.Property<Guid>("Id").ValueGeneratedNever().HasColumnType("uniqueidentifier");
            entity.Property<Guid>("OrderId").HasColumnType("uniqueidentifier");
            entity.Property<Guid>("ProductId").HasColumnType("uniqueidentifier");
            entity.Property<int>("Quantity").HasColumnType("int");
            entity.Property<DateTimeOffset?>("ReleasedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("ReservedAtUtc").HasColumnType("datetimeoffset");
            entity.HasKey("Id");
            entity.HasIndex("OrderId", "ProductId").IsUnique();
            entity.HasIndex("OrderId", "ReleasedAtUtc");
            entity.HasIndex("ProductId");
            entity.ToTable("StockReservations", "inventory", table =>
                table.HasCheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0"));
        });

        modelBuilder.Entity("Inventory.Domain.Stock.StockAdjustment", entity =>
        {
            entity.HasOne("Inventory.Domain.Products.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });

        modelBuilder.Entity("Inventory.Domain.Stock.StockReservation", entity =>
        {
            entity.HasOne("Inventory.Domain.Products.Product", null)
                .WithMany()
                .HasForeignKey("ProductId")
                .OnDelete(DeleteBehavior.Restrict)
                .IsRequired();
        });
#pragma warning restore 612, 618
    }
}
