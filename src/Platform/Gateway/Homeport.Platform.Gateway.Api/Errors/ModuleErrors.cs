// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using System.Text.Json;
using System.Text.Json.Nodes;
using Homeport.Shared.Client.Problems;
using Yarp.ReverseProxy.Model;
using Yarp.ReverseProxy.Transforms;
using Yarp.ReverseProxy.Transforms.Builder;

namespace Homeport.Platform.Gateway.Api.Errors;

/// <summary>
/// Holds a module's error response to ADR-0016 on its way to the caller. A
/// conforming 4xx Problem Details body is passed through with <c>traceId</c>
/// added. Every other error body is dropped, and the status code pages write a
/// Problem Details body of the gateway's in its place, through
/// <see cref="GatewayProblemDetails"/>.
/// </summary>
/// <remarks>
/// The body of a 5xx is dropped even when it conforms, so no internals of the
/// module reach the caller.
/// </remarks>
internal sealed class ModuleErrors(ILogger<ModuleErrors> logger) : ITransformProvider
{
    // An error body is read into memory; one larger than this is not taken for
    // Problem Details, whatever its media type says.
    private const int MaxProblemBytes = 64 * 1024;

    public void ValidateRoute(TransformRouteValidationContext context)
    {
    }

    public void ValidateCluster(TransformClusterValidationContext context)
    {
    }

    public void Apply(TransformBuilderContext context)
    {
        context.AddResponseTransform(RewriteAsync);
    }

    /// <summary>Passes a conforming 4xx body through with <c>traceId</c>, and drops every other error body.</summary>
    internal async ValueTask RewriteAsync(ResponseTransformContext context)
    {
        var response = context.HttpContext.Response;

        // No proxy response means the module did not answer; the forwarder error
        // is answered by the status code pages.
        if (context.ProxyResponse is not { } moduleResponse || response.StatusCode < StatusCodes.Status400BadRequest)
        {
            return;
        }

        var moduleId = context.HttpContext.Features.Get<IReverseProxyFeature>()?.Cluster.Config.ClusterId;
        context.SuppressResponseBody = true;
        RemoveContentHeaders(response, moduleResponse.Content);

        if (response.StatusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogWarning("Module {ModuleId} answered {StatusCode}; the body was replaced", moduleId, response.StatusCode);
            return;
        }

        var problem = await ReadProblemAsync(moduleResponse.Content, context.CancellationToken);
        if (problem is null)
        {
            logger.LogDebug(
                "Module {ModuleId} answered {StatusCode} without Problem Details; the body was replaced",
                moduleId,
                response.StatusCode);
            return;
        }

        problem[ProblemConventions.TraceIdExtension] = GatewayProblemDetails.TraceId(context.HttpContext);
        response.ContentType = MediaTypeNames.Application.ProblemJson;
        await response.WriteAsync(problem.ToJsonString(), context.CancellationToken);
    }

    // The module's content headers describe the body that is dropped, and the
    // status code pages write a body only while the response has no content type.
    private static void RemoveContentHeaders(HttpResponse response, HttpContent content)
    {
        foreach (var header in content.Headers)
        {
            response.Headers.Remove(header.Key);
        }
    }

    private static async Task<JsonObject?> ReadProblemAsync(HttpContent content, CancellationToken cancellationToken)
    {
        if (content.Headers.ContentType?.MediaType != MediaTypeNames.Application.ProblemJson
            || content.Headers.ContentLength > MaxProblemBytes)
        {
            return null;
        }

        await using var body = await content.ReadAsStreamAsync(cancellationToken);
        var buffer = new byte[MaxProblemBytes + 1];
        var length = await body.ReadAtLeastAsync(buffer, buffer.Length, throwOnEndOfStream: false, cancellationToken);
        if (length > MaxProblemBytes)
        {
            return null;
        }

        try
        {
            return JsonNode.Parse(buffer.AsSpan(0, length)) as JsonObject;
        }
        catch (JsonException)
        {
            // Not JSON despite the media type: the body does not conform.
            return null;
        }
    }
}
