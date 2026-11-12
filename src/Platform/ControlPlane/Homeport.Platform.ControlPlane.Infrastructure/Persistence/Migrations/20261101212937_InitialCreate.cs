using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "control_plane");

            migrationBuilder.CreateTable(
                name: "Plans",
                schema: "control_plane",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false),
                    RequestsPerSecond = table.Column<int>(type: "integer", nullable: false),
                    Burst = table.Column<int>(type: "integer", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Plans", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Tenants",
                schema: "control_plane",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uuid", nullable: false),
                    Name = table.Column<string>(type: "character varying(200)", maxLength: 200, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Tenants", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Clients",
                schema: "control_plane",
                columns: table => new
                {
                    ClientId = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                    TenantId = table.Column<Guid>(type: "uuid", nullable: false),
                    PlanId = table.Column<Guid>(type: "uuid", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Clients", x => x.ClientId);
                    table.ForeignKey(
                        name: "FK_Clients_Plans_PlanId",
                        column: x => x.PlanId,
                        principalSchema: "control_plane",
                        principalTable: "Plans",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_Clients_Tenants_TenantId",
                        column: x => x.TenantId,
                        principalSchema: "control_plane",
                        principalTable: "Tenants",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Clients_PlanId",
                schema: "control_plane",
                table: "Clients",
                column: "PlanId");

            migrationBuilder.CreateIndex(
                name: "IX_Clients_TenantId",
                schema: "control_plane",
                table: "Clients",
                column: "TenantId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Clients",
                schema: "control_plane");

            migrationBuilder.DropTable(
                name: "Plans",
                schema: "control_plane");

            migrationBuilder.DropTable(
                name: "Tenants",
                schema: "control_plane");
        }
    }
}
