// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Platform.Gateway.Domain.ApiDocs;
using Homeport.Platform.Gateway.Infrastructure.ApiDocs;
using Homeport.Shared.Internal.Platform.Registry;

namespace Homeport.Platform.Gateway.Api.ApiDocs;

/// <summary>
/// The documents of the consolidated API docs: one per module of the last
/// route table, and one for the platform. A module document that cannot be
/// read is kept as last read, and the documents stay as they are until the
/// next route table arrives.
/// </summary>
public sealed class ApiDocsCatalog(ModuleDocumentsClient client, ILogger<ApiDocsCatalog> logger)
{
    private static readonly string GatewayVersion =
        typeof(ApiDocsCatalog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ApiDocsCatalog).Assembly.GetName().Version?.ToString()
        ?? string.Empty;

    // Read by the refresher alone, one table at a time.
    private readonly Dictionary<Uri, JsonObject> sources = [];

    // Replaced as a whole, never changed, so a request reads one consistent set.
    private IReadOnlyDictionary<string, string> documents = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ApiDocsPaths.PlatformDocument] = ConsolidatedDocuments.ForPlatform(GatewayVersion).ToJsonString(),
    };

    /// <summary>The names of the documents: the platform first, then the module ids in order.</summary>
    internal IReadOnlyList<string> Names
    {
        get
        {
            var names = Volatile.Read(ref documents).Keys.Where(name => name != ApiDocsPaths.PlatformDocument).Order(StringComparer.Ordinal);
            return [ApiDocsPaths.PlatformDocument, .. names];
        }
    }

    /// <summary>The document called <paramref name="name"/>, as JSON; <c>null</c> when there is none.</summary>
    internal string? Find(string name)
    {
        return Volatile.Read(ref documents).GetValueOrDefault(name);
    }

    /// <summary>Reads the documents <paramref name="table"/> names and rebuilds every module's.</summary>
    internal async Task RefreshAsync(RouteTableResponse table, CancellationToken cancellationToken)
    {
        var urls = table.Modules.SelectMany(module => module.Routes).Select(route => route.OpenApiUrl).ToHashSet();
        foreach (var gone in sources.Keys.Where(url => !urls.Contains(url)).ToList())
        {
            sources.Remove(gone);
        }

        var unread = 0;
        foreach (var url in urls)
        {
            try
            {
                sources[url] = await client.ReadAsync(url, cancellationToken);
            }
            catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
                && !cancellationToken.IsCancellationRequested)
            {
                unread++;
                logger.LogWarning(
                    exception,
                    "The OpenAPI document at {OpenApiUrl} could not be read; {Fallback}",
                    url,
                    sources.ContainsKey(url) ? "keeping the last one read" : "its routes stay undocumented");
            }
        }

        var rebuilt = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            [ApiDocsPaths.PlatformDocument] = ConsolidatedDocuments.ForPlatform(GatewayVersion).ToJsonString(),
        };

        foreach (var module in table.Modules)
        {
            var routes = module.Routes
                .Select(route => new DocumentedRoute(route.Method, route.Path, route.Version, route.OpenApiUrl))
                .ToList();
            var prefix = $"{ModuleRoutes.ModulesPrefix}/{module.ModuleId}";
            rebuilt[module.ModuleId] = ConsolidatedDocuments.ForModule(module.ModuleId, prefix, routes, sources).ToJsonString();
        }

        Volatile.Write(ref documents, rebuilt);

        logger.LogInformation(
            "API docs rebuilt for modules {ModuleIds} from {DocumentCount} documents, {UnreadCount} of them not read",
            table.Modules.Select(module => module.ModuleId),
            urls.Count,
            unread);
    }
}
