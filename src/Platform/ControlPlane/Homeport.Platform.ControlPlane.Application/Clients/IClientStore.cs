// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application.Clients;

/// <summary>
/// The registered clients and the audit entries of their changes. What is
/// added, changed or removed is stored by <see cref="TrySaveAsync"/>, all of
/// it or nothing.
/// </summary>
public interface IClientStore
{
    /// <summary>Every client, or every client of <paramref name="tenantId"/>, ordered by client id.</summary>
    Task<IReadOnlyList<ClientOverview>> ListAsync(Guid? tenantId, CancellationToken cancellationToken);

    /// <summary>The client <paramref name="clientId"/>, or <see langword="null"/>.</summary>
    Task<ClientOverview?> FindOverviewAsync(string clientId, CancellationToken cancellationToken);

    /// <summary>
    /// The client <paramref name="clientId"/>, tracked so that a change to it
    /// is stored by <see cref="TrySaveAsync"/>, or <see langword="null"/>.
    /// </summary>
    Task<Client?> FindAsync(string clientId, CancellationToken cancellationToken);

    /// <summary>The tenant <paramref name="id"/>, read only, or <see langword="null"/>.</summary>
    Task<Tenant?> FindTenantAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The plan <paramref name="id"/>, read only, or <see langword="null"/>.</summary>
    Task<Plan?> FindPlanAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>Adds <paramref name="client"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(Client client);

    /// <summary>Removes <paramref name="client"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Remove(Client client);

    /// <summary>Adds <paramref name="entry"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(AuditEntry entry);

    /// <summary>
    /// Stores every change in one transaction, unless a concurrent change
    /// registered the same client id or deleted the plan; then nothing is stored.
    /// </summary>
    Task<ClientSaveOutcome> TrySaveAsync(CancellationToken cancellationToken);
}
