// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// Gives every request the remote address <paramref name="address"/> before
/// the gateway's own pipeline sees it. The in-memory test server sets none, so
/// without it no request comes from anywhere a proxy could be trusted at.
/// </summary>
internal sealed class CallerAddress(IPAddress address) : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next)
    {
        return app =>
        {
            app.Use((context, nextMiddleware) =>
            {
                context.Connection.RemoteIpAddress = address;

                return nextMiddleware(context);
            });
            next(app);
        };
    }
}
