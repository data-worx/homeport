// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Homeport.Platform.Portal.AdminApi;

/// <summary>Wires the portal's client of the admin API (ADR-0020).</summary>
internal static class AdminApiServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="IAdminApi"/> at the address of <see cref="AdminApiOptions"/>,
    /// checked at start-up, with the operator's tokens kept per circuit.
    /// </summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The same services, for chaining.</returns>
    internal static IServiceCollection AddAdminApi(this IServiceCollection services)
    {
        services.AddOptions<AdminApiOptions>()
            .BindConfiguration(AdminApiOptions.SectionName)
            .ValidateOnStart();
        services.AddSingleton<IValidateOptions<AdminApiOptions>, AdminApiOptionsValidator>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddHttpContextAccessor();
        services.AddSingleton<OperatorTokenRefresher>();

        // Scoped is per circuit: each circuit keeps the tokens of its operator.
        services.AddScoped<OperatorTokens>();
        services.AddHttpClient<IAdminApi, AdminApiClient>((provider, client) =>
            client.BaseAddress = provider.GetRequiredService<IOptions<AdminApiOptions>>().Value.Url);

        return services;
    }
}
