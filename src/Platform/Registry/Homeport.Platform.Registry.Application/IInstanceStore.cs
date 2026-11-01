// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application;

/// <summary>Where the live instances are kept, each for a limited time.</summary>
public interface IInstanceStore
{
    /// <summary>Keeps <paramref name="instance"/> for <paramref name="lifetime"/>, replacing what was kept under its id.</summary>
    Task SaveAsync(Instance instance, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>Keeps the instance for another <paramref name="lifetime"/>.</summary>
    /// <returns>Whether the instance was still kept; <c>false</c> once it has expired.</returns>
    Task<bool> RenewAsync(string moduleId, string instanceId, TimeSpan lifetime, CancellationToken cancellationToken);

    /// <summary>Every instance that has not expired.</summary>
    Task<IReadOnlyList<Instance>> ListAsync(CancellationToken cancellationToken);
}
