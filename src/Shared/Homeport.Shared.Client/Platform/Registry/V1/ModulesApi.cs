// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.Platform.Registry.V1;

/// <summary>
/// The registered modules of the admin API (ADR-0020):
/// <c>/platform/v1/admin/modules</c> on the gateway's admin endpoint, for the
/// operator only.
/// </summary>
public static class ModulesApi
{
    /// <summary>
    /// The path below <c>/platform</c>, the path the registry serves it at.
    /// <c>GET</c> returns <see cref="ModulesResponse"/>.
    /// </summary>
    public const string Path = "/v1/admin/modules";
}
