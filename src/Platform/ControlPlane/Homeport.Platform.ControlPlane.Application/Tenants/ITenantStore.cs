// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application.Tenants;

/// <summary>
/// The tenants and the audit entries of their changes. What is added or
/// changed is stored by <see cref="TrySaveAsync"/>, all of it or nothing.
/// </summary>
public interface ITenantStore
{
    /// <summary>Every tenant, ordered by name.</summary>
    Task<IReadOnlyList<TenantOverview>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The tenant <paramref name="id"/>, or <see langword="null"/>.</summary>
    Task<TenantOverview?> FindOverviewAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The tenant <paramref name="id"/>, tracked so that a change to it is
    /// stored by <see cref="TrySaveAsync"/>, or <see langword="null"/>.
    /// </summary>
    Task<Tenant?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The names of every tenant but <paramref name="except"/>.</summary>
    Task<IReadOnlyList<string>> NamesAsync(Guid? except, CancellationToken cancellationToken);

    /// <summary>Adds <paramref name="tenant"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(Tenant tenant);

    /// <summary>Adds <paramref name="entry"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(AuditEntry entry);

    /// <summary>
    /// Stores every change in one transaction. <see langword="false"/> when
    /// another tenant took the name meanwhile; then nothing is stored.
    /// </summary>
    Task<bool> TrySaveAsync(CancellationToken cancellationToken);
}
