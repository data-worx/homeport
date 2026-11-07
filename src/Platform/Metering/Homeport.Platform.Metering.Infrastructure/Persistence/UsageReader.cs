// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Reads the aggregates in PostgreSQL, working for the tenant asked for: the
/// query filters and row-level security both see only its rows.
/// </summary>
internal sealed class UsageReader(MeteringDbContext database, CurrentTenant tenant) : IUsageReader
{
    public async Task<IReadOnlyList<UsageTotal>> ReadAsync(Guid tenantId, UsageQuery query, CancellationToken cancellationToken)
    {
        // Before the first connection opens, which the session setting is set on.
        tenant.Id = tenantId;

        return query.Granularity switch
        {
            UsageGranularity.Hour => await ReadHoursAsync(query, cancellationToken),
            UsageGranularity.Day => await ReadDaysAsync(query, cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(query), query.Granularity, "Every granularity is read."),
        };
    }

    private async Task<IReadOnlyList<UsageTotal>> ReadHoursAsync(UsageQuery query, CancellationToken cancellationToken)
    {
        var hours = database.UsageHourly.AsNoTracking().Where(h => h.Hour >= query.From && h.Hour < query.To);
        if (query.ModuleId is { } moduleId)
        {
            hours = hours.Where(h => h.ModuleId == moduleId);
        }

        if (query.Meter is { } meter)
        {
            hours = hours.Where(h => h.Meter == meter);
        }

        var rows = await hours
            .OrderBy(h => h.Hour).ThenBy(h => h.ModuleId).ThenBy(h => h.Meter)
            .ToListAsync(cancellationToken);

        return rows.Select(h => new UsageTotal(h.Hour, h.ModuleId, h.Meter, h.Quantity)).ToList();
    }

    private async Task<IReadOnlyList<UsageTotal>> ReadDaysAsync(UsageQuery query, CancellationToken cancellationToken)
    {
        var days = database.UsageDaily.AsNoTracking().Where(d => d.Day >= query.FirstDay && d.Day < query.EndDay);
        if (query.ModuleId is { } moduleId)
        {
            days = days.Where(d => d.ModuleId == moduleId);
        }

        if (query.Meter is { } meter)
        {
            days = days.Where(d => d.Meter == meter);
        }

        var rows = await days
            .OrderBy(d => d.Day).ThenBy(d => d.ModuleId).ThenBy(d => d.Meter)
            .ToListAsync(cancellationToken);

        return rows
            .Select(d => new UsageTotal(new DateTimeOffset(d.Day, TimeOnly.MinValue, TimeSpan.Zero), d.ModuleId, d.Meter, d.Quantity))
            .ToList();
    }
}
