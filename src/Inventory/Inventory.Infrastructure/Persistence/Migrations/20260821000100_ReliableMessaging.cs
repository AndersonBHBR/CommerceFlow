using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inventory.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InventoryDbContext))]
[Migration("20260821000100_ReliableMessaging")]
public sealed class ReliableMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InboxMessages",
            schema: "inventory",
            columns: table => new
            {
                MessageId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                ReceivedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ProcessedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table => table.PrimaryKey("PK_InboxMessages", item => item.MessageId));

        migrationBuilder.CreateTable(
            name: "OutboxMessages",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CorrelationId = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                Type = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                RoutingKey = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Payload = table.Column<string>(type: "nvarchar(max)", nullable: false),
                PublishedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                Attempts = table.Column<int>(type: "int", nullable: false),
                NextAttemptAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                LastError = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
            },
            constraints: table => table.PrimaryKey("PK_OutboxMessages", item => item.Id));

        migrationBuilder.CreateTable(
            name: "StockReservations",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                ReservedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                ReleasedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockReservations", item => item.Id);
                table.CheckConstraint("CK_StockReservations_Quantity_Positive", "[Quantity] > 0");
                table.ForeignKey(
                    name: "FK_StockReservations_Products_ProductId",
                    column: item => item.ProductId,
                    principalSchema: "inventory",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_InboxMessages_ProcessedAtUtc",
            schema: "inventory",
            table: "InboxMessages",
            column: "ProcessedAtUtc");
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_PublishedAtUtc_NextAttemptAtUtc",
            schema: "inventory",
            table: "OutboxMessages",
            columns: ["PublishedAtUtc", "NextAttemptAtUtc"]);
        migrationBuilder.CreateIndex(
            name: "IX_StockReservations_OrderId_ProductId",
            schema: "inventory",
            table: "StockReservations",
            columns: ["OrderId", "ProductId"],
            unique: true);
        migrationBuilder.CreateIndex(
            name: "IX_StockReservations_OrderId_ReleasedAtUtc",
            schema: "inventory",
            table: "StockReservations",
            columns: ["OrderId", "ReleasedAtUtc"]);
        migrationBuilder.CreateIndex(
            name: "IX_StockReservations_ProductId",
            schema: "inventory",
            table: "StockReservations",
            column: "ProductId");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InboxMessages", schema: "inventory");
        migrationBuilder.DropTable(name: "OutboxMessages", schema: "inventory");
        migrationBuilder.DropTable(name: "StockReservations", schema: "inventory");
    }
}
