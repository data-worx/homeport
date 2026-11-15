// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Tenants;
using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>The tenants and their audit entries in <see cref="ControlPlaneDbContext"/>.</summary>
internal sealed class TenantStore(ControlPlaneDbContext database, ILogger<TenantStore> logger) : ITenantStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<TenantOverview>> ListAsync(CancellationToken cancellationToken)
    {
        return await Overviews(database.Tenants.OrderBy(t => t.Name))
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<TenantOverview?> FindOverviewAsync(Guid id, CancellationToken cancellationToken)
    {
        return Overviews(database.Tenants.Where(t => t.Id == id))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Tenant?> FindAsync(Guid id, CancellationToken cancellationToken)
    {
        return database.Tenants.SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<string>> NamesAsync(Guid? except, CancellationToken cancellationToken)
    {
        return await database.Tenants
            .Where(t => t.Id != except)
            .Select(t => t.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Tenant tenant)
    {
        database.Tenants.Add(tenant);
    }

    /// <inheritdoc />
    public void Add(AuditEntry entry)
    {
        database.AuditEntries.Add(entry);
    }

    /// <inheritdoc />
    public async Task<bool> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return true;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The only unique column a change of a tenant can collide on is its name.
            logger.LogDebug("A tenant's name was taken by a concurrent change; nothing was stored");
            database.ChangeTracker.Clear();
            return false;
        }
    }

    /// <summary>
    /// The tenants as overviews, filtered and ordered before the projection, which EF cannot see
    /// into.
    /// </summary>
    private IQueryable<TenantOverview> Overviews(IQueryable<Tenant> tenants)
    {
        return tenants
            .AsNoTracking()
            .Select(t => new TenantOverview(t.Id, t.Name, database.Clients.Count(c => c.TenantId == t.Id)));
    }
}
