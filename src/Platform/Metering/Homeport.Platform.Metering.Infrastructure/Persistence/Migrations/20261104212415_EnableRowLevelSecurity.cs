using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace Homeport.Platform.Metering.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class EnableRowLevelSecurity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Written by hand, not scaffolded: EF has no model for row-level security.
            RowLevelSecurity.Enable(migrationBuilder, MeteringDbContext.UsageEventsTable);
            RowLevelSecurity.Enable(migrationBuilder, MeteringDbContext.UsageHourlyTable);
            RowLevelSecurity.Enable(migrationBuilder, MeteringDbContext.UsageDailyTable);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            RowLevelSecurity.Disable(migrationBuilder, MeteringDbContext.UsageDailyTable);
            RowLevelSecurity.Disable(migrationBuilder, MeteringDbContext.UsageHourlyTable);
            RowLevelSecurity.Disable(migrationBuilder, MeteringDbContext.UsageEventsTable);
        }
    }
}
