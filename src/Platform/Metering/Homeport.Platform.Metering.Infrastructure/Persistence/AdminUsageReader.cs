// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Platform admin (ADR-0020): reads the aggregates of every tenant on the
/// connection of the admin role, which row-level security lets read every
/// tenant's aggregates through the policy <c>admin_read</c> and nothing else.
/// The one code path in metering that skips the query filters.
/// </summary>
internal sealed class AdminUsageReader(
    [FromKeyedServices(MeteringPersistenceExtensions.AdminConnectionName)] MeteringDbContext database) : IAdminUsageReader
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantUsageTotal>> ReadAsync(Guid? tenantId, UsageQuery query, AuditEntry audit, CancellationToken cancellationToken)
    {
        // A transaction of its own needs the execution strategy around it, so
        // a transient failure retries the whole of it, the audit entry included.
        return await database.Database.CreateExecutionStrategy().ExecuteAsync(
            async cancellation =>
            {
                database.ChangeTracker.Clear();
                await using var transaction = await database.Database.BeginTransactionAsync(cancellation);

                database.AuditEntries.Add(audit);
                await database.SaveChangesAsync(cancellation);

                var totals = query.Granularity switch
                {
                    UsageGranularity.Hour => await ReadHoursAsync(tenantId, query, cancellation),
                    UsageGranularity.Day => await ReadDaysAsync(tenantId, query, cancellation),
                    _ => throw new ArgumentOutOfRangeException(nameof(query), query.Granularity, "Every granularity is read."),
                };

                await transaction.CommitAsync(cancellation);
                return totals;
            },
            cancellationToken);
    }

    /// <summary>The hourly totals in the query's range, of one tenant or of all.</summary>
    private async Task<IReadOnlyList<TenantUsageTotal>> ReadHoursAsync(Guid? tenantId, UsageQuery query, CancellationToken cancellationToken)
    {
        // Platform admin: every tenant's rows, which the admin role's policy admits.
        var hours = database.UsageHourly.IgnoreQueryFilters().AsNoTracking().Where(h => h.Hour >= query.From && h.Hour < query.To);
        if (tenantId is { } id)
        {
            hours = hours.Where(h => h.TenantId == id);
        }

        if (query.ModuleId is { } moduleId)
        {
            hours = hours.Where(h => h.ModuleId == moduleId);
        }

        if (query.Meter is { } meter)
        {
            hours = hours.Where(h => h.Meter == meter);
        }

        var rows = await hours
            .OrderBy(h => h.Hour).ThenBy(h => h.TenantId).ThenBy(h => h.ModuleId).ThenBy(h => h.Meter)
            .ToListAsync(cancellationToken);

        return rows.Select(h => new TenantUsageTotal(h.TenantId, h.Hour, h.ModuleId, h.Meter, h.Quantity)).ToList();
    }

    /// <summary>The daily totals in the query's range, of one tenant or of all.</summary>
    private async Task<IReadOnlyList<TenantUsageTotal>> ReadDaysAsync(Guid? tenantId, UsageQuery query, CancellationToken cancellationToken)
    {
        // Platform admin: every tenant's rows, which the admin role's policy admits.
        var days = database.UsageDaily.IgnoreQueryFilters().AsNoTracking().Where(d => d.Day >= query.FirstDay && d.Day < query.EndDay);
        if (tenantId is { } id)
        {
            days = days.Where(d => d.TenantId == id);
        }

        if (query.ModuleId is { } moduleId)
        {
            days = days.Where(d => d.ModuleId == moduleId);
        }

        if (query.Meter is { } meter)
        {
            days = days.Where(d => d.Meter == meter);
        }

        var rows = await days
            .OrderBy(d => d.Day).ThenBy(d => d.TenantId).ThenBy(d => d.ModuleId).ThenBy(d => d.Meter)
            .ToListAsync(cancellationToken);

        return rows
            .Select(d => new TenantUsageTotal(d.TenantId, new DateTimeOffset(d.Day, TimeOnly.MinValue, TimeSpan.Zero), d.ModuleId, d.Meter, d.Quantity))
            .ToList();
    }
}
