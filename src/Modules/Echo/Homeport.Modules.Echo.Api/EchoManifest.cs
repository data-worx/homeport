// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk;

namespace Homeport.Modules.Echo.Api;

/// <summary>What Echo declares about itself in its manifest.</summary>
internal static class EchoManifest
{
    /// <summary>The route that returns the request body unchanged.</summary>
    internal const string EchoPath = "/v1/echo";

    /// <summary>The scope a caller needs for <see cref="EchoPath"/>.</summary>
    internal const string InvokeScope = "echo.invoke";

    private const string RequestsMeter = "requests";

    internal static ModuleManifest Create()
    {
        return new ModuleManifest(
            Id: "echo",
            Name: "Echo",
            Version: ModuleVersion.Of(typeof(EchoManifest).Assembly),
            Routes: [new ManifestRoute(HttpMethods.Post, EchoPath, InvokeScope, RequestsMeter, Quantity: 1)],
            Scopes: [InvokeScope],
            Meters: [RequestsMeter]);
    }
}
