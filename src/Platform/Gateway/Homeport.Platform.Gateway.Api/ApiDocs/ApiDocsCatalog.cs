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
/// route table, and one for the platform of the platform routes. A document
/// that cannot be read is kept as last read. The module documents stay as
/// they are until the next route table arrives, the platform's until the
/// next time the platform services' documents are read.
/// </summary>
public sealed class ApiDocsCatalog(
    ModuleDocumentsClient modules,
    PlatformDocumentsClient platform,
    PlatformRoutes platformRoutes,
    ILogger<ApiDocsCatalog> logger)
{
    private static readonly string GatewayVersion =
        typeof(ApiDocsCatalog).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
        ?? typeof(ApiDocsCatalog).Assembly.GetName().Version?.ToString()
        ?? string.Empty;

    // Each read by one refresh alone, one at a time.
    private readonly Dictionary<Uri, JsonObject> moduleSources = [];
    private readonly Dictionary<Uri, JsonObject> platformSources = [];

    // Each replaced as a whole, never changed, so a request reads a consistent document.
    private IReadOnlyDictionary<string, string> moduleDocuments = new Dictionary<string, string>(StringComparer.Ordinal);
    private string platformDocument = ConsolidatedDocuments.ForPlatform(GatewayVersion, PlatformRoutes.PlatformPrefix, [], new Dictionary<Uri, JsonObject>()).ToJsonString();

    /// <summary>The names of the documents: the platform first, then the module ids in order.</summary>
    internal IReadOnlyList<string> Names =>
        [ApiDocsPaths.PlatformDocument, .. Volatile.Read(ref moduleDocuments).Keys.Order(StringComparer.Ordinal)];

    /// <summary>The document called <paramref name="name"/>, as JSON; <c>null</c> when there is none.</summary>
    internal string? Find(string name)
    {
        return name == ApiDocsPaths.PlatformDocument
            ? Volatile.Read(ref platformDocument)
            : Volatile.Read(ref moduleDocuments).GetValueOrDefault(name);
    }

    /// <summary>Reads the documents <paramref name="table"/> names and rebuilds every module's.</summary>
    internal async Task RefreshAsync(RouteTableResponse table, CancellationToken cancellationToken)
    {
        var urls = table.Modules.SelectMany(module => module.Routes).Select(route => route.OpenApiUrl).ToHashSet();
        foreach (var gone in moduleSources.Keys.Where(url => !urls.Contains(url)).ToList())
        {
            moduleSources.Remove(gone);
        }

        var unread = 0;
        foreach (var url in urls)
        {
            if (!await TryReadAsync(moduleSources, url, () => modules.ReadAsync(url, cancellationToken), cancellationToken))
            {
                unread++;
            }
        }

        var rebuilt = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var module in table.Modules)
        {
            var routes = module.Routes
                .Select(route => new DocumentedRoute(route.Method, route.Path, route.Version, route.OpenApiUrl))
                .ToList();
            var prefix = $"{ModuleRoutes.ModulesPrefix}/{module.ModuleId}";
            rebuilt[module.ModuleId] = ConsolidatedDocuments.ForModule(module.ModuleId, prefix, routes, moduleSources).ToJsonString();
        }

        Volatile.Write(ref moduleDocuments, rebuilt);

        logger.LogInformation(
            "API docs rebuilt for modules {ModuleIds} from {DocumentCount} documents, {UnreadCount} of them not read",
            table.Modules.Select(module => module.ModuleId),
            urls.Count,
            unread);
    }

    /// <summary>Reads the documents of the platform services and rebuilds the platform's.</summary>
    internal async Task RefreshPlatformAsync(CancellationToken cancellationToken)
    {
        var documented = platformRoutes.Documented(GatewayVersion);
        var unread = 0;
        foreach (var (url, audience) in documented.Select(entry => (entry.Route.OpenApiUrl, entry.Audience)).Distinct())
        {
            if (!await TryReadAsync(platformSources, url, () => platform.ReadAsync(url, audience, cancellationToken), cancellationToken))
            {
                unread++;
            }
        }

        var routes = documented.Select(entry => entry.Route).ToList();
        var rebuilt = ConsolidatedDocuments.ForPlatform(GatewayVersion, PlatformRoutes.PlatformPrefix, routes, platformSources).ToJsonString();

        // Read periodically, so only a change is worth more than a debug line.
        if (Interlocked.Exchange(ref platformDocument, rebuilt) == rebuilt)
        {
            logger.LogDebug("Platform API docs unchanged, {UnreadCount} documents not read", unread);
            return;
        }

        logger.LogInformation(
            "Platform API docs rebuilt for {RouteCount} platform routes, {UnreadCount} documents not read",
            routes.Count,
            unread);
    }

    // Reads one document into sources; one that fails keeps the last one read.
    private async Task<bool> TryReadAsync(
        Dictionary<Uri, JsonObject> sources,
        Uri url,
        Func<Task<JsonObject>> read,
        CancellationToken cancellationToken)
    {
        try
        {
            sources[url] = await read();
            return true;
        }
        catch (Exception exception) when (exception is HttpRequestException or JsonException or TaskCanceledException
            && !cancellationToken.IsCancellationRequested)
        {
            logger.LogWarning(
                exception,
                "The OpenAPI document at {OpenApiUrl} could not be read; {Fallback}",
                url,
                sources.ContainsKey(url) ? "keeping the last one read" : "its routes stay undocumented");
            return false;
        }
    }
}
