// SPDX-License-Identifier: AGPL-3.0-or-later

using Microsoft.Extensions.DependencyInjection;

namespace Homeport.Platform.Gateway.Infrastructure.ApiDocs;

/// <summary>Wires the client that reads the OpenAPI documents of module instances.</summary>
public static class ModuleDocumentsServiceCollectionExtensions
{
    /// <summary>Adds <see cref="ModuleDocumentsClient"/>.</summary>
    public static IServiceCollection AddModuleDocumentsClient(this IServiceCollection services)
    {
        services.AddHttpClient(ModuleDocumentsClient.ClientName);
        services.AddSingleton<ModuleDocumentsClient>();

        return services;
    }
}
