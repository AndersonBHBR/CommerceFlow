using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace Sales.Infrastructure.Persistence.Migrations;

[DbContext(typeof(SalesDbContext))]
[Migration("20260821000200_ReliableMessaging")]
public sealed class ReliableMessaging : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "InboxMessages",
            schema: "sales",
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
            schema: "sales",
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

        migrationBuilder.CreateIndex(
            name: "IX_InboxMessages_ProcessedAtUtc",
            schema: "sales",
            table: "InboxMessages",
            column: "ProcessedAtUtc");
        migrationBuilder.CreateIndex(
            name: "IX_OutboxMessages_PublishedAtUtc_NextAttemptAtUtc",
            schema: "sales",
            table: "OutboxMessages",
            columns: ["PublishedAtUtc", "NextAttemptAtUtc"]);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "InboxMessages", schema: "sales");
        migrationBuilder.DropTable(name: "OutboxMessages", schema: "sales");
    }
}
