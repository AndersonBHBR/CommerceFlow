using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sales.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("20260820000100_InitialSales")]
public sealed class InitialSales : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.EnsureSchema(name: "sales");

        migrationBuilder.CreateTable(
            name: "Orders",
            schema: "sales",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Number = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                CustomerId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ExternalReference = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false),
                Status = table.Column<int>(type: "int", nullable: false),
                StatusReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                CreatedBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                CreatedAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false),
                CancelledBy = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: true),
                CancelledAtUtc = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                RowVersion = table.Column<byte[]>(type: "rowversion", rowVersion: true, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_Orders", item => item.Id);
                table.CheckConstraint("CK_Orders_Status", "[Status] BETWEEN 1 AND 4");
                table.CheckConstraint("CK_Orders_TotalAmount_Positive", "[TotalAmount] > 0");
            });

        migrationBuilder.CreateTable(
            name: "OrderItems",
            schema: "sales",
            columns: table => new
            {
                Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                ProductId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                Sku = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                ProductName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                Quantity = table.Column<int>(type: "int", nullable: false),
                UnitPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                TotalAmount = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_OrderItems", item => item.Id);
                table.CheckConstraint("CK_OrderItems_Quantity_Positive", "[Quantity] > 0");
                table.CheckConstraint("CK_OrderItems_TotalAmount_Positive", "[TotalAmount] > 0");
                table.CheckConstraint("CK_OrderItems_UnitPrice_Positive", "[UnitPrice] > 0");
                table.ForeignKey(
                    name: "FK_OrderItems_Orders_OrderId",
                    column: item => item.OrderId,
                    principalSchema: "sales",
                    principalTable: "Orders",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_OrderId_ProductId",
            schema: "sales",
            table: "OrderItems",
            columns: ["OrderId", "ProductId"],
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_OrderItems_ProductId",
            schema: "sales",
            table: "OrderItems",
            column: "ProductId");

        migrationBuilder.CreateIndex(
            name: "IX_Orders_CustomerId_CreatedAtUtc",
            schema: "sales",
            table: "Orders",
            columns: ["CustomerId", "CreatedAtUtc"]);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_ExternalReference",
            schema: "sales",
            table: "Orders",
            column: "ExternalReference",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Number",
            schema: "sales",
            table: "Orders",
            column: "Number",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "IX_Orders_Status_CreatedAtUtc",
            schema: "sales",
            table: "Orders",
            columns: ["Status", "CreatedAtUtc"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "OrderItems", schema: "sales");
        migrationBuilder.DropTable(name: "Orders", schema: "sales");
    }
}
