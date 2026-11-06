// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Domain.ApiDocs;
using Scalar.AspNetCore;

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>Maps the view of the consolidated API docs (ADR-0017).</summary>
internal static class ApiDocsEndpointRouteBuilderExtensions
{
    private const string Title = "Homeport API";

    /// <summary>
    /// Maps Scalar at <see cref="ApiDocsPaths.View"/> on the documents of
    /// <see cref="ApiDocsCatalog"/>. Every asset comes from the gateway:
    /// Scalar's script is embedded, and its CDN fonts, telemetry, AI agent,
    /// MCP, hosted client and developer toolbar, which shares documents
    /// through scalar.com, are switched off.
    /// </summary>
    /// <remarks>
    /// Anonymous on purpose: ADR-0017 makes the view anonymous while it is on.
    /// Mapped by Scalar rather than served by a controller, like MapOpenApi.
    /// </remarks>
    internal static IEndpointConventionBuilder MapApiDocs(this IEndpointRouteBuilder endpoints)
    {
        return endpoints
            .MapScalarApiReference(ApiDocsPaths.View, (options, context) =>
            {
                var catalog = context.RequestServices.GetRequiredService<ApiDocsCatalog>();
                options
                    .WithTitle(Title)
                    .WithOpenApiRoutePattern(ApiDocsPaths.Document)
                    .AddDocuments(catalog.Names)
                    .AddPreferredSecuritySchemes([ConsolidatedDocuments.SecuritySchemeName])
                    .DisableDefaultFonts()
                    .DisableTelemetry()
                    .DisableAgent()
                    .DisableMcp()
                    .HideDeveloperTools();
                options.HideClientButton = true;
            })
            .AllowAnonymous();
    }
}
