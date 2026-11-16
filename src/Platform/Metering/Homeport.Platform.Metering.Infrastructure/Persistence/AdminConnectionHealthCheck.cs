// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Homeport.Platform.Metering.Infrastructure.Persistence;

/// <summary>
/// Reports metering unhealthy while the admin connection cannot sign in, so
/// a wrong admin password shows at start rather than at the operator's first
/// read.
/// </summary>
/// <param name="adminContext">The context on <see cref="MeteringPersistenceExtensions.AdminConnectionName"/>.</param>
internal sealed class AdminConnectionHealthCheck(
    [FromKeyedServices(MeteringPersistenceExtensions.AdminConnectionName)] MeteringDbContext adminContext) : IHealthCheck
{
    /// <summary>The name the check is registered under.</summary>
    public const string Name = MeteringPersistenceExtensions.AdminConnectionName;

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
    {
        return await adminContext.Database.CanConnectAsync(cancellationToken)
            ? HealthCheckResult.Healthy()
            : new HealthCheckResult(context.Registration.FailureStatus, "The admin connection cannot reach the database or sign in.");
    }
}
