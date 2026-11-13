using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddAuditEntriesAndUniqueTenantName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "audit_entries",
                schema: "control_plane",
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

            // Written by hand, not scaffolded: EF has no model for privileges.
            AuditPrivileges.Restrict(migrationBuilder, ControlPlaneDbContext.AuditEntriesTable);

            migrationBuilder.CreateIndex(
                name: "IX_Tenants_Name",
                schema: "control_plane",
                table: "Tenants",
                column: "Name",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_audit_entries_OccurredAt",
                schema: "control_plane",
                table: "audit_entries",
                column: "OccurredAt");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "audit_entries",
                schema: "control_plane");

            migrationBuilder.DropIndex(
                name: "IX_Tenants_Name",
                schema: "control_plane",
                table: "Tenants");
        }
    }
}
