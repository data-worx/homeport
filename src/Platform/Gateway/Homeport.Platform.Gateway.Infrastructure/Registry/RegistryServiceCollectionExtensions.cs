// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.Signing;
using Homeport.Shared.Internal.Platform.Registry;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Registry;

/// <summary>Wires the gateway's client of the registry.</summary>
public static class RegistryServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="RegistryClient"/>, which calls the registry from
    /// <see cref="RegistryOptions"/> with a service token. Needs
    /// <see cref="SigningServiceCollectionExtensions.AddGatewaySigning"/>.
    /// </summary>
    public static IServiceCollection AddRegistryClient(this IServiceCollection services)
    {
        services.AddOptions<RegistryOptions>().BindConfiguration(RegistryOptions.SectionName);
        services.AddHttpClient(RegistryClient.ClientName)
            .ConfigureHttpClient((provider, client) => client.BaseAddress = provider.GetRequiredService<IOptions<RegistryOptions>>().Value.Url)
            .AddServiceToken(RegistryService.Audience);
        services.AddSingleton<RegistryClient>();

        return services;
    }
}
