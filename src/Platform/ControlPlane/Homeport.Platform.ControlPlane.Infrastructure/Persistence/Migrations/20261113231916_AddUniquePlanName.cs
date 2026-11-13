using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddUniquePlanName : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_Plans_Name",
                schema: "control_plane",
                table: "Plans",
                column: "Name",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Plans_Name",
                schema: "control_plane",
                table: "Plans");
        }
    }
}
