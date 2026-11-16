// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Reports metering unhealthy while the admin connection cannot sign in, so
/// a wrong admin password shows on the readiness check rather than only at
/// the operator's first read.
/// </summary>
/// <remarks>
/// The result carries the exception, so the health check log tells a refused
/// password from a database that cannot be reached.
/// </remarks>
/// <param name="adminContext">The context on <see cref="MeteringPersistenceExtensions.AdminConnectionName"/>.</param>
internal sealed class AdminConnectionHealthCheck(
    [FromKeyedServices(MeteringPersistenceExtensions.AdminConnectionName)] MeteringDbContext adminContext) : IHealthCheck
{
    /// <summary>The name the check is registered under.</summary>
    public const string Name = MeteringPersistenceExtensions.AdminConnectionName;

    /// <summary>The description of a failed check; the exception carries the cause.</summary>
    internal const string FailureDescription = "The admin connection cannot sign in to the database.";

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        try
        {
            await adminContext.Database.OpenConnectionAsync(cancellationToken);
        }
        catch (DbException exception)
        {
            return new HealthCheckResult(context.Registration.FailureStatus, FailureDescription, exception);
        }

        await adminContext.Database.CloseConnectionAsync();
        return HealthCheckResult.Healthy();
    }
}
