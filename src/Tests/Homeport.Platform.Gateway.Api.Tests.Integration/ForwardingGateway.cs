// SPDX-License-Identifier: AGPL-3.0-or-later

using System.Net.Mime;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Homeport.Platform.Gateway.Api.Tests.Integration;

/// <summary>
/// The gateway in memory, forwarding to a stand-in for Echo on a local port.
/// The stand-in answers every request with the path and the headers it
/// received, so a test sees what the gateway forwards, except for the paths
/// under <see cref="ErrorsPath"/>, which answer with an error.
/// </summary>
public sealed class ForwardingGateway : IAsyncLifetime
{
    private const string LocalAddress = "http://127.0.0.1:0";

    /// <summary>The configuration key service discovery resolves the service "echo" from.</summary>
    internal const string EchoEndpointKey = "services:echo:http:0";

    /// <summary>The paths of the stand-in that answer with an error.</summary>
    internal const string ErrorsPath = "/v1/errors";

    /// <summary>A <c>404</c> as Problem Details of the stand-in's own type.</summary>
    internal const string NotFoundProblemPath = ErrorsPath + "/not-found-problem";

    /// <summary>A <c>500</c> as Problem Details whose detail carries internals.</summary>
    internal const string InternalProblemPath = ErrorsPath + "/internal-problem";

    /// <summary>A <c>400</c> with an HTML body.</summary>
    internal const string HtmlBadRequestPath = ErrorsPath + "/html-bad-request";

    internal const string NotFoundType = "https://stand-in.example/problems/not-found";

    internal const string NotFoundDetail = "No order 42.";

    internal const string InternalDetail = "Password=hunter2 rejected by the database.";

    private WebApplication? module;
    private WebApplicationFactory<Program>? gateway;

    internal HttpClient CreateClient() =>
        (gateway ?? throw new InvalidOperationException("The gateway has not been started.")).CreateClient();

    public async Task InitializeAsync()
    {
        var builder = WebApplication.CreateSlimBuilder();
        builder.WebHost.UseUrls(LocalAddress);
        module = builder.Build();
        module.MapGet(NotFoundProblemPath, () => Results.Problem(
            type: NotFoundType, detail: NotFoundDetail, statusCode: StatusCodes.Status404NotFound));
        module.MapGet(InternalProblemPath, () => Results.Problem(
            detail: InternalDetail, statusCode: StatusCodes.Status500InternalServerError));
        module.MapGet(HtmlBadRequestPath, () => Results.Content(
            "<html><body>Bad request</body></html>", MediaTypeNames.Text.Html, statusCode: StatusCodes.Status400BadRequest));
        module.MapFallback(context => context.Response.WriteAsJsonAsync(new ForwardedRequest(
            context.Request.Path,
            context.Request.Headers.ToDictionary(header => header.Key.ToLowerInvariant(), header => header.Value.ToString()))));
        await module.StartAsync();

        gateway = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(host => host.WithSigningKey().UseSetting(EchoEndpointKey, module.Urls.Single()));
    }

    public async Task DisposeAsync()
    {
        if (gateway is not null)
        {
            await gateway.DisposeAsync();
        }

        if (module is not null)
        {
            await module.DisposeAsync();
        }
    }
}
