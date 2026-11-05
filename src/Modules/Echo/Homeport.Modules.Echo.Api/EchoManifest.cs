// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk;
using Homeport.Shared.Client.ModuleContract;

namespace Homeport.Modules.Echo.Api;

/// <summary>What Echo declares about itself in its manifest.</summary>
internal static class EchoManifest
{
    /// <summary>The route that returns the request body unchanged.</summary>
    internal const string EchoPath = "/v1/echo";

    /// <summary>The route that returns a batch of items unchanged and counts one per item.</summary>
    internal const string BatchPath = "/v1/echo/batch";

    /// <summary>The meter a batch is counted in, one per item.</summary>
    internal const string ItemsMeter = "items";

    /// <summary>The route that returns the headers of the request as Echo received it.</summary>
    internal const string RequestPath = "/v1/request";

    /// <summary>The route that returns who called, as the module token names the caller.</summary>
    internal const string CallerPath = "/v1/caller";

    /// <summary>The scope a caller needs for every route of Echo.</summary>
    internal const string InvokeScope = "echo.invoke";

    private const string RequestsMeter = "requests";

    internal static ModuleManifest Create()
    {
        return new ModuleManifest(
            Id: "echo",
            Name: "Echo",
            Version: ModuleVersion.Of(typeof(EchoManifest).Assembly),
            Routes:
            [
                new ManifestRoute(HttpMethods.Post, EchoPath, InvokeScope, RequestsMeter, Quantity: 1),
                new ManifestRoute(HttpMethods.Get, RequestPath, InvokeScope, RequestsMeter, Quantity: 1),
                new ManifestRoute(HttpMethods.Get, CallerPath, InvokeScope, RequestsMeter, Quantity: 1),
                new ManifestRoute(HttpMethods.Post, BatchPath, InvokeScope, ItemsMeter, Quantity: 1),
            ],
            Scopes: [InvokeScope],
            Meters: [RequestsMeter, ItemsMeter]);
    }
}
