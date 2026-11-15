// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Plans;
using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>The plans and their audit entries in <see cref="ControlPlaneDbContext"/>.</summary>
internal sealed class PlanStore(ControlPlaneDbContext database, ILogger<PlanStore> logger) : IPlanStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<PlanOverview>> ListAsync(CancellationToken cancellationToken)
    {
        return await Overviews(database.Plans.OrderBy(p => p.Name))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<PlanOverview?> FindOverviewAsync(Guid id, CancellationToken cancellationToken)
    {
        return Overviews(database.Plans.Where(p => p.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Plan?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return database.Plans.SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> NamesAsync(Guid? except, CancellationToken cancellationToken)
    {
        return await database.Plans
            .Where(p => p.Id != except)
            .Select(p => p.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Plan plan)
    {
        database.Plans.Add(plan);
    }

    /// <inheritdoc />
    public void Remove(Plan plan)
    {
        database.Plans.Remove(plan);
    }

    /// <inheritdoc />
    public void Add(AuditEntry entry)
    {
        database.AuditEntries.Add(entry);
    }

    /// <inheritdoc />
    public async Task<PlanSaveOutcome> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return PlanSaveOutcome.Saved;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The only unique column a change of a plan can collide on is its name.
            logger.LogDebug("A plan's name was taken by a concurrent change; nothing was stored");
            database.ChangeTracker.Clear();
            return PlanSaveOutcome.NameTaken;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.RestrictViolation })
        {
            // The only reference a removed plan can still have is a client's,
            // whose foreign key restricts the delete.
            logger.LogDebug("A client started using a deleted plan by a concurrent change; nothing was stored");
            database.ChangeTracker.Clear();
            return PlanSaveOutcome.InUse;
        }
    }

    /// <summary>
    /// The plans as overviews, filtered and ordered before the projection, which EF cannot see
    /// into.
    /// </summary>
    private IQueryable<PlanOverview> Overviews(IQueryable<Plan> plans)
    {
        return plans
            .AsNoTracking()
            .Select(p => new PlanOverview(p.Id, p.Name, p.RequestsPerSecond, p.Burst, database.Clients.Count(c => c.PlanId == p.Id)));
    }
}
