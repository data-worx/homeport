// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Shared.Client.ModuleContract;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Homeport.Platform.Gateway.Api.Usage;

/// <summary>
/// Takes the <see cref="ModuleHeaders.Usage"/> header off a module's response
/// before it reaches the caller and keeps it as a <see cref="UsageReport"/>
/// for <see cref="UsageEvents"/>. Done here, while the response has not
/// started: once it has, its headers are gone.
/// </summary>
internal sealed class UsageReports : ITransformProvider
{
    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddResponseTransform(TakeAsync);
    }

    /// <summary>Moves the module's usage report from the response into the request's features.</summary>
    internal static ValueTask TakeAsync(ResponseTransformContext context)
    {
        var headers = context.HttpContext.Response.Headers;
        if (headers.Remove(ModuleHeaders.Usage, out var report))
        {
            context.HttpContext.Features.Set(new UsageReport(report.ToString()));
        }

        return ValueTask.CompletedTask;
    }
}

/// <summary>The quantity a module reported for a request, as it sent it.</summary>
/// <param name="Value">The header's value; several values are joined by a comma.</param>
internal sealed record UsageReport(string Value);
