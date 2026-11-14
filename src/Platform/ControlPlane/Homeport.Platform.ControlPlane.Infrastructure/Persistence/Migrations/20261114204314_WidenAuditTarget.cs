using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class WidenAuditTarget : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Target",
                schema: "control_plane",
                table: "audit_entries",
                type: "character varying(263)",
                maxLength: 263,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(255)",
                oldMaxLength: 255);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Target",
                schema: "control_plane",
                table: "audit_entries",
                type: "character varying(255)",
                maxLength: 255,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "character varying(263)",
                oldMaxLength: 263);
        }
    }
}
