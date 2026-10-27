// SPDX-License-Identifier: AGPL-3.0-or-later

using Homeport.Sdk;

namespace Homeport.Modules.Echo.Api;

/// <summary>
/// The Echo module: the reference module of the module contract, which returns
/// whatever it is sent.
/// </summary>
public sealed class Program
{
    // Not static, so the integration tests can name it as their entry point.
    private Program()
    {
    }

    /// <summary>Wires the module and runs it.</summary>
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddHomeportModule(EchoManifest.Create());

        var app = builder.Build();
        app.MapHomeportModule();

        // Anonymous until the module validates the gateway's token, which comes
        // with #47 ("issue module tokens and check scopes"); it then requires
        // EchoManifest.InvokeScope.
        app.MapPost(EchoManifest.EchoPath, (HttpRequest request) =>
                Results.Stream(request.Body, request.ContentType ?? "application/octet-stream"))
            .WithSummary("Returns the request body unchanged.")
            .AllowAnonymous();

        app.Run();
    }
}
