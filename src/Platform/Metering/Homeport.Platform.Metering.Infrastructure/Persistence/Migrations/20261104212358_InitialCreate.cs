using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.Metering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "metering");

            migrationBuilder.CreateTable(
                name: "UsageDaily",
                schema: "metering",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<string>(type: "text", nullable: false),
                    Meter = table.Column<string>(type: "text", nullable: false),
                    Day = table.Column<DateOnly>(type: "date", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageDaily", x => new { x.TenantId, x.ModuleId, x.Meter, x.Day });
                });

            migrationBuilder.CreateTable(
                name: "UsageEvents",
                schema: "metering",
                columns: table => new
                {
                    EventId = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ClientId = table.Column<string>(type: "text", nullable: false),
                    ModuleId = table.Column<string>(type: "text", nullable: false),
                    Route = table.Column<string>(type: "text", nullable: false),
                    Meter = table.Column<string>(type: "text", nullable: false),
                    Quantity = table.Column<int>(type: "integer", nullable: false),
                    StatusCode = table.Column<int>(type: "integer", nullable: false),
                    DurationMs = table.Column<long>(type: "bigint", nullable: false),
                    TraceId = table.Column<string>(type: "text", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageEvents", x => x.EventId);
                });

            migrationBuilder.CreateTable(
                name: "UsageHourly",
                schema: "metering",
                columns: table => new
                {
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    ModuleId = table.Column<string>(type: "text", nullable: false),
                    Meter = table.Column<string>(type: "text", nullable: false),
                    Hour = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    Quantity = table.Column<long>(type: "bigint", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_UsageHourly", x => new { x.TenantId, x.ModuleId, x.Meter, x.Hour });
                });

            migrationBuilder.CreateIndex(
                name: "IX_UsageEvents_TenantId",
                schema: "metering",
                table: "UsageEvents",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "UsageDaily",
                schema: "metering");

            migrationBuilder.DropTable(
                name: "UsageEvents",
                schema: "metering");

            migrationBuilder.DropTable(
                name: "UsageHourly",
                schema: "metering");
        }
    }
}
