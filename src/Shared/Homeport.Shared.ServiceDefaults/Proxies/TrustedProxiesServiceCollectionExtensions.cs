// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homeport.Shared.ServiceDefaults.Proxies;

/// <summary>Wires which callers a service takes forwarded headers from.</summary>
public static class TrustedProxiesServiceCollectionExtensions
{
    /// <summary>
    /// Applies <c>X-Forwarded-For</c> and <c>X-Forwarded-Proto</c> only from the
    /// proxies in <see cref="TrustedProxiesOptions"/>, checked at start-up.
    /// ASP.NET Core trusts loopback by default; a service does not, so a
    /// caller on the same host cannot spoof its address or scheme either.
    /// <c>X-Forwarded-Host</c> is never applied: no service routes by host.
    /// Takes effect with <c>UseForwardedHeaders</c>.
    /// </summary>
    /// <remarks>
    /// The middleware checks the caller only while a proxy or a network is
    /// known, and trusts every caller otherwise. Without one, the service
    /// therefore applies no forwarded header at all - which also holds when
    /// <c>ASPNETCORE_FORWARDEDHEADERS_ENABLED</c> is set, since these options
    /// are configured after the host's own.
    /// </remarks>
    /// <param name="services">The services of the host.</param>
    /// <returns>The same services, for chaining.</returns>
    public static IServiceCollection AddTrustedProxies(this IServiceCollection services)
    {
        services.AddOptions<TrustedProxiesOptions>()
            .BindConfiguration(TrustedProxiesOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<TrustedProxiesOptions>, TrustedProxiesOptionsValidator>();

        services.AddOptions<ForwardedHeadersOptions>()
            .Configure<IOptions<TrustedProxiesOptions>>((forwarded, trusted) =>
            {
                forwarded.ForwardedHeaders = trusted.Value.Proxies.Count == 0 && trusted.Value.Networks.Count == 0
                    ? ForwardedHeaders.None
                    : ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
                forwarded.KnownProxies.Clear();
                forwarded.KnownIPNetworks.Clear();

                // Unreadable entries fail validation at start-up instead.
                foreach (var proxy in trusted.Value.Proxies)
                {
                    if (IPAddress.TryParse(proxy, out var address))
                    {
                        forwarded.KnownProxies.Add(address);
                    }
                }

                foreach (var network in trusted.Value.Networks)
                {
                    if (System.Net.IPNetwork.TryParse(network, out var parsed))
                    {
                        forwarded.KnownIPNetworks.Add(parsed);
                    }
                }
            });

        return services;
    }
}
