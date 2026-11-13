// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// Tells whether a request arrived on the admin port. It goes by the local
/// port of the connection, never by the <c>Host</c> header, which the caller
/// writes.
/// </summary>
internal sealed class AdminPort(IOptions<AdminApiOptions> options)
{
    /// <summary>Whether <paramref name="context"/> arrived on the admin port; never while the admin API is off.</summary>
    internal bool Serves(HttpContext context)
    {
        return options.Value.Port is { } port && context.Connection.LocalPort == port;
    }
}
