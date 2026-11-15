// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Domain;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// The usage events and their hourly and daily aggregates in the schema
/// <see cref="Schema"/>, and the audit entries of the operator's reads. The
/// usage is tenant-owned: each table is filtered to the
/// <see cref="CurrentTenant"/> here and by row-level security in the database.
/// The audit entries are a platform table, append-only for the service
/// (<see cref="AuditPrivileges"/>).
/// </summary>
internal sealed class MeteringDbContext(DbContextOptions<MeteringDbContext> options, CurrentTenant tenant) : DbContext(options)
{
    /// <summary>The schema metering owns; the migration history lives there too.</summary>
    internal const string Schema = "metering";

    /// <summary>The table of the usage events.</summary>
    internal const string UsageEventsTable = "UsageEvents";

    /// <summary>The table of the hourly aggregates.</summary>
    internal const string UsageHourlyTable = "UsageHourly";

    /// <summary>The table of the daily aggregates.</summary>
    internal const string UsageDailyTable = "UsageDaily";

    /// <summary>The table of the audit entries.</summary>
    internal const string AuditEntriesTable = "audit_entries";

    /// <summary>The usage events, one per metered request.</summary>
    public DbSet<UsageEvent> UsageEvents => Set<UsageEvent>();

    /// <summary>The usage per tenant, module, meter and hour.</summary>
    public DbSet<HourlyUsage> UsageHourly => Set<HourlyUsage>();

    /// <summary>The usage per tenant, module, meter and day.</summary>
    public DbSet<DailyUsage> UsageDaily => Set<DailyUsage>();

    /// <summary>The audit entries of the operator's reads of usage.</summary>
    public DbSet<AuditEntry> AuditEntries => Set<AuditEntry>();

    /// <summary>
    /// The current tenant. Read by the query filters each time a query runs, so they follow the
    /// tenant.
    /// </summary>
    private Guid? TenantId => tenant.Id;

    /// <summary>
    /// Maps the usage tables into metering's schema, each with the tenant query
    /// filter, and the audit table without one.
    /// </summary>
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

        modelBuilder.Entity<AuditEntry>(entry =>
        {
            entry.ToTable(AuditEntriesTable);
            entry.Property(e => e.Id).ValueGeneratedNever();
            entry.Property(e => e.OperatorSubject).HasMaxLength(AuditEntry.MaxFieldLength);
            entry.Property(e => e.OperatorName).HasMaxLength(AuditEntry.MaxFieldLength);
            entry.Property(e => e.Action).HasMaxLength(AuditEntry.MaxFieldLength);
            entry.Property(e => e.Target).HasMaxLength(AuditEntry.MaxFieldLength);
            entry.Property(e => e.Summary).HasMaxLength(AuditEntry.MaxSummaryLength);
            entry.HasIndex(e => e.OccurredAt);
        });
    }
}
