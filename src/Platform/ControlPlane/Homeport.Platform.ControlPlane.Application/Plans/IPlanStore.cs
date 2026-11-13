// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.ControlPlane.Domain;

namespace Homeport.Platform.ControlPlane.Application.Plans;

/// <summary>
/// The plans and the audit entries of their changes. What is added, changed
/// or removed is stored by <see cref="TrySaveAsync"/>, all of it or nothing.
/// </summary>
public interface IPlanStore
{
    /// <summary>Every plan, ordered by name.</summary>
    Task<IReadOnlyList<PlanOverview>> ListAsync(CancellationToken cancellationToken);

    /// <summary>The plan <paramref name="id"/>, or <see langword="null"/>.</summary>
    Task<PlanOverview?> FindOverviewAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>
    /// The plan <paramref name="id"/>, tracked so that a change to it is
    /// stored by <see cref="TrySaveAsync"/>, or <see langword="null"/>.
    /// </summary>
    Task<Plan?> FindAsync(Guid id, CancellationToken cancellationToken);

    /// <summary>The names of every plan but <paramref name="except"/>.</summary>
    Task<IReadOnlyList<string>> NamesAsync(Guid? except, CancellationToken cancellationToken);

    /// <summary>Adds <paramref name="plan"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(Plan plan);

    /// <summary>Removes <paramref name="plan"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Remove(Plan plan);

    /// <summary>Adds <paramref name="entry"/>, stored by <see cref="TrySaveAsync"/>.</summary>
    void Add(AuditEntry entry);

    /// <summary>
    /// Stores every change in one transaction, unless a concurrent change
    /// took the name or started using a removed plan; then nothing is stored.
    /// </summary>
    Task<PlanSaveOutcome> TrySaveAsync(CancellationToken cancellationToken);
}
