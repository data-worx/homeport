// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Registration;
using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application;

/// <summary>
/// The registry's use cases: module instances register and keep themselves
/// registered, and the gateway reads the routes of the live ones (ADR-0004).
/// </summary>
public sealed class Registrations(IInstanceStore store, IRegistrationKeySource keys)
{
    /// <summary>
    /// How long an instance stays registered without a heartbeat. The SDK
    /// sends one every 10 s, so two may be lost before the instance is dropped.
    /// </summary>
    public static readonly TimeSpan InstanceLifetime = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Whether <paramref name="presentedKey"/> is a valid registration key for
    /// <paramref name="moduleId"/>; never for a module id the registry does not know.
    /// </summary>
    public bool IsAuthorized(string moduleId, string presentedKey)
    {
        return RegistrationKeys.Matches(presentedKey, keys.KeysFor(moduleId));
    }

    /// <summary>Registers the instance, or registers it again, when its manifest holds.</summary>
    /// <returns>
    /// Every way the manifest breaks the module contract, by field path;
    /// empty when the instance was registered.
    /// </returns>
    public async Task<IReadOnlyDictionary<string, string[]>> RegisterAsync(
        string moduleId,
        Instance instance,
        CancellationToken cancellationToken)
    {
        var errors = ManifestRules.Validate(moduleId, instance.Manifest);
        if (errors.Count == 0)
        {
            await store.SaveAsync(instance, InstanceLifetime, cancellationToken);
        }

        return errors;
    }

    /// <summary>Keeps the instance registered for another <see cref="InstanceLifetime"/>.</summary>
    /// <returns>Whether the instance was still registered; when not, it has to register again.</returns>
    public Task<bool> RenewAsync(string moduleId, string instanceId, CancellationToken cancellationToken)
    {
        return store.RenewAsync(moduleId, instanceId, InstanceLifetime, cancellationToken);
    }

    /// <summary>The routes of every module with at least one live instance.</summary>
    public async Task<IReadOnlyList<ModuleRoutes>> RoutesAsync(CancellationToken cancellationToken)
    {
        return RouteTable.Build(await store.ListAsync(cancellationToken));
    }
}
