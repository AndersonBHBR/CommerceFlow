using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Inventory.Infrastructure.Persistence.Migrations;

[DbContext(typeof(InventoryDbContext))]
[Migration("20260819000100_InitialInventory")]
public sealed class InitialInventory : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "inventory");

        migrationBuilder.CreateTable(
            name: "Products",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                IsActive = table.Column<bool>(type: "bit", nullable: false),
                AvailableQuantity = table.Column<int>(type: "int", nullable: false),
                ReservedQuantity = table.Column<int>(type: "int", nullable: false),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Products", item => item.Id);
                table.CheckConstraint(
                    "CK_Products_AvailableQuantity_NonNegative",
                    "[AvailableQuantity] >= 0");
                table.CheckConstraint(
                    "CK_Products_QuantityConsistency",
                    "[AvailableQuantity] >= [ReservedQuantity]");
                table.CheckConstraint(
                    "CK_Products_ReservedQuantity_NonNegative",
                    "[ReservedQuantity] >= 0");
            });

        migrationBuilder.CreateTable(
            name: "StockAdjustments",
            schema: "inventory",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                Reason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                PerformedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: true),
                OccurredAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_StockAdjustments", item => item.Id);
                table.CheckConstraint("CK_StockAdjustments_Quantity_NonZero", "[Quantity] <> 0");
                table.ForeignKey(
                    name: "FK_StockAdjustments_Products_ProductId",
                    column: item => item.ProductId,
                    principalSchema: "inventory",
                    principalTable: "Products",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Restrict);
            });

        migrationBuilder.CreateIndex(
            name: "IX_Products_Sku",
            schema: "inventory",
            table: "Products",
            column: "Sku",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_StockAdjustments_ProductId_OccurredAtUtc",
            schema: "inventory",
            table: "StockAdjustments",
            columns: ["ProductId", "OccurredAtUtc"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "StockAdjustments", schema: "inventory");
        migrationBuilder.DropTable(name: "Products", schema: "inventory");
    }
}
