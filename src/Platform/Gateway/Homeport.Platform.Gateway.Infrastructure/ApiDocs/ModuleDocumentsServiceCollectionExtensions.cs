// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Gateway.Infrastructure.ApiDocs;

/// <summary>Wires the clients that read the OpenAPI documents of module instances and platform services.</summary>
public static class ModuleDocumentsServiceCollectionExtensions
{
    /// <summary>Adds <see cref="ModuleDocumentsClient"/>.</summary>
    public static IServiceCollection AddModuleDocumentsClient(this IServiceCollection services)
    {
        services.AddHttpClient(ModuleDocumentsClient.ClientName);
        services.AddSingleton<ModuleDocumentsClient>();

        return services;
    }

    /// <summary>
    /// Adds <see cref="PlatformDocumentsClient"/>. Needs
    /// <see cref="Signing.SigningServiceCollectionExtensions.AddGatewaySigning"/>.
    /// </summary>
    public static IServiceCollection AddPlatformDocumentsClient(this IServiceCollection services)
    {
        services.AddHttpClient(PlatformDocumentsClient.ClientName);
        services.AddSingleton<PlatformDocumentsClient>();

        return services;
    }
}
