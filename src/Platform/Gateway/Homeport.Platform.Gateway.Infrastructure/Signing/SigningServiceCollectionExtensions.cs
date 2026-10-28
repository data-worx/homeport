// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Gateway.Infrastructure.Signing;

/// <summary>Wires the gateway's signing key and its service tokens.</summary>
public static class SigningServiceCollectionExtensions
{
    /// <summary>
    /// Binds <see cref="SigningOptions"/> and checks them at start-up, so the
    /// gateway does not start without a usable signing key.
    /// </summary>
    public static IServiceCollection AddGatewaySigning(this IServiceCollection services)
    {
        services.AddOptions<SigningOptions>()
            .BindConfiguration(SigningOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<SigningOptions>, SigningOptionsValidator>();
        services.AddSingleton<SigningKeys>();
        services.AddSingleton<ServiceTokenIssuer>();
        services.TryAddSingleton(TimeProvider.System);

        return services;
    }

    /// <summary>
    /// Sends every request of the client with a service token for
    /// <paramref name="audience"/>, the platform service it calls.
    /// </summary>
    public static IHttpClientBuilder AddServiceToken(this IHttpClientBuilder client, string audience)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(audience);

        return client.AddHttpMessageHandler(services =>
            new ServiceTokenHandler(services.GetRequiredService<ServiceTokenIssuer>(), audience));
    }
}
