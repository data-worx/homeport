// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Portal.ApiDocs;

/// <summary>Wires the link to the gateway's API docs.</summary>
internal static class ApiDocsServiceCollectionExtensions
{
    /// <summary>The message of a docs address that is not absolute.</summary>
    internal const string NotAbsoluteMessage = "ApiDocs:Url must be an absolute address, such as https://api.example.com/platform/docs.";

    /// <summary>Adds <see cref="ApiDocsOptions"/>, checked at start-up.</summary>
    /// <param name="services">The services of the host.</param>
    /// <returns>The same services, for chaining.</returns>
    internal static IServiceCollection AddApiDocsLink(this IServiceCollection services)
    {
        services.AddOptions<ApiDocsOptions>()
            .BindConfiguration(ApiDocsOptions.SectionName)
            .Validate(options => options.Url is null || options.Url.IsAbsoluteUri, NotAbsoluteMessage)
            .ValidateOnStart();

        return services;
    }
}
