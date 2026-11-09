// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.ServiceDefaults;

/// <summary>
/// The names of Homeport's own span attributes and metric labels, which every
/// component uses the same way.
/// </summary>
public static class TelemetryAttributes
{
    /// <summary>The tenant a request or an event belongs to.</summary>
    public const string TenantId = "homeport.tenant.id";

    /// <summary>The client that made the request.</summary>
    public const string ClientId = "homeport.client.id";

    /// <summary>The module the request was routed to.</summary>
    public const string ModuleId = "homeport.module.id";

    /// <summary>The meter the request is counted in.</summary>
    public const string Meter = "homeport.meter";
}
