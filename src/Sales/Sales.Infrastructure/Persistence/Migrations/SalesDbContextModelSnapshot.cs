using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

namespace Sales.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SalesDbContext))]
public sealed class SalesDbContextModelSnapshot : ModelSnapshot
{
    protected override void BuildModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder
            .HasDefaultSchema("sales")
            .HasAnnotation("ProductVersion", "10.0.11")
            .HasAnnotation("Relational:MaxIdentifierLength", 128);

        modelBuilder.Entity("Sales.Infrastructure.Persistence.Messaging.InboxMessage", entity =>
        {
            entity.Property<Guid>("MessageId")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");
            entity.Property<DateTimeOffset>("ProcessedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<DateTimeOffset>("ReceivedAtUtc").HasColumnType("datetimeoffset");
            entity.Property<string>("Type")
                .IsRequired()
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");
            entity.HasKey("MessageId");
            entity.HasIndex("ProcessedAtUtc");
            entity.ToTable("InboxMessages", "sales");
        });

        modelBuilder.Entity("Sales.Infrastructure.Persistence.Messaging.OutboxMessage", entity =>
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
            entity.ToTable("OutboxMessages", "sales");
        });

        modelBuilder.Entity("Sales.Domain.Orders.Order", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");

            entity.Property<DateTimeOffset?>("CancelledAtUtc")
                .HasColumnType("datetimeoffset");

            entity.Property<string>("CancelledBy")
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            entity.Property<DateTimeOffset>("CreatedAtUtc")
                .HasColumnType("datetimeoffset");

            entity.Property<string>("CreatedBy")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            entity.Property<Guid>("CustomerId")
                .HasColumnType("uniqueidentifier");

            entity.Property<string>("ExternalReference")
                .IsRequired()
                .HasMaxLength(100)
                .HasColumnType("nvarchar(100)");

            entity.Property<string>("Number")
                .IsRequired()
                .HasMaxLength(40)
                .HasColumnType("nvarchar(40)");

            entity.Property<byte[]>("RowVersion")
                .IsConcurrencyToken()
                .IsRequired()
                .ValueGeneratedOnAddOrUpdate()
                .HasColumnType("rowversion");

            entity.Property<int>("Status")
                .HasColumnType("int");

            entity.Property<string>("StatusReason")
                .HasMaxLength(500)
                .HasColumnType("nvarchar(500)");

            entity.Property<decimal>("TotalAmount")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            entity.HasKey("Id");
            entity.HasIndex("CustomerId", "CreatedAtUtc");
            entity.HasIndex("ExternalReference").IsUnique();
            entity.HasIndex("Number").IsUnique();
            entity.HasIndex("Status", "CreatedAtUtc");

            entity.ToTable("Orders", "sales", table =>
            {
                table.HasCheckConstraint("CK_Orders_Status", "[Status] BETWEEN 1 AND 4");
                table.HasCheckConstraint("CK_Orders_TotalAmount_Positive", "[TotalAmount] > 0");
            });
        });

        modelBuilder.Entity("Sales.Domain.Orders.OrderItem", entity =>
        {
            entity.Property<Guid>("Id")
                .ValueGeneratedNever()
                .HasColumnType("uniqueidentifier");

            entity.Property<Guid>("OrderId")
                .HasColumnType("uniqueidentifier");

            entity.Property<Guid>("ProductId")
                .HasColumnType("uniqueidentifier");

            entity.Property<string>("ProductName")
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnType("nvarchar(200)");

            entity.Property<int>("Quantity")
                .HasColumnType("int");

            entity.Property<string>("Sku")
                .IsRequired()
                .HasMaxLength(64)
                .HasColumnType("nvarchar(64)");

            entity.Property<decimal>("TotalAmount")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            entity.Property<decimal>("UnitPrice")
                .HasPrecision(18, 2)
                .HasColumnType("decimal(18,2)");

            entity.HasKey("Id");
            entity.HasIndex("OrderId", "ProductId").IsUnique();
            entity.HasIndex("ProductId");

            entity.ToTable("OrderItems", "sales", table =>
            {
                table.HasCheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
                table.HasCheckConstraint("CK_OrderItems_TotalAmount_Positive", "[TotalAmount] > 0");
                table.HasCheckConstraint("CK_OrderItems_UnitPrice_Positive", "[UnitPrice] > 0");
            });
        });

        modelBuilder.Entity("Sales.Domain.Orders.OrderItem", entity =>
        {
            entity.HasOne("Sales.Domain.Orders.Order", null)
                .WithMany("Items")
                .HasForeignKey("OrderId")
                .OnDelete(DeleteBehavior.Cascade)
                .IsRequired();
        });

        modelBuilder.Entity("Sales.Domain.Orders.Order", entity =>
        {
            entity.Navigation("Items");
        });
#pragma warning restore 612, 618
    }
}
