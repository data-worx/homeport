// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;
using Homeport.Shared.Contracts;

namespace Homeport.Platform.ControlPlane.Application.Tenants;

/// <summary>
/// The operator lists, creates and renames tenants. Every change is stored
/// together with its audit entry (ADR-0020).
/// </summary>
public sealed class TenantAdministration(ITenantStore store, TimeProvider clock)
{
    /// <summary>The tenant does not exist.</summary>
    public const string NotFoundCode = "tenant.not-found";

    /// <summary>Every tenant, ordered by name.</summary>
    public Task<IReadOnlyList<TenantOverview>> ListAsync(CancellationToken cancellationToken)
    {
        return store.ListAsync(cancellationToken);
    }

    /// <summary>The tenant <paramref name="id"/>, or <see cref="NotFoundCode"/>.</summary>
    public async Task<Result<TenantOverview>> GetAsync(Guid id, CancellationToken cancellationToken)
    {
        var tenant = await store.FindOverviewAsync(id, cancellationToken);

        return tenant is null ? NotFound(id) : Result<TenantOverview>.Success(tenant);
    }

    /// <summary>Creates a tenant named <paramref name="name"/> on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<TenantOverview>> CreateAsync(string name, Operator by, CancellationToken cancellationToken)
    {
        var now = clock.GetUtcNow();
        var created = Tenant.Create(Guid.CreateVersion7(now), name, await store.NamesAsync(except: null, cancellationToken));
        if (!created.IsSuccess)
        {
            return Result<TenantOverview>.Failure(created.Error);
        }

        var tenant = created.Value;
        store.Add(tenant);
        store.Add(by.Audit(now, AuditEntry.TenantCreated, AuditEntry.TenantTarget(tenant.Id), $"Created the tenant '{tenant.Name}'."));

        return await SaveAsync(tenant, clientCount: 0, cancellationToken);
    }

    /// <summary>Renames the tenant <paramref name="id"/> to <paramref name="name"/> on behalf of <paramref name="by"/>.</summary>
    public async Task<Result<TenantOverview>> RenameAsync(Guid id, string name, Operator by, CancellationToken cancellationToken)
    {
        var overview = await store.FindOverviewAsync(id, cancellationToken);
        var tenant = await store.FindAsync(id, cancellationToken);
        if (overview is null || tenant is null)
        {
            return NotFound(id);
        }

        var oldName = tenant.Name;
        var renamed = tenant.Rename(name, await store.NamesAsync(except: id, cancellationToken));
        if (!renamed.IsSuccess)
        {
            return Result<TenantOverview>.Failure(renamed.Error);
        }

        if (tenant.Name == oldName)
        {
            return Result<TenantOverview>.Success(overview);
        }

        store.Add(by.Audit(clock.GetUtcNow(), AuditEntry.TenantRenamed, AuditEntry.TenantTarget(id), $"Renamed the tenant '{oldName}' to '{tenant.Name}'."));

        return await SaveAsync(tenant, overview.ClientCount, cancellationToken);
    }

    private async Task<Result<TenantOverview>> SaveAsync(Tenant tenant, int clientCount, CancellationToken cancellationToken)
    {
        // The name was free when it was checked; another change may have
        // taken it since, which the store's unique name catches.
        if (!await store.TrySaveAsync(cancellationToken))
        {
            return Result<TenantOverview>.Failure(Tenant.NameTaken(tenant.Name));
        }

        return Result<TenantOverview>.Success(new TenantOverview(tenant.Id, tenant.Name, clientCount));
    }

    private static Result<TenantOverview> NotFound(Guid id)
    {
        return Result<TenantOverview>.Failure(new Error(NotFoundCode, $"There is no tenant {id}."));
    }
}
