// SPDX-License-Identifier: AGPL-3.0-or-later

namespace Homeport.Platform.Gateway.Api.Admin;

/// <summary>
/// Keeps the admin API and the public endpoint apart (ADR-0020), before any
/// token is looked at: a path under <see cref="AdminRoutes.AdminPrefix"/> on
/// any other port, and any other path on the admin port, answers <c>404</c>
/// like a path nothing is mounted at - whatever route would match it.
/// </summary>
internal sealed class AdminPortBoundary(RequestDelegate next, AdminPort adminPort, ILogger<AdminPortBoundary> logger)
{
    public async Task InvokeAsync(HttpContext context)
    {
        var onAdminPort = adminPort.Serves(context);
        if (onAdminPort != context.Request.Path.StartsWithSegments(AdminRoutes.AdminPrefix))
        {
            logger.LogDebug(
                "A request for {Path} on port {Port} was answered as unknown: the admin API answers on the admin port only, and only it does",
                context.Request.Path,
                context.Connection.LocalPort);

            // No endpoint, so it is answered exactly as an unmatched path is.
            context.SetEndpoint(null);
            context.Response.StatusCode = StatusCodes.Status404NotFound;

            return;
        }

        await next(context);
    }
}
