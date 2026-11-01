// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Internal.Platform.ControlPlane;

/// <summary>How the gateway reaches the control plane and asks it about a client.</summary>
public static class ControlPlaneService
{
    /// <summary>The control plane's service name, as the AppHost declares it.</summary>
    public const string ServiceName = "control-plane";

    /// <summary>The audience of the service tokens the control plane accepts (ADR-0014).</summary>
    public const string Audience = "homeport-control-plane";

    /// <summary>The route value that names the client in <see cref="ClientPath"/>.</summary>
    public const string ClientIdRouteValue = "clientId";

    /// <summary><c>GET</c> returns a <see cref="ClientResponse"/>, <c>404</c> for an unknown client.</summary>
    public const string ClientPath = "/clients/{" + ClientIdRouteValue + "}";
}
