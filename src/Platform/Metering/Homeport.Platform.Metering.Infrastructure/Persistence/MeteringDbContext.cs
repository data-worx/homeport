// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// The usage events and their hourly and daily aggregates in the schema
/// <see cref="Schema"/>. All three are tenant-owned: each is filtered to the
/// <see cref="CurrentTenant"/> here and by row-level security in the database.
/// </summary>
internal sealed class MeteringDbContext(DbContextOptions<MeteringDbContext> options, CurrentTenant tenant) : DbContext(options)
{
    /// <summary>The schema metering owns; the migration history lives there too.</summary>
    internal const string Schema = "metering";

    internal const string UsageEventsTable = "UsageEvents";

    internal const string UsageHourlyTable = "UsageHourly";

    internal const string UsageDailyTable = "UsageDaily";

    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();

    public DbSet<HourlyUsage> UsageHourly => Set<HourlyUsage>();

    public DbSet<DailyUsage> UsageDaily => Set<DailyUsage>();

    // Read by the query filters each time a query runs, so they follow the tenant.
    private Guid? TenantId => tenant.Id;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema(Schema);

        modelBuilder.Entity<UsageEvent>(usage =>
        {
            usage.ToTable(UsageEventsTable);
            usage.HasKey(u => u.EventId);
            usage.Property(u => u.EventId).ValueGeneratedNever();
            usage.HasIndex(u => u.TenantId);
            usage.HasQueryFilter(u => u.TenantId == TenantId);
        });

        modelBuilder.Entity<HourlyUsage>(hourly =>
        {
            hourly.ToTable(UsageHourlyTable);
            hourly.HasKey(h => new { h.TenantId, h.ModuleId, h.Meter, h.Hour });
            hourly.HasQueryFilter(h => h.TenantId == TenantId);
        });

        modelBuilder.Entity<DailyUsage>(daily =>
        {
            daily.ToTable(UsageDailyTable);
            daily.HasKey(d => new { d.TenantId, d.ModuleId, d.Meter, d.Day });
            daily.HasQueryFilter(d => d.TenantId == TenantId);
        });
    }
}
