// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>Applies metering's migrations; for the <c>migrate</c> command of the host.</summary>
public static class MeteringMigrations
{
    /// <summary>
    /// Applies the pending migrations on the connection of <paramref name="services"/>,
    /// which must be the owner role's.
    /// </summary>
    /// <returns>The migrations it applied.</returns>
    public static async Task<IReadOnlyList<string>> ApplyAsync(IServiceProvider services, CancellationToken cancellationToken)
    {
        var database = services.GetRequiredService<MeteringDbContext>().Database;

        // Through the execution strategy like everything after it: the first
        // connection may come while PostgreSQL still initialises, and is
        // retried rather than failing the step.
        var pending = (await database.CreateExecutionStrategy()
            .ExecuteAsync(database.GetPendingMigrationsAsync, cancellationToken)).ToList();
        await database.MigrateAsync(cancellationToken);

        return pending;
    }
}
