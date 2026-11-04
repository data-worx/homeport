// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Metering.Application;
using Homeport.Platform.Metering.Domain.Usage;
using Microsoft.EntityFrameworkCore;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Keeps usage in PostgreSQL, working for the event's tenant. The unique
/// <see cref="UsageEvent.EventId"/> decides a race between two deliveries of
/// one event, and the aggregates' keys one between two first events of a
/// bucket: the loser fails with a <see cref="DbUpdateException"/>, and its
/// retry finds the row the winner wrote.
/// </summary>
internal sealed class UsageStore(MeteringDbContext database, CurrentTenant tenant) : IUsageStore
{
    public async Task<bool> RecordAsync(UsageEvent usage, HourlyUsage hourly, DailyUsage daily, CancellationToken cancellationToken)
    {
        // Before the first connection opens, which the session setting is set on.
        tenant.Id = usage.TenantId;

        // A transaction of its own needs the execution strategy around it, so
        // a transient failure retries the whole of it.
        return await database.Database.CreateExecutionStrategy().ExecuteAsync(
            async cancellation =>
            {
                database.ChangeTracker.Clear();
                await using var transaction = await database.Database.BeginTransactionAsync(cancellation);

                if (await database.UsageEvents.AnyAsync(u => u.EventId == usage.EventId, cancellation))
                {
                    return false;
                }

                database.UsageEvents.Add(usage);
                await database.SaveChangesAsync(cancellation);

                // One UPDATE that adds in the database, so concurrent events of one
                // bucket never overwrite each other; a missing bucket is inserted.
                var hours = await database.UsageHourly
                    .Where(h => h.TenantId == hourly.TenantId && h.ModuleId == hourly.ModuleId && h.Meter == hourly.Meter && h.Hour == hourly.Hour)
                    .ExecuteUpdateAsync(set => set.SetProperty(h => h.Quantity, h => h.Quantity + hourly.Quantity), cancellation);
                if (hours == 0)
                {
                    database.UsageHourly.Add(hourly);
                }

                var days = await database.UsageDaily
                    .Where(d => d.TenantId == daily.TenantId && d.ModuleId == daily.ModuleId && d.Meter == daily.Meter && d.Day == daily.Day)
                    .ExecuteUpdateAsync(set => set.SetProperty(d => d.Quantity, d => d.Quantity + daily.Quantity), cancellation);
                if (days == 0)
                {
                    database.UsageDaily.Add(daily);
                }

                await database.SaveChangesAsync(cancellation);
                await transaction.CommitAsync(cancellation);
                return true;
            },
            cancellationToken);
    }
}
