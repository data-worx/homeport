// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Infrastructure.ApiDocs;

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>Wires the consolidated API docs (ADR-0017).</summary>
internal static class ApiDocsServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="ApiDocsOptions"/>, on by default in Development, the
    /// catalog of documents and the refresher that rebuilds it when the
    /// routes change and, for the platform, periodically. Needs
    /// <see cref="Routing.PlatformRoutes"/> and the gateway's signing.
    /// </summary>
    internal static IServiceCollection AddApiDocs(this IServiceCollection services)
    {
        // The default first, so a configured value overrides it.
        services.AddOptions<ApiDocsOptions>()
            .Configure<IHostEnvironment>((options, environment) => options.Enabled = environment.IsDevelopment())
            .BindConfiguration(ApiDocsOptions.SectionName);
        services.AddModuleDocumentsClient();
        services.AddPlatformDocumentsClient();
        services.AddSingleton<ApiDocsCatalog>();
        services.AddSingleton<ApiDocsRefresher>();
        services.AddHostedService(provider => provider.GetRequiredService<ApiDocsRefresher>());

        return services;
    }
}
