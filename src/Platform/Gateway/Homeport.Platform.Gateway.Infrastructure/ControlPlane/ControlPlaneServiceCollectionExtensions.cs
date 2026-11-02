// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Internal.Platform.ControlPlane;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.ControlPlane;

/// <summary>Wires the gateway's client of the control plane.</summary>
public static class ControlPlaneServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ControlPlaneClient"/>, which calls the control plane
    /// from <see cref="ControlPlaneOptions"/> with a service token. Needs
    /// <see cref="SigningServiceCollectionExtensions.AddGatewaySigning"/>.
    /// </summary>
    public static IServiceCollection AddControlPlaneClient(this IServiceCollection services)
    {
        services.AddOptions<ControlPlaneOptions>().BindConfiguration(ControlPlaneOptions.SectionName);
        services.AddMemoryCache();
        services.AddHttpClient(ControlPlaneClient.ClientName)
            .ConfigureHttpClient((provider, client) => client.BaseAddress = provider.GetRequiredService<IOptions<ControlPlaneOptions>>().Value.Url)
            .AddServiceToken(ControlPlaneService.Audience);
        services.AddSingleton<ControlPlaneClient>();

        return services;
    }
}
