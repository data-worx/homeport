// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.ModuleContract;

/// <summary>
/// Where a module instance registers and sends its heartbeats. Unversioned:
/// the <see cref="ModuleManifest.ContractVersion"/> in the payload decides (ADR-0013).
/// </summary>
public static class RegistryPaths
{
    /// <summary>
    /// <c>PUT</c> registers the instance with a <see cref="RegisterInstanceRequest"/>,
    /// or registers it again; <c>{instanceId}</c> is chosen by the instance.
    /// </summary>
    public const string Instance = "/instances/{moduleId}/{instanceId}";

    /// <summary>
    /// <c>POST</c> keeps the instance registered. <c>404</c> means the registry
    /// no longer knows it, and the instance registers again.
    /// </summary>
    public const string Heartbeat = Instance + "/heartbeat";
}
