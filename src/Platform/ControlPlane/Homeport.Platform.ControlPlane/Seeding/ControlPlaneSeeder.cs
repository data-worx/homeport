// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Persistence;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.ControlPlane.Seeding;

/// <summary>
/// Brings the seeded tenant, plan and client in line with
/// <see cref="SeedOptions"/>: adds what is missing and updates what changed,
/// so running it again leaves exactly one of each.
/// </summary>
internal sealed class ControlPlaneSeeder(ControlPlaneDbContext database, IOptions<SeedOptions> options)
{
    internal async Task SeedAsync(CancellationToken cancellationToken)
    {
        var seed = options.Value;

        var tenant = await database.Tenants.FindAsync([seed.TenantId], cancellationToken);
        if (tenant is null)
        {
            database.Tenants.Add(new Tenant { Id = seed.TenantId, Name = seed.TenantName });
        }
        else
        {
            tenant.Name = seed.TenantName;
        }

        var plan = await database.Plans.FindAsync([seed.PlanId], cancellationToken);
        if (plan is null)
        {
            database.Plans.Add(new Plan { Id = seed.PlanId, Name = seed.PlanName, RequestsPerSecond = seed.RequestsPerSecond, Burst = seed.Burst });
        }
        else
        {
            plan.Name = seed.PlanName;
            plan.RequestsPerSecond = seed.RequestsPerSecond;
            plan.Burst = seed.Burst;
        }

        var client = await database.Clients.FindAsync([seed.ClientId], cancellationToken);
        if (client is null)
        {
            database.Clients.Add(new Client { ClientId = seed.ClientId, TenantId = seed.TenantId, PlanId = seed.PlanId });
        }
        else
        {
            client.TenantId = seed.TenantId;
            client.PlanId = seed.PlanId;
        }

        await database.SaveChangesAsync(cancellationToken);
    }
}
