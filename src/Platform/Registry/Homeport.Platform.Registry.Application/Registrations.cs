// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Registry.Domain.Manifests;
using Homeport.Platform.Registry.Domain.Registration;
using Homeport.Platform.Registry.Domain.Routing;

namespace Homeport.Platform.Registry.Application;

/// <summary>
/// The registry's use cases: module instances register and keep themselves
/// registered, the gateway reads the routes of the live ones (ADR-0004), and
/// the operator reads the modules (ADR-0020).
/// </summary>
public sealed class Registrations(IInstanceStore store, IRegistrationKeySource keys, TimeProvider clock)
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
        var stored = await store.ListAsync(cancellationToken);
        return RouteTable.Build(stored.Select(kept => kept.Instance));
    }

    /// <summary>
    /// Every module with at least one live instance, ordered by module id,
    /// with its instances and the routes the gateway serves for it.
    /// </summary>
    /// <remarks>
    /// Every registration and heartbeat keeps an instance for
    /// <see cref="InstanceLifetime"/>, so its last heartbeat lies that long
    /// before its registration ends.
    /// </remarks>
    public async Task<IReadOnlyList<RegisteredModule>> ModulesAsync(CancellationToken cancellationToken)
    {
        var stored = await store.ListAsync(cancellationToken);
        var now = clock.GetUtcNow();
        var routes = RouteTable.Build(stored.Select(kept => kept.Instance))
            .ToDictionary(module => module.ModuleId, module => module.Routes, StringComparer.Ordinal);

        return stored
            .GroupBy(kept => kept.Instance.Manifest.Id, StringComparer.Ordinal)
            .OrderBy(module => module.Key, StringComparer.Ordinal)
            .Select(module => new RegisteredModule(
                module.Key,
                module
                    .OrderBy(kept => kept.Instance.InstanceId, StringComparer.Ordinal)
                    .Select(kept => Registered(kept, now))
                    .ToList(),
                routes[module.Key]))
            .ToList();
    }

    /// <summary>An instance with the moments its registration started and ends, seen at <paramref name="now"/>.</summary>
    private static RegisteredInstance Registered(StoredInstance kept, DateTimeOffset now)
    {
        var expiresAt = now + kept.TimeToLive;
        return new RegisteredInstance(kept.Instance, expiresAt - InstanceLifetime, expiresAt);
    }
}
