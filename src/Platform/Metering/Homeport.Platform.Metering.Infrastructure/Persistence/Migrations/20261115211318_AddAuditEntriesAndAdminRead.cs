using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.Metering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntriesAndAdminRead : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "metering",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    OccurredAt = table.Column<DateTimeOffset>(type: "timestamp with time zone", nullable: false),
                    OperatorSubject = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    OperatorName = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Action = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Target = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    Summary = table.Column<string>(type: "character varying(1000)", maxLength: 1000, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_audit_entries", x => x.Id);
                });

            // Written by hand, not scaffolded: EF has no model for privileges
            // or row-level security. The admin role reads the aggregates of
            // every tenant and writes the audit entries (ADR-0020).
            AuditPrivileges.Restrict(migrationBuilder, MeteringDbContext.AuditEntriesTable);
            RowLevelSecurity.EnableAdminRead(migrationBuilder, MeteringDbContext.UsageHourlyTable);
            RowLevelSecurity.EnableAdminRead(migrationBuilder, MeteringDbContext.UsageDailyTable);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_OccurredAt",
                schema: "metering",
                table: "audit_entries",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RowLevelSecurity.DisableAdminRead(migrationBuilder, MeteringDbContext.UsageDailyTable);
            RowLevelSecurity.DisableAdminRead(migrationBuilder, MeteringDbContext.UsageHourlyTable);

            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "metering");
        }
    }
}
