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
        builder.Services.AddControllers();

        var app = builder.Build();
        app.MapHomeportModule();
        app.MapControllers();

        app.Run();
    }
}
