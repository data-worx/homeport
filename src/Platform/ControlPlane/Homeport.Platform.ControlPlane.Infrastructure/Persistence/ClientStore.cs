// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Application.Clients;
using Homeport.Platform.ControlPlane.Domain;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;

namespace Homeport.Platform.ControlPlane.Infrastructure.Persistence;

/// <summary>The registered clients and their audit entries in <see cref="ControlPlaneDbContext"/>.</summary>
internal sealed class ClientStore(ControlPlaneDbContext database, ILogger<ClientStore> logger) : IClientStore
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<ClientOverview>> ListAsync(Guid? tenantId, CancellationToken cancellationToken)
    {
        var clients = tenantId is { } id ? database.Clients.Where(c => c.TenantId == id) : database.Clients;

        return await Overviews(clients)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ClientOverview?> FindOverviewAsync(string clientId, CancellationToken cancellationToken)
    {
        return Overviews(database.Clients.Where(c => c.ClientId == clientId))
            .SingleOrDefaultAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<Client?> FindAsync(string clientId, CancellationToken cancellationToken)
    {
        return database.Clients.SingleOrDefaultAsync(c => c.ClientId == clientId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Tenant?> FindTenantAsync(Guid id, CancellationToken cancellationToken)
    {
        return database.Tenants.AsNoTracking().SingleOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public Task<Plan?> FindPlanAsync(Guid id, CancellationToken cancellationToken)
    {
        return database.Plans.AsNoTracking().SingleOrDefaultAsync(p => p.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public void Add(Client client)
    {
        database.Clients.Add(client);
    }

    /// <inheritdoc />
    public void Remove(Client client)
    {
        database.Clients.Remove(client);
    }

    /// <inheritdoc />
    public void Add(AuditEntry entry)
    {
        database.AuditEntries.Add(entry);
    }

    /// <inheritdoc />
    public async Task<ClientSaveOutcome> TrySaveAsync(CancellationToken cancellationToken)
    {
        try
        {
            await database.SaveChangesAsync(cancellationToken);
            return ClientSaveOutcome.Saved;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            // The only unique column a change of a client can collide on is its id.
            logger.LogDebug("A client id was registered by a concurrent change; nothing was stored");
            database.ChangeTracker.Clear();
            return ClientSaveOutcome.ClientIdTaken;
        }
        catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation })
        {
            // Tenants are never deleted, so the reference that vanished is the plan's.
            logger.LogDebug("A client's plan was deleted by a concurrent change; nothing was stored");
            database.ChangeTracker.Clear();
            return ClientSaveOutcome.PlanUnknown;
        }
    }

    /// <summary>
    /// The clients as overviews, ordered before the projection, which EF cannot see into.
    /// </summary>
    private IQueryable<ClientOverview> Overviews(IQueryable<Client> clients)
    {
        return from client in clients.AsNoTracking()
               join tenant in database.Tenants on client.TenantId equals tenant.Id
               join plan in database.Plans on client.PlanId equals plan.Id
               orderby client.ClientId
               select new ClientOverview(client.ClientId, client.TenantId, tenant.Name, plan.Id, plan.Name);
    }
}
