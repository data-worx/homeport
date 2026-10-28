// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Platform.Gateway.Api.Routing;
using Homeport.Shared.ServiceDefaults;
using Yarp.ReverseProxy.Transforms;

namespace Homeport.Platform.Gateway.Api;

/// <summary>
/// The gateway: the one entry point of Homeport, which forwards
/// <c>/modules/{moduleId}/{path}</c> to the module.
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the gateway and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceDefaults();
        builder.Services.AddAuthorization();
        builder.Services.AddReverseProxy()
            .LoadFromMemory(StaticRoutes.Routes, StaticRoutes.Clusters)
            .AddTransforms(context => context.AddRequestTransform(HomeportHeaders.StripAsync))
            .AddServiceDiscoveryDestinationResolver();

        var app = builder.Build();
        app.MapDefaultEndpoints();
        app.UseAuthorization();

        // Mapped by YARP rather than served by a controller (dotnet-core.web):
        // the proxy endpoints are framework-mapped, like the health endpoints.
        app.MapReverseProxy();

        app.Run();
    }
}
