// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Shared.Client.ModuleContract;

/// <summary>The headers a module sends the gateway with its response, which the gateway removes.</summary>
public static class ModuleHeaders
{
    /// <summary>
    /// Reports how much a request counts, as <c>&lt;meter&gt;=&lt;quantity&gt;</c>,
    /// instead of the quantity the route declares. The meter is the route's
    /// own, and the quantity a positive integer.
    /// </summary>
    public const string Usage = "X-Homeport-Usage";
}
