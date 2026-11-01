// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.Registry;

/// <summary>How the gateway reaches the registry and asks it for the routes.</summary>
public static class RegistryService
{
    /// <summary>The registry's service name, as the AppHost declares it.</summary>
    public const string ServiceName = "registry";

    /// <summary>The audience of the service tokens the registry accepts (ADR-0014).</summary>
    public const string Audience = "homeport-registry";

    /// <summary><c>GET</c> returns a <see cref="RouteTableResponse"/> with an <c>ETag</c>.</summary>
    public const string RoutesPath = "/routes";
}
